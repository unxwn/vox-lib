namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// What the committed seed dataset contains — the books the project actually
/// holds — stated once so the tests assert against a single description of it
/// rather than each keeping its own copy.
/// <para>
/// It is small, and deliberately so: four books, one credit each, one of them a
/// compiler rather than an author, and no drafts. Anything needing a catalogue
/// with behaviour — paging, a contested sort order, a book missing its
/// description — reads <see cref="SampleCatalogue"/> instead, which a fixture
/// seeds. Shipping those to a reader to keep a test happy is the thing this
/// split exists to prevent.
/// </para>
/// </summary>
public static class SeededCatalogue
{
    public const int PageSize = 20;

    public const int PublishedCount = 4;

    public const int PageCount = 1;

    /// <summary>
    /// Every book, by slug. The whole catalogue fits on one page, so this is
    /// also the order the first page presents them in: by title under Ukrainian
    /// collation, settled by slug.
    /// </summary>
    public static readonly string[] SlugsInOrder =
    [
        "yevhen-konovalets-oda-vozhdyzmu", // Євген Коновалець. Ода вождизму
        "liudyna-na-perekhresti-rozdumy-pro-ekzystentsiinyi-intelekt", // Людина на перехресті
        "stratehiia-i-taktyka-liderstva", // Стратегія і тактика лідерства
        "shchodennyk-sotnyka-ustyma-yak-kozaky-kavkaz-voiuvaly", // Щоденник сотника Устима
    ];

    /// <summary>
    /// Every title, as <c>content.md</c> records them. The seed test matches in
    /// both directions against this, rather than counting: a count alone passes
    /// against the wrong four.
    /// </summary>
    public static readonly string[] Titles =
    [
        "Євген Коновалець. Ода вождизму",
        "Людина на перехресті. Роздуми про екзистенційний інтелект",
        "Стратегія і тактика лідерства",
        "Щоденник сотника Устима. Як козаки Кавказ воювали",
    ];

    /// <summary>The one book credited to a compiler rather than an author.</summary>
    public const string CompiledSlug = "yevhen-konovalets-oda-vozhdyzmu";

    public const string CompilerName = "Дмитро Савченко";

    public const string CompilerSlug = "dmytro-savchenko";

    /// <summary>A book credited to an author, for contrast with the compiler.</summary>
    public const string AuthoredSlug = "stratehiia-i-taktyka-liderstva";

    public const string AuthorName = "Джоко Віллінк";

    public const string AuthorSlug = "dzhoko-villink";

    /// <summary>Every author, in the order the index must present them.</summary>
    public static readonly string[] AuthorSlugsInOrder =
    [
        "valerii-bobrovych", // Бобрович, Валерій
        "dzhoko-villink", // Віллінк, Джоко
        "ihor-kozlovskyi", // Козловський, Ігор
        "dmytro-savchenko", // Савченко, Дмитро
    ];
}
