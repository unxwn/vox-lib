# Phase 0 Research: Accounts and Sessions

**Feature**: `specs/002-user-accounts/` | **Date**: 2026-09-09

Every unknown in the plan's Technical Context is resolved below. Package versions were checked
against nuget.org on the date above, and the ASP.NET Core behaviours against the
`aspnetcore-10.0` documentation rather than the current one, because two of them changed in
.NET 11 and this project targets .NET 10.

## R1. How Identity is wired in without its screens or its endpoints

**Decision**: `AddIdentityCore<AccountDao>()` with `.AddSignInManager()`,
`.AddDefaultTokenProviders()` and `.AddEntityFrameworkStores<VoxLibDbContext>()`, and cookie
authentication registered separately with
`AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies()`. No
`AddIdentity`, no `AddDefaultIdentity`, no `MapIdentityApi`, no
`Microsoft.AspNetCore.Identity.UI` package. The endpoints are hand-written minimal APIs under
`/api/account/`.

**Rationale**: The spec asks for Identity as a user store, a password hasher, a lockout
counter and a token generator, and for nothing else. `AddIdentityCore` is exactly that set;
`AddIdentity` additionally registers role services and three role tables that a
claims-based access model never reads. `MapIdentityApi` is the wrong shape twice over: it
issues bearer tokens rather than a cookie session, and its response bodies and error wording
are fixed, so FR-005 and FR-034 could not both be satisfied through it. The Identity UI Razor
pages are ruled out by the spec and by Principle I: they are generic scaffolding, they are not
Ukrainian, and the accessibility of the account screens is the thing this feature is measured
on.

**Alternatives considered**:

- `MapIdentityApi` plus a cookie option. It does support cookies, but the endpoint set, the
  status codes and the message text are not ours, and every one of FR-005, FR-011, FR-016 and
  FR-019 is a statement about exactly those.
- A hand-written user store with a hand-rolled hasher. Rejected outright. Password hashing,
  lockout counting and token generation are the three things in this feature where a mistake
  is invisible until it matters, and the platform ships a reviewed implementation of all
  three.
- `AddIdentity` for symmetry with the common samples. Rejected because roles are unused: the
  single distinction this product has today is beneficiary status, which is one claim.

## R2. How the session is held, and how it is ended before it expires

**Decision**: The Identity application cookie, `HttpOnly`, `SameSite=Strict`, `Secure` outside
development, with `ExpireTimeSpan` of 30 days and `SlidingExpiration` enabled. Validity is
rechecked against the database on every request by setting
`SecurityStampValidatorOptions.ValidationInterval` to `TimeSpan.Zero`. Ending a session from
the server is `UserManager.UpdateSecurityStampAsync`, which is also what `ResetPasswordAsync`
does on its own.

**Rationale**: This is where four requirements meet. FR-013 wants a session page scripts
cannot read, which is what `HttpOnly` means and what a token in `localStorage` is not. FR-012
and SC-004 want thirty days across a browser restart, which is a persistent cookie with a
sliding expiry. FR-014 and FR-022 want a session that can be ended from the server and stops
working on its next use, and SC-006 measures that as 100% of attempts. Identity revalidates
the cookie against the stored security stamp on a timer whose default is thirty minutes; with
that default, a person who recovers an account taken by someone else leaves the taker signed
in for up to half an hour, which fails SC-006 as written. Setting the interval to zero makes
the check happen on every request. The cost is one indexed read per authenticated request,
which is the right trade for a product whose access rules are about to gate copyrighted
audio.

The same mechanism does a second job for free. The revalidation rebuilds the principal from
the store, so the beneficiary claim described in R4 is refreshed on every request rather than
frozen at sign-in. Beneficiary status can be withdrawn, and a claim baked into a thirty-day
cookie could not honour that.

**Alternatives considered**:

- A self-contained token, JWT or otherwise, carrying its own validity. Rejected by the spec's
  own reasoning and confirmed here: FR-014 and FR-022 both require ending a session before it
  expires, and a token that validates itself cannot be withdrawn without a revocation list,
  which is the database read this design does anyway, minus the simplicity.
- The default thirty-minute validation interval, accepting the window. Rejected because
  SC-006 says 100% of attempts on the next use, and because the window is exactly the case
  the requirement exists for.
- A server-side session table keyed by an opaque cookie. It would work, but it is a second
  mechanism beside the security stamp Identity already maintains, and it would have to be
  written and tested here.

## R3. Where the data protection keys live

**Decision**: `PersistKeysToDbContext<VoxLibDbContext>()` from
`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.12, with `SetApplicationName`
pinned to a constant so the purpose strings stay stable, and a `data_protection_keys` table in
the same migration as the account tables.

**Rationale**: This is the quiet failure that would otherwise be found in production. Data
protection encrypts the session cookie and every confirmation and recovery link, and its
default key store is the file system under the user profile. `CLAUDE.md` records that
`/home/vscode` is overlayfs and is erased on container rebuild, so in development every
rebuild would sign everyone out and void every unfollowed link, and the symptom would look
like a bug in this feature rather than a missing key ring. In production the same applies to
any container that is replaced, and it becomes worse the moment there is more than one
instance, because two instances with different key rings cannot read each other's cookies.
The database is already there, already backed up with the accounts it protects, and shared by
every instance.

**Alternatives considered**:

- A named volume for the key directory. Fixes development and nothing else, and adds a fifth
  volume to the Compose file for a problem the database already solves.
- Leaving the default and treating it as a deployment concern. Rejected because the failure is
  silent, arrives at the worst time, and would be blamed on the wrong code.

## R4. Beneficiary status as a claim, and the policy over it

**Decision**: `AccountDao` carries `IsVerifiedBeneficiary`, defaulting to false. A
`UserClaimsPrincipalFactory<AccountDao>` subclass in `VoxLib.Dal` adds a
`voxlib:beneficiary` claim when it is true. `VoxLib.Api` registers two policies at the
composition root: `SignedIn`, which requires an authenticated principal, and
`VerifiedBeneficiary`, which requires the claim. This feature applies neither to any endpoint,
because it gates nothing yet.

**Rationale**: FR-025 and FR-026 ask for the status to exist and to be available to access
decisions without changing how accounts are stored later. A claim computed from the account on
every principal build satisfies both: the audio feature adds
`.RequireAuthorization("VerifiedBeneficiary")` to one endpoint and nothing else moves. Roles
were considered and rejected in R1. Registering the policies now, unused, is deliberate and
cheap: it is what makes the later change one line, and a policy with no endpoint attached
costs nothing at runtime.

The claim is not stale, for the reason given in R2: the security stamp revalidation rebuilds
the principal on every request, so withdrawing beneficiary status takes effect on the account's
next request rather than in thirty days.

**Alternatives considered**:

- Reading the status from the database inside each authorisation handler. Equivalent in
  freshness and slightly more code, and it puts a database call inside the authorisation
  pipeline rather than in the one place that already makes one.
- A `Listener` looked up per request through the endpoint filter. Rejected as a second
  identity mechanism beside the principal the framework already builds.

## R5. Not revealing whether an address has an account

**Decision**: Four separate measures, because the requirement is about four different
channels.

1. Registration always answers `202 Accepted` with an empty body, whether the address was new
   or already had an account. When it already had one, a message goes to that address saying
   an account already exists and offering the recovery link, so the response is identical and
   the person who actually owns the address is still told something useful.
2. Sign-in with an unknown address still runs a password verification, against a fixed dummy
   hash computed once at startup, so the time taken does not distinguish an unknown address
   from a wrong password.
3. Unknown address and wrong password both return `401` with the same body and the same
   Ukrainian wording. "The address must be confirmed first", FR-011, is returned only after
   the supplied password has verified, which is the single exception FR-005 allows.
4. Lockout and the per-address rate limit both return `429` with the same `Retry-After`
   header and the same body.

**Rationale**: Points 1 to 3 are standard and follow directly from FR-005 and FR-015. Point 4
resolves a real tension inside the spec: FR-016 requires telling a person they are locked out
and when they may try again, and a lockout message on its own is a disclosure that the account
exists, which FR-005 forbids. The resolution is that the per-address rate limiter FR-017
already requires produces the same refusal for an address with no account, so an attacker
guessing against an unknown address sees the same `429` and the same wait as one guessing
against a real account. Both requirements are met as written; neither is relaxed.

This is why the counter for the rate limiter is partitioned by the submitted address rather
than only by client address. Partitioning by client address alone would leave the unknown
address answering `401` while the real one answered `429`, which is the disclosure.

**Alternatives considered**:

- A deliberate fixed delay on every sign-in response. It hides timing but wastes a request
  thread and is easy to get wrong under load. The dummy hash costs the same work the real
  path costs, which is the point.
- Refusing a locked-out account with the same `401` as a wrong password, and saying nothing
  about the wait. Rejected: it satisfies FR-005 by breaking FR-016, and it leaves a person who
  is genuinely locked out with no idea why their correct password stopped working.

## R6. Cross-site request forgery against a cookie session on a JSON API

**Decision**: Three layers. The session cookie is `SameSite=Strict`. No state-changing
endpoint answers `GET`. And every state-changing endpoint validates an antiforgery token,
obtained from `GET /api/account/antiforgery-token`, which calls
`IAntiforgery.GetAndStoreTokens` and writes the request token into a readable `XSRF-TOKEN`
cookie; the frontend echoes it in the `X-XSRF-TOKEN` header, configured through
`AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN")`.

**Rationale**: On .NET 10, `UseAntiforgery` validates only endpoints that read form data. An
endpoint that binds its body from JSON is not rejected on a cross-origin request, so the
middleware alone protects nothing here. .NET 11 adds an automatic middleware that judges
requests by `Sec-Fetch-Site` and `Origin`, but this project targets .NET 10, so the
token-based system is the one available. The Microsoft guidance for exactly this shape, a
JavaScript client with a cookie session, is the token-in-a-header pattern above.

`SameSite=Strict` is chosen over `Lax` because it costs nothing here. Strict withholds the
cookie on a top-level navigation arriving from another site, which would normally show a
signed-in person as signed out when they follow a link into the site. The spec's last
assumption removes that cost: the account pages are not generated ahead of time and whether
someone is signed in is established after the page loads, and that request is same-site
because the page's own origin is the site. The one place it matters is the confirmation and
recovery links, which arrive from a mail client, and those are followed by people who are not
signed in.

The antiforgery endpoint is anonymous, unlike the sample in the documentation, which requires
authorisation. Registration and sign-in are state-changing requests made by someone with no
session, and sign-in is the classic target of the attack where a victim is logged into the
attacker's account.

**Alternatives considered**:

- `SameSite=Strict` alone. Defensible, and it is the layer doing most of the work, but it is a
  single browser-enforced control on the mechanism that guards every account in the product.
- Checking `Origin` by hand. This is what .NET 11 does properly; hand-rolling it a version
  early means owning the edge cases the framework will handle next year.

## R7. Rate limiting

**Decision**: The rate limiter already in the shared framework, `AddRateLimiter`, with two
named policies. `by-address` is a fixed window partitioned by the submitted email address,
applied to sign-in, resend and recovery. `by-client` is a fixed window partitioned by the
remote address, applied to those three and to registration. A rejected request answers `429`
with `Retry-After`, matching the lockout response described in R5.

**Rationale**: FR-017 asks for a limit on registration, sign-in, resend and recovery from a
single source, so that per-account lockout is not the only defence against attempts spread
across many addresses. The framework limiter is in the box, needs no package, and its
partitioning is exactly the two axes the requirement names. Partitioning by address as well as
by client is not redundancy: it is what makes FR-009 true, since the mail-resend limit is a
limit per address by definition, and it is what makes the lockout indistinguishable from the
limit in R5.

The limiter is in-memory and therefore per instance. That is honest and sufficient for one
instance, and is recorded in `quickstart.md` as a known gap rather than solved speculatively.

**Alternatives considered**:

- A distributed limiter backed by the database or a cache. Solves a problem the product does
  not have yet, at the cost of a store to run and a failure mode when it is unreachable.
- Relying on lockout alone. Explicitly rejected by FR-017, and it is the weaker half: lockout
  cannot see an attacker trying one password against ten thousand addresses.

## R8. Single-use, time-limited links

**Decision**: Identity's default token providers, which are `DataProtectorTokenProvider`
instances. `GenerateEmailConfirmationTokenAsync` and `GeneratePasswordResetTokenAsync`
produce the tokens; `DataProtectionTokenProviderOptions.TokenLifespan` is set to 24 hours for
both. The link carries the account id and the token, both base64url encoded, and points at a
frontend route, which posts them to the API.

**Rationale**: These tokens carry the account's security stamp inside them, which is what
makes them behave as single-use without a table of issued links to maintain. Setting a
password rotates the stamp, so a recovery token cannot be replayed and, per FR-022, every
existing session dies at the same moment for the same reason. Confirmation is single-use for a
different reason and needs to be handled explicitly, because confirming an address does not
rotate the stamp: the handler checks whether the address is already confirmed before it
validates the token, so a second visit answers "already confirmed" rather than an error, which
is exactly the distinction FR-008 draws. An expired token is told apart from a used one by
that same order of checks, and is offered a replacement.

FR-023 says a password set through recovery also confirms the address, since following a link
sent there proves what confirmation proves. That is one extra call in the recovery
orchestrator, and it removes the dead end where somebody who never confirmed can recover but
still cannot sign in.

**Alternatives considered**:

- A table of issued links with an explicit consumed flag. More obvious to read, and it is the
  design if the tokens did not already carry the stamp. Rejected as a second mechanism for
  something the platform already does correctly.
- A shorter lifespan, an hour or two. Rejected for this audience: a person navigating by
  screen reader who reads mail on a different device should not lose the link because they
  came back after lunch, and every flow already offers a replacement.

## R9. Mail transport

**Decision**: `IEmailSender` declared in `VoxLib.Model`, implemented in the new
`VoxLib.Platform` project by `SmtpEmailSender` over MailKit 4.17.0, configured entirely from
`Smtp:*` configuration values: host, port, whether to start TLS, user, password, and the from
address. No provider SDK. Mailpit joins the dev container as the development host.

**Rationale**: The spec requires the provider to be chosen at plan time and makes delivery a
hard dependency, because nobody can sign in before a message arrives. Plain SMTP keeps that a
deployment decision rather than a code decision: Resend, Brevo, Postmark and Amazon SES all
speak SMTP, so choosing between them later changes configuration and nothing else. MailKit is
the library Microsoft's own `System.Net.Mail.SmtpClient` documentation points to by name, and
`SmtpClient` has been obsolete since .NET 5.

Mailpit is the development host because confirmation is on the critical path: without
somewhere for mail to go, not one flow in this feature can be walked by hand. It listens for
SMTP on 1025 and serves a web interface on 8025. In this Compose file the `app` service uses
`network_mode: service:db`, so every service shares one network namespace; the Mailpit service
must join it the same way, and then the API reaches it at `localhost:1025` and a browser at
`localhost:8025`. Adding it as an ordinary service on a bridge network instead would leave it
unreachable from the API, and the symptom would be a connection refused at the moment a
message is sent.

**Alternatives considered**:

- A provider SDK now. It buys delivery telemetry and templates, neither of which this feature
  uses, and it puts a vendor-shaped code path into `VoxLib.Platform` before there is a vendor.
- Writing messages to the log in development instead of running Mailpit. Cheaper, but the
  thing being tested by hand is a link in a message, and reading it out of a log is worse than
  reading it out of a mailbox, especially for a person using a screen reader.

## R10. Where the account screens live in a prerendered frontend

**Decision**: The five account routes are added to `routes.ts` and deliberately left out of
the `prerender` list in `react-router.config.ts`. Because `/` is prerendered, React Router
writes the single-page fallback to `build/client/__spa-fallback.html`, and the static host must
serve that file for any path it does not have a document for.

**Rationale**: The spec's closing assumption says these pages are personal to one person and
are not generated ahead of time as the catalogue pages are. Not listing them is therefore
correct rather than an omission, and it also gives FR-028 its first half for free: a page that
was never generated cannot be indexed. React Router's documented behaviour for `ssr: false`
with a `prerender` list is exactly this: prerendered paths get their own document, everything
else is served the fallback and hydrates as a single-page application.

The consequence is a new deployment requirement, and it is worth naming because nothing in
the repository serves the built frontend yet: whatever ends up hosting `build/client` has to
fall back to `__spa-fallback.html` rather than answering 404. This is recorded in
`quickstart.md` as a known gap for the deployment feature, not solved here.

**Alternatives considered**:

- Prerendering the account routes as empty shells. They would be indexable, which FR-028
  forbids, and they would carry no content worth prerendering.
- A second frontend application for the account screens. It would need its own build, its own
  styles and its own accessibility run, to serve five forms.

## R11. Keeping account pages out of search indexes

**Decision**: Both halves. Each account route declares `<meta name="robots" content="noindex">`
through its `meta` export, and the API sends `X-Robots-Tag: noindex` on every response under
`/api/account/`.

**Rationale**: FR-028 and Principle IV. The constitution is explicit that `robots.txt` is
advisory and is never the protection, and that app routes carry the header. The meta tag
covers the case where a crawler reaches the page through the single-page fallback, and the
header covers the API responses. Neither is the access control; that is the session.

## R12. Lockout, password policy, and the numbers this feature has to state

**Decision**: Lockout after 5 consecutive failures, for 15 minutes, with lockout enabled for
new accounts. Passwords require at least 12 characters and nothing else: no required digit, no
required uppercase, no required symbol. Token lifespan 24 hours. Session 30 days, sliding. The
password numbers are served by `GET /api/account/password-policy` so the frontend states them
from one source rather than a second copy in Ukrainian prose.

**Rationale**: FR-002 requires the requirements to be stated before submission and named on
rejection, and FR-016 requires a stated number and a stated period, so these are requirements
rather than tuning. Length alone, with a floor well above Identity's default of six, is the
current guidance and it matters more than usual here: character-class rules push people toward
short passwords full of substitutions, and they are painful to type on a phone with a screen
reader. The spec's edge cases ask about a password written entirely in Cyrillic and one
containing an emoji; a length-only policy over Unicode accepts both, which is the correct
answer for a Ukrainian audience and is a test in this feature.

The endpoint for the policy exists because the alternative is the same numbers written twice,
once in `Program.cs` and once in a Ukrainian sentence in a React component, drifting the first
time either changes. It is nine lines and it makes FR-002 a property of the system rather than
a promise.

**Alternatives considered**:

- Identity's defaults: six characters, a digit, an uppercase, a lowercase and a symbol.
  Rejected on both length and class rules, for the reasons above.
- A permanent lockout needing manual intervention. Rejected: the spec is explicit that there
  is no administrative capability, so a permanent lockout is a destroyed account.

## R13. Proving it works

**Decision**: Endpoint tests as in 001, with two additions to the harness. `RecordingEmailSender`
replaces `IEmailSender` in the test host and keeps the messages it was asked to send, so a test
extracts the link and follows it. `AccountApiFixture` shares one `CookieContainer` between
`HttpClient` instances, so a test can throw a client away and build another one and have that
mean "closed the browser and reopened it", which is what FR-012 and SC-004 are about.
Accessibility outcomes are covered by a Playwright specification over the five screens.

**Rationale**: Principle VI: behaviour over mocks, verified over real HTTP against a real
database. The email sender is the one thing that has to be substituted, because the alternative
is either sending real mail from continuous integration or asserting nothing about the message
that the whole feature depends on. Substituting it at the boundary the interface already
defines is not a mock-heavy test; the rest of the journey, the hashing, the stamp, the cookie
and the token, all run for real.

Continuous integration needs no new service. Mailpit is a development convenience, not a test
dependency, so `ci.yml` is unchanged and the job names that the branch-protection ruleset
matches stay as they are.

**Alternatives considered**:

- Mailpit in continuous integration, with tests reading its API. It exercises real SMTP, but
  it adds a service to a workflow whose job names are load-bearing, and it makes every test
  that involves a link depend on a second process.
- Asserting only that sending was attempted. It would leave the link itself, the thing every
  one of user stories 2 and 4 turns on, untested.

## R14. Ending one session without ending the others

**Decision**: The session lives on the server. `CookieAuthenticationOptions.SessionStore` is
given an `ITicketStore` implementation in `VoxLib.Api` that keeps the encrypted ticket in a
`sessions` table through an `ISessionStore` declared in `VoxLib.Model`, so the cookie carries
only an unguessable key. Signing out deletes that row.

**Rationale**: This was not in the plan and was found by the test for FR-018's second
acceptance scenario, which is worth recording as an argument for writing that test.

FR-018 makes two demands at once. A session captured before signing out must be refused
afterwards, and signing out on one device must leave another device signed in. Nothing about a
self-contained cookie satisfies the first: the encrypted ticket stays valid until it expires,
however many times its owner signs out, because signing out only tells the browser to forget
it. Rotating the account's security stamp satisfies the first and breaks the second, because a
stamp belongs to an account and rotating it ends every session that account has. Only
per-session state satisfies both, and the framework's ticket store is the supported way to
hold it.

It costs a read per authenticated request. That is genuinely cheap here, because R2 already
pays for a read of the account on every request, so this joins work that was happening anyway
rather than adding a round trip. It also makes the cookie carry no identity at all, which is
strictly better than an encrypted ticket in a browser.

The rows are swept when a session is created rather than by a background service. A scheduled
sweep is the right answer once there is enough traffic to need one; until then it is a service
to run and watch, for a table that gains one row per sign-in.

**Alternatives considered**:

- Rotating the security stamp on sign out. One line, no table, and it fails the acceptance
  scenario about two devices. It would also mean signing out at a library computer signs a
  person out on their phone, which is precisely backwards.
- A revocation list of ended sessions. Same amount of machinery, weaker: it grows without
  bound, needs its own cleanup, and leaves the identity inside the cookie.
- Accepting that a captured cookie keeps working until it expires. Rejected: the spec says
  otherwise in as many words, and a thirty-day session makes the window thirty days.

## R15. Where the per-address limit is applied

**Decision**: The framework's rate limiter middleware carries the caller half of FR-017,
partitioned by remote address. The address half is an `IAttemptLimiter` declared in
`VoxLib.Model` and applied inside the orchestrators, not in the middleware.

**Rationale**: The plan said both halves would be rate limiter policies. They cannot be. The
address a request is about is in its body, and a middleware partitioner would have to read the
body before the endpoint does, which either consumes it or requires buffering every request to
recover it. Applying it where the address is already parsed is simpler and testable.

Two details of it are requirements rather than tuning, and they are commented as such in the
code. Its sign-in threshold and window are equal to the lockout the identity component applies,
because R5 depends on an address with no account being refused at the same attempt, for the
same period, with the same answer. And a failed attempt consumes an allowance while a
successful one clears it, because those are the semantics a lockout counter has, and two
counters that move on different events drift apart until the difference is visible from
outside.

Recovery clears it too. That was also found by a test: the store clears the account's own
lockout for FR-021, and knows nothing about the attempts counted against the address, so
without the extra line a person recovered their account and was still refused for another
quarter of an hour, which is the exact experience that requirement exists to prevent.

**Alternatives considered**:

- Buffering every request body so a middleware partitioner can read the address. Possible, and
  it puts a cost on every request in the product to move ten lines from one file to another.
- Limiting only by caller. Explicitly rejected by FR-017, and it would break R5: the unknown
  address would answer 401 while the real one answered 429, which is the disclosure.
