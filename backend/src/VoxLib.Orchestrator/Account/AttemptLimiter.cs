using System.Collections.Concurrent;
using VoxLib.Model.Account;

namespace VoxLib.Orchestrator.Account;

/// <summary>
/// A fixed window per address.
/// <para>
/// The sign-in numbers are not tuning: they are equal to the lockout the
/// identity component applies to a real account, so that an address with no
/// account is refused at the same attempt, for the same period, with the same
/// answer. If either pair changes, both must change together, or a lockout
/// starts disclosing that an address is registered.
/// </para>
/// <para>
/// The counters are held in memory and are therefore per instance. That is
/// correct while one instance runs and is recorded as a known gap in
/// quickstart.md rather than solved speculatively.
/// </para>
/// <para>
/// This is not the framework's rate limiter middleware, and the reason is worth
/// knowing: the address this limits is in the request body, and a middleware
/// partitioner would have to read the body before the endpoint does. Limiting by
/// caller stays in the middleware, where the caller's address is already known.
/// </para>
/// </summary>
public sealed class AttemptLimiter(TimeProvider clock) : IAttemptLimiter
{
    /// <summary>Equal to the identity component's MaxFailedAccessAttempts.</summary>
    public const int FailedSignInsPerWindow = 5;

    /// <summary>Equal to the identity component's DefaultLockoutTimeSpan.</summary>
    public static readonly TimeSpan SignInWindow = TimeSpan.FromMinutes(15);

    /// <summary>
    /// A person who did not receive a message asks again once, perhaps twice.
    /// More than this is somebody using the address of someone else.
    /// </summary>
    public const int MessagesPerWindow = 3;

    public static readonly TimeSpan MessageWindow = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<(string Address, AttemptKind Kind), Window> _windows =
        new();

    public AttemptDecision Check(string address, AttemptKind kind)
    {
        var (limit, _) = Allowance(kind);
        var now = clock.GetUtcNow();

        return _windows.TryGetValue(Key(address, kind), out var window)
            && window.EndsAt > now
            && window.Count >= limit
                ? AttemptDecision.Refuse(window.EndsAt - now)
                : AttemptDecision.Allow;
    }

    public AttemptDecision Consume(string address, AttemptKind kind)
    {
        var (limit, length) = Allowance(kind);
        var now = clock.GetUtcNow();

        var window = _windows.AddOrUpdate(
            Key(address, kind),
            _ => new Window(now + length, 1),
            (_, existing) =>
                existing.EndsAt <= now
                    ? new Window(now + length, 1)
                    : existing with { Count = existing.Count + 1 });

        return window.Count <= limit
            ? AttemptDecision.Allow
            : AttemptDecision.Refuse(window.EndsAt - now);
    }

    public void Clear(string address, AttemptKind kind) => _windows.TryRemove(Key(address, kind), out _);

    /// <summary>
    /// Matched the way the store matches an address, so that varying the letter
    /// case does not buy a fresh allowance.
    /// </summary>
    private static (string, AttemptKind) Key(string address, AttemptKind kind) =>
        (address.Trim().ToUpperInvariant(), kind);

    private static (int Limit, TimeSpan Length) Allowance(AttemptKind kind) => kind switch
    {
        AttemptKind.SignIn => (FailedSignInsPerWindow, SignInWindow),
        AttemptKind.SendMessage => (MessagesPerWindow, MessageWindow),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private sealed record Window(DateTimeOffset EndsAt, int Count);
}
