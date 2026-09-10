namespace VoxLib.Model.Account;

/// <summary>
/// What a password must satisfy. One source of truth: the API configures the
/// identity component from it and also serves it, so the sentence a person reads
/// before submitting and the rule that rejects them cannot drift apart. FR-002.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>
    /// Counted in characters over Unicode, so a password written entirely in
    /// Cyrillic or containing an emoji is measured the way its author would
    /// measure it.
    /// </summary>
    public const int MinimumLength = 12;

    /// <summary>
    /// The longest password accepted. Not a security bound: hashing a megabyte
    /// of text on every sign-in attempt is, so this is a limit on the work an
    /// anonymous caller can ask for.
    /// </summary>
    public const int MaximumLength = 256;

    // No character class is required, deliberately. Class rules push people
    // toward short passwords full of substitutions, and they are painful to type
    // on a phone with a screen reader. Length is the requirement that matters.
    public const bool RequiresDigit = false;

    public const bool RequiresUppercase = false;

    public const bool RequiresLowercase = false;

    public const bool RequiresNonAlphanumeric = false;

    /// <summary>
    /// Length in text elements rather than UTF-16 code units, so an emoji built
    /// from a surrogate pair counts once rather than twice.
    /// </summary>
    public static int LengthOf(string password) =>
        new System.Globalization.StringInfo(password).LengthInTextElements;

    public static bool IsLongEnough(string password) => LengthOf(password) >= MinimumLength;
}
