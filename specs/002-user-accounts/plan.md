# Implementation Plan: Accounts and Sessions

**Branch**: `feature/user-accounts` | **Date**: 2026-09-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-user-accounts/spec.md`

## Summary

Give the product an identity to authorise against. A visitor registers with an email address
and a password, confirms the address by following a link sent to it, signs in, stays signed in
for thirty days across browser restarts, recovers a forgotten password, and signs out. Every
screen is Ukrainian, and every screen works with a screen reader and with the keyboard alone.
The catalogue stays fully readable with no account at all.

The approach follows from the two clarifications and from the spec's own assumptions.
ASP.NET Core Identity supplies the user store, the password hasher, the lockout counter and
the link tokens; none of Identity's ready-made screens or endpoints are used, because
`MapIdentityApi` returns bearer tokens rather than a cookie session and the Identity UI Razor
pages do not meet the accessibility bar this product is held to. The session is the Identity
application cookie, which the browser cannot read from script, and its validity is rechecked
against the database on every use rather than on a timer, because FR-014 and FR-022 both
require ending a session before it expires and SC-006 measures that on the session's next use.

Three pieces of infrastructure arrive with this feature and are the plan's real cost.
`VoxLib.Platform` is created for the first time, holding an SMTP sender behind an interface
declared in `VoxLib.Model`, so the mail provider stays a configuration value. Mailpit joins
the dev container so development never sends real mail. Data protection keys move into the
database, because they encrypt both the session cookie and every confirmation and recovery
link, and the default file-system store sits on the container's overlayfs, which
`CLAUDE.md` records as erased on rebuild.

## Technical Context

**Language/Version**: C# on .NET 10, SDK 10.0.400. TypeScript 7.0 on Node 24.

**Primary Dependencies**: ASP.NET Core minimal APIs; `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
10.0.12 and `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.12, both matching the
Entity Framework Core 10.0.12 already pinned in `VoxLib.Dal`; MailKit 4.17.0 for SMTP;
the rate limiter and the antiforgery service already in the ASP.NET Core shared framework;
React 19.2 and React Router 8.3.1 unchanged.

**Storage**: The existing PostgreSQL 18 database gains the Identity tables, renamed to the
snake_case convention the catalogue tables already use, plus a table of data protection keys.

**Testing**: xunit with `WebApplicationFactory<Program>` over real HTTP against a real
database, as in 001. The mail sender is replaced in the test host by a recording one, so a
test reads the link out of the message it captured and follows it, which is the same journey
a person makes. Playwright 1.63 with `@axe-core/playwright` covers the accessibility outcomes.

**Target Platform**: One origin serving prerendered catalogue documents, a single-page
fallback for the account screens, and the .NET API. Browsers running VoiceOver on iOS and
TalkBack on Android are the platforms that matter.

**Performance Goals**: No new goal. The one cost worth naming is that rechecking the session
against the database on every use adds a single indexed read per authenticated request, which
is the price of SC-006 and is paid deliberately.

**Constraints**: The session cookie is `HttpOnly` and `SameSite=Strict`, so page scripts
cannot read it and another site cannot cause it to be sent. No state-changing request uses a
method a browser follows from a link. No response, and no measurable difference in the time
taken to produce one, reveals whether an address has an account, except once the correct
password for that account has been supplied. Every account screen and every message is
Ukrainian. No audio anywhere.

**Scale/Scope**: Accounts in the low thousands. Eight paths under `/api/account/` carrying ten operations, five
frontend routes, one new backend project.

No unresolved unknowns remain. Phase 0 is recorded in [research.md](./research.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against the six gates in `.specify/memory/constitution.md`.

**1. Accessibility.** PASS. All five user stories carry screen reader and keyboard-only
acceptance scenarios, and the design answers each one concretely. Every field is a native
control with a real `<label>`; a rejected field carries `aria-invalid` and points at its own
message through `aria-describedby`, which is FR-031; the summary of failures is a polite live
region and focus moves to it, which is FR-033 and the reason SC-001 is achievable; the email
and password fields carry the conventional `autocomplete` tokens so a password manager fills
and saves them, which is FR-032. Media session does not apply: this feature has no playback.
SC-001 through SC-003 are verified by the accessibility run described in R13, not by
inspection.

**2. Dependency rule.** PASS, with one deliberate placement recorded below. `VoxLib.Model`
gains `Account/` and still references no other VoxLib project; it declares `Listener`,
`IAccountStore`, `IAccountLinks`, `IAccountSession`, `IAccountMessages` and `PasswordPolicy`.
`VoxLib.Dal` implements the store and the links over Identity, `VoxLib.Platform` implements
mail delivery over SMTP, and `VoxLib.Orchestrator` holds the rules: what registering does when
the address is already taken, when a sign-in is refused and with which of the three reasons,
what recovery does to a lockout and to existing sessions. Endpoint handlers parse, delegate
and shape.

The placement: `IAccountSession`, the two lines that actually write and clear the cookie, is
implemented in `VoxLib.Api`. Issuing a cookie needs `HttpContext`, and `SignInManager` lives
in the ASP.NET Core shared framework, so implementing it anywhere else means giving a class
library a framework reference to carry a transport concern. The interface is still declared in
`VoxLib.Model`, so the orchestrator names the intent and not the mechanism.

**3. Audio and access.** PASS, and mostly by construction. This feature adds no audio field,
no storage interface and no signed URL; it exists so that the audio feature has something to
authorise against. `Listener.IsVerifiedBeneficiary` is the field FR-025 requires and it
defaults to false for every account, so the rule that will read it later is a change to one
access rule rather than a migration. FR-027 is proved rather than assumed: the existing
`AnonymousAccessTests` keep passing, and a new test walks every catalogue endpoint with no
cookie after the account endpoints exist.

**4. HTTP.** PASS. Eight resource-shaped paths under `/api/account/`, ten operations in all, listed in
`contracts/accounts.yaml`. `/health` is untouched, the catalogue routes are untouched, and the
frontend calls relative paths only. The browser session is a resource: `POST` creates it,
`GET` reads it, `DELETE` ends it.

**5. Tests.** PASS. Every behaviour names the endpoint test that proves it, and the mapping is
in `quickstart.md`. The ones that matter most: an address that already has an account produces
a byte-identical response to a new one; an unconfirmed account cannot be signed in to; the
cookie survives a new `HttpClient` built from the same cookie container, which is what
"closed the browser" means to a test; recovering a password refuses the cookie that existed
beforehand on its very next request; a signed-out cookie replayed afterwards is refused.

**6. Abstractions.** PASS. The plan adopts nothing from the deferred list in Principle VII: no
aggregate roots, no domain events, no specifications, no CQRS, no MediatR. Authorisation is a
policy over a claim rather than a role, which is what the spec asks for and is also the
smaller of the two: there is exactly one distinction today and roles would add three tables
to hold it. `VoxLib.Platform` is created because the constitution names it and it now has
work to do.

### Post-implementation note

Two design decisions changed during implementation, both because a test failed rather than
because the design was reconsidered. They are recorded as R14 and R15 in `research.md`, and
`data-model.md` reflects them.

R14: the session moved onto the server, in a `sessions` table behind an `ISessionStore`
declared in `VoxLib.Model`. FR-018 requires both that a captured session be refused after
signing out and that other devices stay signed in, and no property of a self-contained cookie
or of the account's security stamp provides both. This adds one table and one read per
authenticated request, joining the read R2 already pays for.

R15: the per-address half of FR-017 is applied in the orchestrators rather than as a rate
limiter policy, because the address it keys on lives in the request body and a middleware
partitioner would have to read the body before the endpoint does.

### Post-design re-check

Re-evaluated after the Phase 1 artifacts were written. All six gates still pass. One tension
inside the spec surfaced during design and is resolved rather than waived: FR-016 requires
telling a person that they are locked out and when they may try again, while FR-005 forbids
revealing whether an address has an account, and a lockout message plainly reveals one. The
resolution is in R5. Both cases return the same `429` with the same `Retry-After`, because the
per-address rate limiter FR-017 already requires produces that response for an address with no
account, so the two are indistinguishable from outside. No requirement is relaxed.

## Project Structure

### Documentation (this feature)

```text
specs/002-user-accounts/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── accounts.yaml    # Phase 1 output, OpenAPI 3.1
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output, created by /speckit-tasks
```

### Source Code (repository root)

```text
backend/
├── VoxLib.slnx                          # VoxLib.Platform added here
├── src/
│   ├── VoxLib.Api/
│   │   ├── Program.cs                   # Identity, cookie, policies, rate limiter,
│   │   │                                # antiforgery, and the new registrations
│   │   └── Account/
│   │       ├── AccountEndpoints.cs      # registration, confirmation, recovery
│   │       ├── SessionEndpoints.cs      # POST, GET and DELETE of the session
│   │       ├── HttpAccountSession.cs    # IAccountSession over SignInManager
│   │       └── Contracts/
│   │           ├── Requests/            # Registration, SignIn, Confirmation,
│   │           │                        # RecoveryRequest, PasswordReset
│   │           └── Responses/           # Session, PasswordPolicy
│   ├── VoxLib.Model/
│   │   └── Account/                     # Listener, IAccountStore, IAccountLinks,
│   │                                    # IAccountSession, IAccountMessages,
│   │                                    # PasswordPolicy, LinkPurpose, the outcomes
│   ├── VoxLib.Orchestrator/
│   │   └── Account/                     # Registration, EmailConfirmation, SignIn,
│   │                                    # PasswordRecovery: the rules
│   ├── VoxLib.Dal/
│   │   ├── Account/                     # AccountDao, AccountStore, AccountLinks,
│   │   │                                # ListenerClaimsFactory, mapping to Listener
│   │   └── Persistence/                 # VoxLibDbContext now an IdentityUserContext,
│   │                                    # DataProtectionKeyContext, new migration
│   └── VoxLib.Platform/                 # NEW project
│       └── Email/                       # SmtpEmailSender over MailKit, options
└── tests/VoxLib.Api.Tests/
    ├── Account/                         # registration, confirmation, sign-in,
    │                                    # lockout, recovery, sign-out, enumeration,
    │                                    # antiforgery, rate limit
    └── Infrastructure/                  # AccountApiFixture, RecordingEmailSender

frontend/
├── react-router.config.ts               # account paths stay out of prerender
├── src/
│   ├── account/                         # session hook, form state, Ukrainian text
│   ├── api/account.ts                   # the account calls, plus the token header
│   ├── components/                      # FormField, StatusRegion, SessionMenu
│   ├── routes/                          # register, sign-in, confirm,
│   │                                    # forgot-password, reset-password
│   └── styles/account.css
└── tests/a11y/account.spec.ts

.devcontainer/docker-compose.yml         # mailpit service on the shared network stack
```

**Structure Decision**: Web application, unchanged from 001 except that `VoxLib.Platform`
is created, which brings the backend to the full five-project shape the constitution names.
The account screens are ordinary routes in the existing frontend rather than a separate
application; what makes them different is that they are not prerendered, so they are served
by the single-page fallback the build already emits.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified

No violations. The plan adopts no deferred pattern from Principle VII, and the one project it
adds is one the constitution already names.

Four costs are recorded here because they are the largest in the plan and the obvious places
to push back:

| Addition | Why needed | What it costs |
| --- | --- | --- |
| Rechecking the session against the database on every use, rather than on Identity's default thirty-minute timer | FR-014 and FR-022 require a session to stop working before it expires, and SC-006 measures it on the session's next use. A timer means a window of up to thirty minutes in which a recovered account is still held by whoever took it | One indexed read per authenticated request |
| Data protection keys in the database | The keys encrypt the session cookie and every confirmation and recovery link. The default file-system store is under `/home/vscode`, which `CLAUDE.md` records as erased on container rebuild, and would be ephemeral in production too, so every restart would sign everyone out and void every unfollowed link | One table, one context, one package |
| Mailpit in the dev container | Confirmation is required before the first sign-in, so nothing in this feature can be exercised by hand without somewhere for mail to go, and the alternative is sending real mail from a development machine | A fourth service in Compose, and a container rebuild that nothing in continuous integration verifies |
| Identity's unused columns on the account table | `IdentityUser` brings phone number and two-factor columns that this feature does not use. Trimming them means a custom store rather than the Entity Framework one, which is a large amount of code to avoid five nullable columns | Five columns that are always empty, and a note in `data-model.md` saying so |
