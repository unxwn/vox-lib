namespace VoxLib.Model.Common;

/// <summary>
/// One page of a larger set, carrying enough to state which page the visitor is
/// on and how many there are, as FR-004 requires.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Zero when nothing matches, so an empty catalogue has no pages to visit.</summary>
    public int PageCount => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
