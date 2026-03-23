using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ACLS.Persistence.Converters;

/// <summary>
/// ValueComparer for List{string} so EF Core can detect in-place mutations
/// (e.g. Add/Remove on RequiredSkills or Skills) during change tracking.
/// Elements are compared using ordinal string equality.
/// </summary>
public sealed class JsonStringListComparer : ValueComparer<List<string>>
{
    public JsonStringListComparer()
        : base(
            (l1, l2) => l1 != null && l2 != null && l1.SequenceEqual(l2, StringComparer.Ordinal),
            list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            list => list.ToList())
    {
    }
}
