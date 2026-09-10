namespace VoxLib.Api.Account;

/// <summary>
/// The numbers and names this feature has to state. They are here rather than
/// scattered through the composition root because several of them are told to a
/// person, and a number a person is told is a requirement rather than tuning.
/// </summary>
public static class AccountDefaults
{
    public const string ApplicationName = "vox-lib";

    public const string SessionCookieName = "vox_lib_session";

    public const string AntiforgeryCookieName = "vox_lib_antiforgery";

    /// <summary>
    /// Written into a cookie the page can read, and echoed back in this header.
    /// The pair is what proves a state-changing request came from this site.
    /// </summary>
    public const string AntiforgeryHeaderName = "X-XSRF-TOKEN";

    /// <summary>Readable by the page, unlike the antiforgery cookie itself.</summary>
    public const string AntiforgeryTokenCookieName = "XSRF-TOKEN";

    public const string SignedInPolicy = "SignedIn";

    public const string VerifiedBeneficiaryPolicy = "VerifiedBeneficiary";

    public const string ByClientPolicy = "account-by-client";

    /// <summary>FR-016: the number of consecutive failures a person is told about.</summary>
    public const int MaxFailedAccessAttempts = 5;

    /// <summary>FR-016: the period a person is told they must wait.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>SC-004: thirty days without signing in again.</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    /// <summary>FR-008 and FR-024: how long a confirmation or recovery link works.</summary>
    public static readonly TimeSpan LinkLifespan = TimeSpan.FromHours(24);

    /// <summary>
    /// The coarse backstop in FR-017, counted per calling address. It is
    /// deliberately loose, because the security work is done by the per-address
    /// limit and the account lockout, both of which stop five wrong guesses at
    /// one account whatever the caller does.
    /// <para>
    /// Loose because of who shares an address. A school, a library or a mobile
    /// carrier puts many people behind one, and a limit tuned as though an
    /// address were a person would refuse a classroom rather than an attacker.
    /// At this rate a scripted caller manages about seven attempts a minute,
    /// which is slow enough to be useless and fast enough that nobody sharing a
    /// connection will ever meet it.
    /// </para>
    /// </summary>
    public const int RequestsPerClientWindow = 100;

    public static readonly TimeSpan ClientWindow = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The longest address accepted, which is the longest an address can be. A
    /// longer one is rejected rather than silently truncated into somebody
    /// else's.
    /// </summary>
    public const int MaxEmailLength = 254;
}
