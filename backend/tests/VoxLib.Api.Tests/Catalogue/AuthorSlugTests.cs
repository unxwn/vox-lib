using VoxLib.Model.Book;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// FR-038: the rule that turns a name into an address, and the rule that a
/// collision is an error rather than something to paper over with a number.
/// <para>
/// These are unit tests rather than endpoint tests because the subject is a pure
/// function on the domain. The collision case is constructed here: R3 found one
/// in the placeholder catalogue, FR-064 removed that catalogue, and the rule is
/// worth proving on its own merit rather than because the seed happened to
/// violate it.
/// </para>
/// </summary>
public class AuthorSlugTests
{
    /// <summary>
    /// The four real book slugs, which is how R3 chose this transliteration in
    /// the first place: the cover files in Seed/covers/source/ were named before
    /// the function was written, so these are the answers it has to reproduce
    /// rather than a restatement of what it does.
    /// </summary>
    [Theory]
    [InlineData("Стратегія і тактика лідерства", "stratehiia-i-taktyka-liderstva")]
    [InlineData(
        "Людина на перехресті. Роздуми про екзистенційний інтелект",
        "liudyna-na-perekhresti-rozdumy-pro-ekzystentsiinyi-intelekt")]
    [InlineData("Євген Коновалець. Ода вождизму", "yevhen-konovalets-oda-vozhdyzmu")]
    [InlineData(
        "Щоденник сотника Устима. Як козаки Кавказ воювали",
        "shchodennyk-sotnyka-ustyma-yak-kozaky-kavkaz-voiuvaly")]
    public void The_real_book_slugs_are_reproduced_exactly(string title, string expected) =>
        Assert.Equal(expected, AuthorSlug.From(title));

    /// <summary>
    /// The pair the system is most often got wrong on. Ґ and Г are different
    /// letters and transliterate differently, and conflating them is the single
    /// most common defect in a Ukrainian transliteration. No real title
    /// exercises it, which is exactly why it is pinned here.
    /// </summary>
    [Theory]
    [InlineData("Ґудзик", "gudzyk")]
    [InlineData("Гайдамаки", "haidamaky")]
    public void The_two_letters_that_look_alike_stay_apart(string name, string expected) =>
        Assert.Equal(expected, AuthorSlug.From(name));

    /// <summary>
    /// Five letters transliterate differently at the start of a word than inside
    /// one, which is what the KMU 2010 table says and what a passport does.
    /// </summary>
    [Theory]
    [InlineData("Юрій Яновський", "yurii-yanovskyi")]
    [InlineData("Ігор Козловський", "ihor-kozlovskyi")]
    [InlineData("Валерій Бобрович", "valerii-bobrovych")]
    public void A_letter_at_the_start_of_a_word_differs_from_one_inside_it(
        string name,
        string expected) => Assert.Equal(expected, AuthorSlug.From(name));

    /// <summary>
    /// An apostrophe is dropped without ending the word, which is subtler than
    /// it looks: if it ended one, the я after it would take its word-initial
    /// form and Валер'ян would come out as valer-yan.
    /// </summary>
    [Fact]
    public void An_apostrophe_is_dropped_without_starting_a_new_word() =>
        Assert.Equal("valerian-pidmohylnyi", AuthorSlug.From("Валер'ян Підмогильний"));

    /// <summary>
    /// The collision FR-038 forbids resolving. Two spellings of one person
    /// produce one address, and the seeder's job is to refuse rather than to
    /// append a number: an automatic suffix turns two rows for one person into
    /// two addresses and buries the defect.
    /// </summary>
    [Fact]
    public void Two_spellings_of_one_person_collide_rather_than_being_numbered()
    {
        Assert.Equal(AuthorSlug.From("Taras Shevchenko"), AuthorSlug.From("Тарас Шевченко"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_name_that_is_not_a_name_is_refused(string name) =>
        Assert.Throws<ArgumentException>(() => AuthorSlug.From(name));
}
