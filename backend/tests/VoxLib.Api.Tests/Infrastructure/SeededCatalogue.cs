namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// What the committed seed dataset contains, stated once so the tests assert
/// against a single description of it rather than each keeping its own copy.
/// </summary>
public static class SeededCatalogue
{
    public const int PageSize = 20;

    public const int PublishedCount = 26;

    public const int PageCount = 2;

    /// <summary>Seeded but not published, so nothing may ever show it.</summary>
    public const string DraftSlug = "chorna-rada";

    /// <summary>
    /// Every published book, in the order the catalogue must present them:
    /// by title under Ukrainian collation, settled by slug.
    /// <para>
    /// This order is the point of several requirements at once, so it is worth
    /// reading rather than skimming. Under a naive byte ordering "Ґудзик" would
    /// fall at the end of the catalogue rather than after "Гайдамаки", and
    /// "Єретик", "Інтермеццо" and "Їжачок" would fall before "Апостол черні",
    /// because those letters sit outside the contiguous Cyrillic run. Latin
    /// sorts after Cyrillic, which is why the English title is last.
    /// </para>
    /// </summary>
    public static readonly string[] SlugsInOrder =
    [
        "apostol-cherni", // А
        "boiarynia", // Б
        "vershnyky", // В
        "haidamaky", // Г
        "gudzyk", // Ґ, immediately after Г and not at the end
        "dim-na-hori", // Д
        "eneida", // Е
        "yeretyk", // Є, after Е and not before А
        "zhovtyi-kniaz", // Ж
        "zemlia", // З
        "intermezzo", // І
        "yizhachok-i-zymova-kazka", // Ї, after І
        "kaidasheva-simia", // К
        "lisova-pisnia", // Л
        "misto", // М
        "natalka-poltavka", // Н
        "opovidannia-vovchok", // О
        "podorozh-doktora-leonardo", // П
        "roksolana", // Р
        "son-shevchenko", // С, last on page one
        "son-vovchok", // the same title, first on page two
        "tini-zabutykh-predkiv", // Т
        "ukradene-shchastia", // У
        "fata-morgana", // Ф
        "khiba-revut-voly", // Х
        "kobzar-selected-poems", // Latin, after every Cyrillic title
    ];

    /// <summary>
    /// The two books that share a title, straddling the page boundary. Together
    /// they are what proves the order is total: sorted by title alone, nothing
    /// decides which of them page one ends with.
    /// </summary>
    public const string TitleSharedByTwoBooks = "Сон";

    public const string LastSlugOnFirstPage = "son-shevchenko";

    public const string FirstSlugOnSecondPage = "son-vovchok";

    /// <summary>Published, and deliberately without cover art, per FR-006.</summary>
    public const string SlugWithoutCoverArt = "gudzyk";

    /// <summary>Published, and deliberately without a description.</summary>
    public const string SlugWithoutDescription = "yeretyk";

    /// <summary>Published with no chapters yet, so its running time is zero.</summary>
    public const string SlugWithoutChapters = "intermezzo";

    /// <summary>Published, with metadata in a language other than Ukrainian.</summary>
    public const string SlugInAnotherLanguage = "kobzar-selected-poems";
}
