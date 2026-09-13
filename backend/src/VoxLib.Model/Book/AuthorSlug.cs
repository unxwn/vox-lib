using System.Text;

namespace VoxLib.Model.Book;

/// <summary>
/// Turns a person's name into the address their page lives at, by the
/// transliteration table the Cabinet of Ministers fixed in 2010 — the one a
/// Ukrainian passport uses, so a name a reader recognises produces an address
/// they recognise.
/// <para>
/// Called exactly once, when the author row is created, and never recomputed.
/// FR-038 requires that correcting a spelling leave the address alone, which is
/// the same rule <see cref="Book.Slug"/> already follows and for the same reason:
/// an address that moves when a typo is fixed breaks every link to it.
/// </para>
/// </summary>
public static class AuthorSlug
{
    /// <summary>
    /// Five letters transliterate differently at the start of a word. This is
    /// the whole of what makes the table positional rather than a dictionary.
    /// </summary>
    private static readonly Dictionary<char, (string Initial, string Inner)> Positional =
        new()
        {
            ['є'] = ("ye", "ie"),
            ['ї'] = ("yi", "i"),
            ['й'] = ("y", "i"),
            ['ю'] = ("yu", "iu"),
            ['я'] = ("ya", "ia"),
        };

    private static readonly Dictionary<char, string> Fixed =
        new()
        {
            ['а'] = "a",
            ['б'] = "b",
            ['в'] = "v",
            ['г'] = "h",
            ['ґ'] = "g",
            ['д'] = "d",
            ['е'] = "e",
            ['ж'] = "zh",
            ['з'] = "z",
            ['и'] = "y",
            ['і'] = "i",
            ['к'] = "k",
            ['л'] = "l",
            ['м'] = "m",
            ['н'] = "n",
            ['о'] = "o",
            ['п'] = "p",
            ['р'] = "r",
            ['с'] = "s",
            ['т'] = "t",
            ['у'] = "u",
            ['ф'] = "f",
            ['х'] = "kh",
            ['ц'] = "ts",
            ['ч'] = "ch",
            ['ш'] = "sh",
            ['щ'] = "shch",
        };

    /// <summary>
    /// Written but never sounded, and — the part that is easy to get wrong —
    /// they do not end a word. Dropping them has to leave the letter after them
    /// inside the word, or Козловський comes out as kozlovsky.
    /// </summary>
    private const string Silent = "ь'’ʼ";

    /// <summary>
    /// The transliteration of <paramref name="name"/>, lowercased, with every
    /// run of anything else collapsed to a single hyphen.
    /// </summary>
    public static string From(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var builder = new StringBuilder(name.Length * 2);
        var atWordStart = true;

        for (var i = 0; i < name.Length; i++)
        {
            var letter = char.ToLowerInvariant(name[i]);

            // The one digraph in the table: зг is zgh, so that it stays legible
            // as з followed by г rather than reading back as ж.
            if (letter == 'з' && i + 1 < name.Length && char.ToLowerInvariant(name[i + 1]) == 'г')
            {
                builder.Append("zgh");
                atWordStart = false;
                i++;
                continue;
            }

            if (Silent.Contains(letter))
            {
                continue;
            }

            if (Positional.TryGetValue(letter, out var forms))
            {
                builder.Append(atWordStart ? forms.Initial : forms.Inner);
                atWordStart = false;
                continue;
            }

            if (Fixed.TryGetValue(letter, out var fixedForm))
            {
                builder.Append(fixedForm);
                atWordStart = false;
                continue;
            }

            // A name already written in latin letters keeps them.
            if (char.IsAsciiLetterOrDigit(letter))
            {
                builder.Append(letter);
                atWordStart = false;
                continue;
            }

            Separate(builder);
            atWordStart = true;
        }

        return builder.ToString().Trim('-');
    }

    private static void Separate(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] != '-')
        {
            builder.Append('-');
        }
    }
}
