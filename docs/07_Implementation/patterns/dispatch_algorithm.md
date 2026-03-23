# Dispatch Algorithm

**Document:** `docs/07_Implementation/patterns/dispatch_algorithm.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document defines the Smart Dispatch algorithm exactly. The scoring formula, weights, edge cases, and return contract described here are authoritative and must be implemented precisely. Do not adjust weights, change the scoring formula, or handle edge cases differently from what is specified. The algorithm is tested against this specification in `tests/unit/Domain.Tests/DispatchServiceTests.cs`.

---

## 1. Purpose

`IDispatchService.FindOptimalStaffAsync` takes a `Complaint` and returns a ranked list of `StaffScore` objects representing available staff members ordered from most suitable to least suitable. The list is consumed by the Manager Dashboard to guide — not automate — assignment decisions. The manager reviews the ranked list and makes the final assignment.

The service never makes assignments. It only ranks.

---

## 2. Interface Contract

**Location:** `ACLS.Domain/Dispatch/IDispatchService.cs`

```csharp
namespace ACLS.Domain.Dispatch;

public interface IDispatchService
{
    /// <summary>
    /// Returns a ranked list of available staff members for the given complaint,
    /// ordered by match score descending. Returns an empty list if no staff are available.
    /// Never throws for an empty candidate pool — returns empty list.
    /// </summary>
    Task<IReadOnlyList<StaffScore>> FindOptimalStaffAsync(
        Complaint complaint,
        int propertyId,
        CancellationToken ct);
}
```

**Return type:** `IReadOnlyList<StaffScore>` ordered by `MatchScore` descending.  
**Empty pool:** Returns `[]` (empty list). Never throws.  
**SOS complaints:** Uses the same algorithm with `urgencyWeight = 2.0`. Returns the same ranked list — the SOS handler then notifies ALL candidates on the list simultaneously rather than waiting for manager selection.

---

## 3. Supporting Value Objects

**Location:** `ACLS.Domain/Dispatch/`

```csharp
// StaffScore.cs
namespace ACLS.Domain.Dispatch;

public sealed record StaffScore
{
    public StaffMember StaffMember { get; init; }
    public double MatchScore { get; init; }    // final weighted score
    public double SkillScore { get; init; }    // 0.0 – 1.0
    public double IdleScore { get; init; }     // 0.0 – 1.0

    public StaffScore(
        StaffMember staffMember,
        double matchScore,
        double skillScore,
        double idleScore)
    {
        StaffMember = staffMember;
        MatchScore = matchScore;
        SkillScore = skillScore;
        IdleScore = idleScore;
    }
}
```

```csharp
// DispatchWeights.cs — constants, never magic numbers inline
namespace ACLS.Domain.Dispatch;

public static class DispatchWeights
{
    public const double SkillWeight = 0.6;
    public const double IdleWeight = 0.4;
    public const double SosUrgencyMultiplier = 2.0;
    public const double DefaultUrgencyMultiplier = 1.0;
}
```

---

## 4. The Scoring Formula

For each candidate `StaffMember` in the available pool:

```
matchScore = (skillScore × 0.6 + idleScore × 0.4) × urgencyWeight
```

Where:
- `skillScore` is calculated per Section 5
- `idleScore` is calculated per Section 6
- `urgencyWeight` is determined per Section 7

The final ranked list is ordered by `matchScore` descending. Higher score = better candidate.

---

## 5. SkillScore Calculation

`SkillScore` measures what proportion of the complaint's required skills the staff member possesses.

```
skillScore = count(intersection(staff.Skills, complaint.RequiredSkills))
             ÷ count(complaint.RequiredSkills)
```

**Range:** 0.0 to 1.0

### Edge Cases

| Scenario | Behaviour |
|---|---|
| `complaint.RequiredSkills` is empty or null | `skillScore = 1.0` for all candidates — skills are not a differentiator |
| Staff has all required skills | `skillScore = 1.0` |
| Staff has none of the required skills | `skillScore = 0.0` |
| Staff has some required skills | `skillScore = matching count ÷ total required count` |
| Skill matching is case-insensitive | `"plumbing"` matches `"Plumbing"` |

### Implementation

```csharp
private static double CalculateSkillScore(
    StaffMember staff,
    Complaint complaint)
{
    if (complaint.RequiredSkills is null || complaint.RequiredSkills.Count == 0)
        return 1.0;

    var requiredSkills = complaint.RequiredSkills
        .Select(s => s.ToLowerInvariant())
        .ToHashSet();

    var staffSkills = staff.Skills
        .Select(s => s.ToLowerInvariant())
        .ToHashSet();

    var matchingCount = requiredSkills.Intersect(staffSkills).Count();

    return (double)matchingCount / requiredSkills.Count;
}
```

---

## 6. IdleScore Calculation

`IdleScore` measures how long a staff member has been without an active assignment, relative to the rest of the candidate pool. A staff member who has been idle the longest gets the highest `idleScore`. This prevents workload concentration on a single staff member.

```
idleTime(staff)     = DateTime.UtcNow - staff.LastAssignedAt
maxIdleTime(pool)   = max(idleTime) across all candidates in pool
idleScore(staff)    = idleTime(staff) ÷ maxIdleTime(pool)
```

**Range:** 0.0 to 1.0

### Edge Cases

| Scenario | Behaviour |
|---|---|
| `staff.LastAssignedAt` is null (never assigned) | Treat as if assigned at `DateTime.MinValue` — maximum idle time, gives `idleScore = 1.0` |
| All candidates have the same `LastAssignedAt` | All candidates receive `idleScore = 1.0` — idle time is not a differentiator |
| Only one candidate in pool | `idleScore = 1.0` — no relative comparison needed |
| `maxIdleTime` is zero (all assigned at same instant) | `idleScore = 1.0` for all — avoid division by zero |

### Implementation

```csharp
private static double CalculateIdleScore(
    StaffMember staff,
    DateTime now,
    TimeSpan maxIdleTime)
{
    if (maxIdleTime == TimeSpan.Zero)
        return 1.0;  // All staff equally idle — avoid division by zero

    var lastAssigned = staff.LastAssignedAt ?? DateTime.MinValue;
    var idleTime = now - lastAssigned;

    // Clamp to [0, maxIdleTime] in case of clock skew
    var clampedIdleTime = TimeSpan.FromTicks(
        Math.Max(0, Math.Min(idleTime.Ticks, maxIdleTime.Ticks)));

    return clampedIdleTime.TotalSeconds / maxIdleTime.TotalSeconds;
}
```

The `maxIdleTime` is computed once across the entire candidate pool before scoring individual candidates:

```csharp
var now = DateTime.UtcNow;

var maxIdleTime = candidates
    .Select(s => now - (s.LastAssignedAt ?? DateTime.MinValue))
    .Max();
```

---

## 7. UrgencyWeight

`urgencyWeight` is a multiplier applied to the base score. It inflates scores for emergency complaints so that the entire ranking reflects the elevated priority context.

| Urgency | `urgencyWeight` |
|---|---|
| `SOS_EMERGENCY` | `2.0` |
| `HIGH` | `1.0` |
| `MEDIUM` | `1.0` |
| `LOW` | `1.0` |

Only `SOS_EMERGENCY` triggers the multiplier. `HIGH`, `MEDIUM`, and `LOW` all use the default multiplier of `1.0`.

```csharp
private static double GetUrgencyWeight(Urgency urgency)
    => urgency == Urgency.SOS_EMERGENCY
        ? DispatchWeights.SosUrgencyMultiplier
        : DispatchWeights.DefaultUrgencyMultiplier;
```

**Effect on score range:** With `urgencyWeight = 2.0`, the maximum possible `matchScore` is `2.0` (when `skillScore = 1.0` and `idleScore = 1.0`). With `urgencyWeight = 1.0`, the maximum is `1.0`. The scores are not normalised after applying the multiplier — the ranking order is what matters, not the absolute values.

---

## 8. Complete Algorithm Implementation

**Location:** `ACLS.Infrastructure/Dispatch/DispatchService.cs`

```csharp
namespace ACLS.Infrastructure.Dispatch;

public sealed class DispatchService : IDispatchService
{
    private readonly IStaffRepository _staffRepository;

    public DispatchService(IStaffRepository staffRepository)
        => _staffRepository = staffRepository;

    public async Task<IReadOnlyList<StaffScore>> FindOptimalStaffAsync(
        Complaint complaint,
        int propertyId,
        CancellationToken ct)
    {
        // Step 1: Fetch all available staff for this property
        var candidates = await _staffRepository.GetAvailableAsync(propertyId, ct);

        if (candidates.Count == 0)
            return [];

        // Step 2: Compute max idle time across the pool (for normalisation)
        var now = DateTime.UtcNow;
        var maxIdleTime = candidates
            .Select(s => now - (s.LastAssignedAt ?? DateTime.MinValue))
            .Max();

        // Step 3: Determine urgency weight
        var urgencyWeight = GetUrgencyWeight(complaint.Urgency);

        // Step 4: Score each candidate
        var scores = candidates
            .Select(staff =>
            {
                var skillScore = CalculateSkillScore(staff, complaint);
                var idleScore = CalculateIdleScore(staff, now, maxIdleTime);
                var matchScore = (skillScore * DispatchWeights.SkillWeight
                               + idleScore  * DispatchWeights.IdleWeight)
                               * urgencyWeight;

                return new StaffScore(
                    staffMember: staff,
                    matchScore: matchScore,
                    skillScore: skillScore,
                    idleScore: idleScore);
            })
            .OrderByDescending(s => s.MatchScore)  // Step 5: Rank
            .ToList();

        return scores.AsReadOnly();
    }

    private static double CalculateSkillScore(
        StaffMember staff,
        Complaint complaint)
    {
        if (complaint.RequiredSkills is null || complaint.RequiredSkills.Count == 0)
            return 1.0;

        var requiredSkills = complaint.RequiredSkills
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        var staffSkills = staff.Skills
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        var matchingCount = requiredSkills.Intersect(staffSkills).Count();

        return (double)matchingCount / requiredSkills.Count;
    }

    private static double CalculateIdleScore(
        StaffMember staff,
        DateTime now,
        TimeSpan maxIdleTime)
    {
        if (maxIdleTime == TimeSpan.Zero)
            return 1.0;

        var lastAssigned = staff.LastAssignedAt ?? DateTime.MinValue;
        var idleTime = now - lastAssigned;

        var clampedIdleTime = TimeSpan.FromTicks(
            Math.Max(0, Math.Min(idleTime.Ticks, maxIdleTime.Ticks)));

        return clampedIdleTime.TotalSeconds / maxIdleTime.TotalSeconds;
    }

    private static double GetUrgencyWeight(Urgency urgency)
        => urgency == Urgency.SOS_EMERGENCY
            ? DispatchWeights.SosUrgencyMultiplier
            : DispatchWeights.DefaultUrgencyMultiplier;
}
```

---

## 9. NUnit Test Specification

**Location:** `tests/unit/Domain.Tests/DispatchServiceTests.cs`

The following tests must be implemented and must pass. They are the specification encoded as assertions.

### 9.1 Required Test Cases

```csharp
[TestFixture]
public sealed class DispatchServiceTests
{
    // Test 1: Perfect skill match ranks higher than partial match
    [Test]
    public async Task FindOptimalStaff_WhenOneStaffHasAllRequiredSkills_RanksThatStaffFirst()

    // Test 2: Partial skill match scores proportionally
    [Test]
    public async Task FindOptimalStaff_WhenStaffHasHalfRequiredSkills_SkillScoreIsPointFive()

    // Test 3: No required skills — all candidates equal on skill
    [Test]
    public async Task FindOptimalStaff_WhenComplaintHasNoRequiredSkills_AllCandidatesGetSkillScoreOfOne()

    // Test 4: Idle time breaks ties — longer idle ranks first
    [Test]
    public async Task FindOptimalStaff_WhenSkillScoresEqual_LongerIdleStaffRanksFirst()

    // Test 5: Never assigned staff gets maximum idle score
    [Test]
    public async Task FindOptimalStaff_WhenStaffHasNeverBeenAssigned_GetsMaxIdleScore()

    // Test 6: SOS doubles the match score
    [Test]
    public async Task FindOptimalStaff_WhenUrgencyIsSosEmergency_MatchScoreIsDoubled()

    // Test 7: SOS preserves ranking order
    [Test]
    public async Task FindOptimalStaff_WhenUrgencyIsSosEmergency_RankingOrderIsPreserved()

    // Test 8: Empty pool returns empty list, does not throw
    [Test]
    public async Task FindOptimalStaff_WhenNoStaffAvailable_ReturnsEmptyList()

    // Test 9: Single candidate returns list of one
    [Test]
    public async Task FindOptimalStaff_WhenOneCandidateInPool_ReturnsSingleScoreWithIdleScoreOfOne()

    // Test 10: All candidates with same idle time get IdleScore = 1.0
    [Test]
    public async Task FindOptimalStaff_WhenAllCandidatesHaveSameLastAssignedAt_AllGetIdleScoreOfOne()

    // Test 11: Skill matching is case-insensitive
    [Test]
    public async Task FindOptimalStaff_WhenSkillCasingDiffers_StillMatchesCorrectly()

    // Test 12: Score weights sum correctly (0.6 + 0.4 = 1.0 at max)
    [Test]
    public async Task FindOptimalStaff_WhenPerfectMatch_MatchScoreIsOne()
}
```

### 9.2 Example Test Implementation

```csharp
[Test]
public async Task FindOptimalStaff_WhenOneStaffHasAllRequiredSkills_RanksThatStaffFirst()
{
    // Arrange
    var complaint = CreateComplaint(
        requiredSkills: ["Plumbing", "General"],
        urgency: Urgency.HIGH);

    var fullMatchStaff = CreateStaff(
        skills: ["Plumbing", "General", "Electrical"],
        lastAssignedAt: DateTime.UtcNow.AddHours(-2));

    var partialMatchStaff = CreateStaff(
        skills: ["Plumbing"],
        lastAssignedAt: DateTime.UtcNow.AddHours(-3));  // more idle but fewer skills

    _staffRepositoryMock
        .Setup(r => r.GetAvailableAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync([fullMatchStaff, partialMatchStaff]);

    // Act
    var result = await _sut.FindOptimalStaffAsync(
        complaint, propertyId: 1, CancellationToken.None);

    // Assert
    Assert.That(result, Has.Count.EqualTo(2));
    Assert.That(result[0].StaffMember, Is.SameAs(fullMatchStaff),
        "Staff with full skill match should rank first despite less idle time.");
    Assert.That(result[0].SkillScore, Is.EqualTo(1.0));
    Assert.That(result[1].SkillScore, Is.EqualTo(0.5));
}

[Test]
public async Task FindOptimalStaff_WhenUrgencyIsSosEmergency_MatchScoreIsDoubled()
{
    // Arrange
    var highComplaint = CreateComplaint(
        requiredSkills: ["Plumbing"],
        urgency: Urgency.HIGH);

    var sosComplaint = CreateComplaint(
        requiredSkills: ["Plumbing"],
        urgency: Urgency.SOS_EMERGENCY);

    var staff = CreateStaff(
        skills: ["Plumbing"],
        lastAssignedAt: null);  // never assigned → idleScore = 1.0

    _staffRepositoryMock
        .Setup(r => r.GetAvailableAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync([staff]);

    // Act
    var highResult = await _sut.FindOptimalStaffAsync(
        highComplaint, propertyId: 1, CancellationToken.None);

    var sosResult = await _sut.FindOptimalStaffAsync(
        sosComplaint, propertyId: 1, CancellationToken.None);

    // Assert — SOS score should be exactly double the HIGH score
    Assert.That(sosResult[0].MatchScore,
        Is.EqualTo(highResult[0].MatchScore * 2.0).Within(0.0001));
}

[Test]
public async Task FindOptimalStaff_WhenNoStaffAvailable_ReturnsEmptyList()
{
    // Arrange
    _staffRepositoryMock
        .Setup(r => r.GetAvailableAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync([]);

    var complaint = CreateComplaint(requiredSkills: ["Plumbing"], urgency: Urgency.HIGH);

    // Act
    var result = await _sut.FindOptimalStaffAsync(
        complaint, propertyId: 1, CancellationToken.None);

    // Assert
    Assert.That(result, Is.Empty);
}

[Test]
public async Task FindOptimalStaff_WhenPerfectMatch_MatchScoreIsOne()
{
    // Arrange — staff has all required skills and was the longest idle
    var complaint = CreateComplaint(
        requiredSkills: ["Plumbing"],
        urgency: Urgency.HIGH);  // urgencyWeight = 1.0

    var staff = CreateStaff(
        skills: ["Plumbing"],
        lastAssignedAt: null);  // never assigned → idleScore = 1.0

    _staffRepositoryMock
        .Setup(r => r.GetAvailableAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync([staff]);

    // Act
    var result = await _sut.FindOptimalStaffAsync(
        complaint, propertyId: 1, CancellationToken.None);

    // Assert
    // skillScore=1.0 × 0.6 + idleScore=1.0 × 0.4 = 1.0 × urgencyWeight=1.0 = 1.0
    Assert.That(result[0].MatchScore, Is.EqualTo(1.0).Within(0.0001));
    Assert.That(result[0].SkillScore, Is.EqualTo(1.0).Within(0.0001));
    Assert.That(result[0].IdleScore, Is.EqualTo(1.0).Within(0.0001));
}
```

---

## 10. How the Algorithm Integrates with SOS

For `SOS_EMERGENCY` complaints, the algorithm is called the same way but the caller (`TriggerSosCommandHandler`) uses the result differently from a normal dispatch:

```
Normal dispatch (Manager-initiated):
  IDispatchService.FindOptimalStaffAsync(complaint) → ranked list
  Manager reviews list in dashboard → selects one staff member → calls AssignComplaint

SOS dispatch (Resident-initiated):
  IDispatchService.FindOptimalStaffAsync(complaint) → ranked list
  System takes ALL candidates from list → notifies ALL simultaneously
  (does not wait for manager selection)
```

The `DispatchService` itself does not know whether the result will be used for manual selection or simultaneous blast. It just returns the ranked list. The caller decides what to do with it.

---

## 11. Score Examples

### Example 1: Two candidates, different skills

**Complaint:** Category = Plumbing, RequiredSkills = `["Plumbing", "General"]`, Urgency = HIGH

| Staff | Skills | LastAssignedAt | SkillScore | IdleScore | MatchScore |
|---|---|---|---|---|---|
| John | Plumbing, General, Electrical | 2 hours ago | 1.0 | 0.40 | (1.0×0.6 + 0.40×0.4) × 1.0 = **0.76** |
| Maria | General, HVAC | 5 hours ago | 0.5 | 1.0 | (0.5×0.6 + 1.0×0.4) × 1.0 = **0.70** |

**Result:** John ranks first (0.76 > 0.70) — skill match outweighs idle time advantage.

### Example 2: Same skills, different idle times

**Complaint:** RequiredSkills = `[]` (no specific skill), Urgency = MEDIUM

| Staff | Skills | LastAssignedAt | SkillScore | IdleScore | MatchScore |
|---|---|---|---|---|---|
| Alice | General | 1 hour ago | 1.0 | 0.25 | (1.0×0.6 + 0.25×0.4) × 1.0 = **0.70** |
| Bob | General | 4 hours ago | 1.0 | 1.0 | (1.0×0.6 + 1.0×0.4) × 1.0 = **1.00** |

**Result:** Bob ranks first (1.00 > 0.70) — idle time is the differentiator when skills are equal.

### Example 3: SOS multiplier

**Same scenario as Example 1 but Urgency = SOS_EMERGENCY:**

| Staff | MatchScore (HIGH) | MatchScore (SOS) |
|---|---|---|
| John | 0.76 | **1.52** |
| Maria | 0.70 | **1.40** |

**Result:** Ranking order preserved. Both scores doubled. All candidates notified simultaneously.

---

*End of Dispatch Algorithm v1.0*
