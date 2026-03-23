using ACLS.Domain.Complaints;
using ACLS.Domain.Dispatch;
using ACLS.Domain.Staff;
using ACLS.Infrastructure.Dispatch;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Domain.Tests.Dispatch;

/// <summary>
/// Unit tests for the Smart Dispatch algorithm (DispatchService).
/// All 12 tests specified in docs/07_Implementation/patterns/dispatch_algorithm.md Section 9.
/// The concrete DispatchService is tested with a mocked IStaffRepository (no database).
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public sealed class DispatchServiceTests
{
    private IStaffRepository _staffRepositoryMock = null!;
    private DispatchService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _staffRepositoryMock = Substitute.For<IStaffRepository>();
        _sut = new DispatchService(_staffRepositoryMock);
    }

    // ── Factory helpers ───────────────────────────────────────────────────────

    private static Complaint CreateComplaint(
        List<string>? requiredSkills = null,
        Urgency urgency = Urgency.MEDIUM)
        => Complaint.Create(
            title: "Test complaint",
            description: "Test description for dispatch scoring.",
            category: "Plumbing",
            urgency: urgency,
            unitId: 1,
            residentId: 1,
            propertyId: 1,
            permissionToEnter: false,
            requiredSkills: requiredSkills);

    private static StaffMember CreateStaff(
        List<string>? skills = null,
        DateTime? lastAssignedAt = default,
        // Use a sentinel to distinguish "pass null" from "use default"
        bool neverAssigned = false)
    {
        var staff = StaffMember.Create(
            userId: new Random().Next(1, 10_000),
            jobTitle: "Maintenance",
            skills: skills ?? []);

        if (!neverAssigned && lastAssignedAt.HasValue)
            staff.SetLastAssignedAtForTesting(lastAssignedAt.Value);
        // neverAssigned == true → LastAssignedAt stays null (never assigned)

        return staff;
    }

    // ── Test 1: Perfect skill match ranks higher than partial match ────────────

    [Test]
    public async Task FindOptimalStaff_WhenOneStaffHasAllRequiredSkills_RanksThatStaffFirst()
    {
        // Arrange
        var complaint = CreateComplaint(
            requiredSkills: ["Plumbing", "General"],
            urgency: Urgency.HIGH);

        var fullMatchStaff = StaffMember.Create(userId: 1, skills: ["Plumbing", "General", "Electrical"]);
        fullMatchStaff.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-2));

        var partialMatchStaff = StaffMember.Create(userId: 2, skills: ["Plumbing"]);
        partialMatchStaff.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-3)); // more idle but fewer skills

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([fullMatchStaff, partialMatchStaff]);

        // Act
        var result = await _sut.FindOptimalStaffAsync(complaint, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].StaffMember.Should().BeSameAs(fullMatchStaff,
            "staff with full skill match should rank first despite less idle time");
        result[0].SkillScore.Should().Be(1.0);
        result[1].SkillScore.Should().BeApproximately(0.5, precision: 0.0001);
    }

    // ── Test 2: Partial skill match scores proportionally ─────────────────────

    [Test]
    public async Task FindOptimalStaff_WhenStaffHasHalfRequiredSkills_SkillScoreIsPointFive()
    {
        // Arrange
        var complaint = CreateComplaint(
            requiredSkills: ["Plumbing", "Electrical"],
            urgency: Urgency.MEDIUM);

        var staff = StaffMember.Create(userId: 1, skills: ["Plumbing"]);
        staff.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-1));

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([staff]);

        // Act
        var result = await _sut.FindOptimalStaffAsync(complaint, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].SkillScore.Should().BeApproximately(0.5, precision: 0.0001);
    }

    // ── Test 3: No required skills — all candidates equal on skill ─────────────

    [Test]
    public async Task FindOptimalStaff_WhenComplaintHasNoRequiredSkills_AllCandidatesGetSkillScoreOfOne()
    {
        // Arrange
        var complaint = CreateComplaint(requiredSkills: [], urgency: Urgency.LOW);

        var staffA = StaffMember.Create(userId: 1, skills: ["Plumbing"]);
        staffA.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-1));

        var staffB = StaffMember.Create(userId: 2, skills: []);
        staffB.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-2));

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([staffA, staffB]);

        // Act
        var result = await _sut.FindOptimalStaffAsync(complaint, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.SkillScore.Should().Be(1.0));
    }

    // ── Test 4: Idle time breaks ties — longer idle ranks first ───────────────

    [Test]
    public async Task FindOptimalStaff_WhenSkillScoresEqual_LongerIdleStaffRanksFirst()
    {
        // Arrange — both have same skills, but staffB was idle longer
        var complaint = CreateComplaint(requiredSkills: ["Plumbing"], urgency: Urgency.MEDIUM);

        var staffA = StaffMember.Create(userId: 1, skills: ["Plumbing"]);
        staffA.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-1)); // less idle

        var staffB = StaffMember.Create(userId: 2, skills: ["Plumbing"]);
        staffB.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-5)); // more idle

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([staffA, staffB]);

        // Act
        var result = await _sut.FindOptimalStaffAsync(complaint, CancellationToken.None);

        // Assert
        result[0].StaffMember.Should().BeSameAs(staffB,
            "staffB was idle longer and should rank first when skill scores are equal");
        result[0].IdleScore.Should().BeApproximately(1.0, precision: 0.0001);
    }

    // ── Test 5: Never-assigned staff gets maximum idle score ──────────────────

    [Test]
    public async Task FindOptimalStaff_WhenStaffHasNeverBeenAssigned_GetsMaxIdleScore()
    {
        // Arrange
        var complaint = CreateComplaint(requiredSkills: [], urgency: Urgency.LOW);

        var neverAssignedStaff = StaffMember.Create(userId: 1, skills: []);
        // LastAssignedAt is null — never been assigned

        var recentlyAssignedStaff = StaffMember.Create(userId: 2, skills: []);
        recentlyAssignedStaff.SetLastAssignedAtForTesting(DateTime.UtcNow.AddMinutes(-30));

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([neverAssignedStaff, recentlyAssignedStaff]);

        // Act
        var result = await _sut.FindOptimalStaffAsync(complaint, CancellationToken.None);

        // Assert
        result[0].StaffMember.Should().BeSameAs(neverAssignedStaff,
            "never-assigned staff has the max idle time and should rank first");
        result[0].IdleScore.Should().BeApproximately(1.0, precision: 0.0001);
    }

    // ── Test 6: SOS doubles the match score ───────────────────────────────────

    [Test]
    public async Task FindOptimalStaff_WhenUrgencyIsSosEmergency_MatchScoreIsDoubled()
    {
        // Arrange — same staff, both complaints require same skill
        var highComplaint = CreateComplaint(requiredSkills: ["Plumbing"], urgency: Urgency.HIGH);
        var sosComplaint  = CreateComplaint(requiredSkills: ["Plumbing"], urgency: Urgency.SOS_EMERGENCY);

        var staff = StaffMember.Create(userId: 1, skills: ["Plumbing"]);
        // LastAssignedAt = null → idleScore = 1.0

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([staff]);

        // Act
        var highResult = await _sut.FindOptimalStaffAsync(
            highComplaint, CancellationToken.None);

        var sosResult = await _sut.FindOptimalStaffAsync(
            sosComplaint, CancellationToken.None);

        // Assert — SOS score must be exactly double the HIGH score
        sosResult[0].MatchScore.Should()
            .BeApproximately(highResult[0].MatchScore * 2.0, precision: 0.0001,
                because: "SOS_EMERGENCY urgencyWeight=2.0 is exactly double the default 1.0");
    }

    // ── Test 7: SOS preserves ranking order ───────────────────────────────────

    [Test]
    public async Task FindOptimalStaff_WhenUrgencyIsSosEmergency_RankingOrderIsPreserved()
    {
        // Arrange — full-match staff should rank first for both HIGH and SOS
        var highComplaint = CreateComplaint(requiredSkills: ["Plumbing"], urgency: Urgency.HIGH);
        var sosComplaint  = CreateComplaint(requiredSkills: ["Plumbing"], urgency: Urgency.SOS_EMERGENCY);

        var fullMatchStaff = StaffMember.Create(userId: 1, skills: ["Plumbing"]);
        fullMatchStaff.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-1));

        var noMatchStaff = StaffMember.Create(userId: 2, skills: []);
        noMatchStaff.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-2));

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([fullMatchStaff, noMatchStaff]);

        // Act
        var highResult = await _sut.FindOptimalStaffAsync(
            highComplaint, CancellationToken.None);

        var sosResult = await _sut.FindOptimalStaffAsync(
            sosComplaint, CancellationToken.None);

        // Assert — same first-place ranking whether HIGH or SOS
        highResult[0].StaffMember.Should().BeSameAs(fullMatchStaff);
        sosResult[0].StaffMember.Should().BeSameAs(fullMatchStaff,
            "SOS multiplier scales all scores equally; relative ranking order is preserved");
    }

    // ── Test 8: Empty pool returns empty list, does not throw ─────────────────

    [Test]
    public async Task FindOptimalStaff_WhenNoStaffAvailable_ReturnsEmptyList()
    {
        // Arrange
        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var complaint = CreateComplaint(requiredSkills: ["Plumbing"], urgency: Urgency.HIGH);

        // Act
        var result = await _sut.FindOptimalStaffAsync(
            complaint, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ── Test 9: Single candidate returns list of one ──────────────────────────

    [Test]
    public async Task FindOptimalStaff_WhenOneCandidateInPool_ReturnsSingleScoreWithIdleScoreOfOne()
    {
        // Arrange
        var complaint = CreateComplaint(requiredSkills: [], urgency: Urgency.LOW);
        var staff = StaffMember.Create(userId: 1, skills: []);
        staff.SetLastAssignedAtForTesting(DateTime.UtcNow.AddHours(-3));

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([staff]);

        // Act
        var result = await _sut.FindOptimalStaffAsync(
            complaint, CancellationToken.None);

        // Assert — single candidate is always normalised to IdleScore = 1.0
        result.Should().HaveCount(1);
        result[0].IdleScore.Should().BeApproximately(1.0, precision: 0.0001,
            because: "with only one candidate, they are the max idle — no comparison possible");
    }

    // ── Test 10: All candidates with same LastAssignedAt get IdleScore = 1.0 ──

    [Test]
    public async Task FindOptimalStaff_WhenAllCandidatesHaveSameLastAssignedAt_AllGetIdleScoreOfOne()
    {
        // Arrange
        var sameTime = DateTime.UtcNow.AddHours(-2);
        var complaint = CreateComplaint(requiredSkills: [], urgency: Urgency.LOW);

        var staffA = StaffMember.Create(userId: 1, skills: []);
        staffA.SetLastAssignedAtForTesting(sameTime);

        var staffB = StaffMember.Create(userId: 2, skills: []);
        staffB.SetLastAssignedAtForTesting(sameTime);

        var staffC = StaffMember.Create(userId: 3, skills: []);
        staffC.SetLastAssignedAtForTesting(sameTime);

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([staffA, staffB, staffC]);

        // Act
        var result = await _sut.FindOptimalStaffAsync(
            complaint, CancellationToken.None);

        // Assert — maxIdleTime - minIdleTime = 0, all get IdleScore = 1.0
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(s =>
            s.IdleScore.Should().BeApproximately(1.0, precision: 0.0001,
                because: "when all idle times are equal the max is the same as each value, so ratio = 1.0"));
    }

    // ── Test 11: Skill matching is case-insensitive ────────────────────────────

    [Test]
    public async Task FindOptimalStaff_WhenSkillCasingDiffers_StillMatchesCorrectly()
    {
        // Arrange — complaint requires "PLUMBING" (upper), staff has "plumbing" (lower)
        var complaint = CreateComplaint(
            requiredSkills: ["PLUMBING", "ELECTRICAL"],
            urgency: Urgency.MEDIUM);

        var staff = StaffMember.Create(
            userId: 1,
            skills: ["plumbing", "electrical"]); // lower case

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([staff]);

        // Act
        var result = await _sut.FindOptimalStaffAsync(
            complaint, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].SkillScore.Should().BeApproximately(1.0, precision: 0.0001,
            because: "skill comparison is case-insensitive; 'plumbing' matches 'PLUMBING'");
    }

    // ── Test 12: Score weights sum correctly producing max score = 1.0 ────────

    [Test]
    public async Task FindOptimalStaff_WhenPerfectMatch_MatchScoreIsOne()
    {
        // Arrange — staff has all required skills, never been assigned (idleScore = 1.0)
        // urgency = HIGH → urgencyWeight = 1.0
        // matchScore = (1.0 × 0.6 + 1.0 × 0.4) × 1.0 = 1.0
        var complaint = CreateComplaint(
            requiredSkills: ["Plumbing"],
            urgency: Urgency.HIGH);

        var staff = StaffMember.Create(userId: 1, skills: ["Plumbing"]);
        // LastAssignedAt = null → idleScore = 1.0 (treated as DateTime.MinValue, max idle)

        _staffRepositoryMock
            .GetAvailableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([staff]);

        // Act
        var result = await _sut.FindOptimalStaffAsync(
            complaint, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].SkillScore.Should().BeApproximately(1.0, precision: 0.0001);
        result[0].IdleScore.Should().BeApproximately(1.0, precision: 0.0001);
        result[0].MatchScore.Should().BeApproximately(1.0, precision: 0.0001,
            because: $"(1.0 × {DispatchWeights.SkillWeight} + 1.0 × {DispatchWeights.IdleWeight}) × {DispatchWeights.DefaultUrgencyMultiplier} = 1.0");
    }
}
