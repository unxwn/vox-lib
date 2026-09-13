namespace VoxLib.Model.Book;

/// <summary>
/// How one person is credited on one book. Two values, not an open vocabulary:
/// translator is deferred with everything else the content feature will bring,
/// and a narrator is a name on the book rather than a third role here, because
/// it says something about a recording rather than about the text.
/// </summary>
public enum CreditRole
{
    /// <summary>Wrote it. What a reader assumes when nothing says otherwise.</summary>
    Author,

    /// <summary>Assembled it from other people's writing.</summary>
    Compiler,
}
