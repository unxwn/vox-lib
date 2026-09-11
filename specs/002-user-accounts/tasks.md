---
description: "Task list for Accounts and Sessions"
---

# Tasks: Accounts and Sessions

**Input**: Design documents from `specs/002-user-accounts/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/accounts.yaml](./contracts/accounts.yaml)

**Tests**: Included. Principle VI of the constitution requires an endpoint test for every
behaviour change, and Principle I is not satisfied by automation alone, so each story also
carries a manual pass on a real screen reader. This feature adds a second reason: registering,
signing in and recovering are the three places where a mistake is invisible until it matters,
so the tests here are the deliverable as much as the endpoints are.

**Organization**: Grouped by user story. Stories are implemented **sequentially**, in priority
order. They are sequential twice over: they share `Program.cs`, `AccountEndpoints.cs`,
`routes.ts` and the generated `schema.d.ts`, and the spec's own ordering means an account has
to exist before it can be confirmed, be confirmed before it can be signed in to, and be
signed in to before signing out means anything. Parallelism inside a story is still available
and marked.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel, different files and no dependency on incomplete work
- **[Story]**: Which user story the task serves
- File paths are exact

## Path Conventions

Web application. Backend under `backend/src/` and `backend/tests/`, frontend under
`frontend/src/`, per the structure decision in `plan.md`.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 [P] Add a `mailpit` service to `.devcontainer/docker-compose.yml` using `axllent/mailpit`, with `network_mode: service:db` so it shares the one network namespace the `app` service already joins, and `MP_SMTP_AUTH_ACCEPT_ANY` and `MP_SMTP_AUTH_ALLOW_INSECURE` set for development. Forward 8025 in `.devcontainer/devcontainer.json`. Without the shared namespace the API cannot reach it and the failure appears as a connection refused at the moment a message is sent, per R9
- [X] T002 Create `backend/src/VoxLib.Platform/VoxLib.Platform.csproj`, register it in `backend/VoxLib.slnx`, reference `VoxLib.Model` from it and nothing else, and add `MailKit` 4.17.0. Add the project reference from `VoxLib.Api` only, so it is reached at the composition root as `VoxLib.Dal` already is
- [X] T003 Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.12 and `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.12 to `backend/src/VoxLib.Dal/VoxLib.Dal.csproj`, matching the Entity Framework Core 10.0.12 already pinned there
- [X] T004 [P] Add the `Smtp` configuration section to `backend/src/VoxLib.Api/appsettings.json` and `appsettings.Development.json`: host, port, whether to start TLS, user, password and the from address, with development pointing at Mailpit on `localhost:1025`. No provider name appears in code, per R9
- [X] T005 [P] Add the application base address used to build confirmation and recovery links to `backend/src/VoxLib.Api/appsettings.json` and `appsettings.Development.json`, defaulting to `http://localhost:5173`. A link in a message has no request to be relative to, so this is the one place an origin is configured

---

## Phase 2: Foundational (Blocking Prerequisites)

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T006 [P] Create the domain identity in `backend/src/VoxLib.Model/Account/Listener.cs`: `AccountId`, `Email` and `IsVerifiedBeneficiary`, a static `Anonymous` instance so that nobody being signed in is a value rather than a null, and `IsSignedIn` on the type itself
- [X] T007 [P] Create `backend/src/VoxLib.Model/Account/PasswordPolicy.cs` holding the single source of truth from R12: minimum length 12, no character class required. Length is counted over Unicode so a password written entirely in Cyrillic or containing an emoji is accepted
- [X] T008 [P] Create the outcome types in `backend/src/VoxLib.Model/Account/`: `RegistrationOutcome`, `SignInOutcome` carrying the moment a locked-out person may try again, `ConfirmationOutcome`, `RecoveryOutcome` and `LinkPurpose`. `RegistrationOutcome` deliberately has no "already exists" value, per FR-005
- [X] T009 Declare the interfaces in `backend/src/VoxLib.Model/Account/`: `IAccountStore`, `IAccountLinks`, `IAccountMessages` and `IAccountSession`, plus `IEmailSender` in `backend/src/VoxLib.Model/Messaging/`. `VoxLib.Model` still references no other VoxLib project
- [X] T010 Create `backend/src/VoxLib.Dal/Account/AccountDao.cs` deriving from `IdentityUser<Guid>`, adding `IsVerifiedBeneficiary` defaulting to false and `CreatedAt` as `timestamptz`
- [X] T011 Change `backend/src/VoxLib.Dal/Persistence/VoxLibDbContext.cs` to derive from `IdentityUserContext<AccountDao, Guid>` rather than `DbContext`, calling `base.OnModelCreating` first and leaving the catalogue configuration untouched. Rename the tables to the snake_case convention the catalogue already uses: `accounts`, `account_claims`, `account_logins`, `account_tokens`. Use the user variant, not `IdentityDbContext`, so the three role tables a claims-based model never reads are not created
- [X] T012 Add the unique index on `accounts.normalized_email` in `backend/src/VoxLib.Dal/Persistence/VoxLibDbContext.cs`, which is what makes FR-006 true however many times the same registration arrives. Do not apply the Ukrainian collation to it: an address is compared for exact equality and never sorted for a reader, and a linguistic collation on an identity column is how two different addresses become one account
- [X] T013 Add data protection key storage in `backend/src/VoxLib.Dal/Persistence/`, implementing `IDataProtectionKeyContext` on `VoxLibDbContext` so the `data_protection_keys` table lives beside the accounts it protects. R3: the default file-system store is under `/home/vscode`, which `CLAUDE.md` records as erased on rebuild, and it would be ephemeral in production too
- [X] T014 Generate the migration into `backend/src/VoxLib.Dal/Persistence/Migrations/` and read the generated SQL: it must carry the renamed tables, the unique index from T012, the key table from T013, and no role tables
- [X] T015 Implement `AccountStore` in `backend/src/VoxLib.Dal/Account/AccountStore.cs` over `UserManager<AccountDao>`: find by address, create, verify a password, read and set confirmation, read and record failed attempts and lockout, set a password, and end every session for an account by rotating the security stamp. Map `AccountDao` to `Listener` here and nowhere else, and let nothing else across that line: not the hash, not the stamp, not the failed count
- [X] T016 [P] Implement `AccountLinks` in `backend/src/VoxLib.Dal/Account/AccountLinks.cs` over the default token providers, issuing and redeeming a token for a `LinkPurpose`, base64url encoded. Set `DataProtectionTokenProviderOptions.TokenLifespan` to 24 hours
- [X] T017 [P] Implement `ListenerClaimsFactory` in `backend/src/VoxLib.Dal/Account/ListenerClaimsFactory.cs`, a `UserClaimsPrincipalFactory<AccountDao>` that adds the `voxlib:beneficiary` claim when the account carries the status
- [X] T018 [P] Implement `SmtpEmailSender` in `backend/src/VoxLib.Platform/Email/SmtpEmailSender.cs` over MailKit, configured entirely from the `Smtp` section added in T004. No provider SDK and no provider name in code, so choosing between Resend, Brevo, Postmark and Amazon SES later is a configuration change
- [X] T019 [P] Implement `IAccountSession` in `backend/src/VoxLib.Api/Account/HttpAccountSession.cs` over `SignInManager<AccountDao>`. It lives in `VoxLib.Api` because writing the cookie needs `HttpContext`; the interface stays declared in `VoxLib.Model` so the orchestrator names the intent and not the mechanism, as the plan's Constitution Check records
- [X] T020 Wire Identity in `backend/src/VoxLib.Api/Program.cs`: `AddIdentityCore<AccountDao>` with `AddSignInManager`, `AddDefaultTokenProviders` and `AddEntityFrameworkStores<VoxLibDbContext>`, then `AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies()`. No `AddIdentity`, no `AddDefaultIdentity`, no `MapIdentityApi`, no Identity UI package. Configure `PasswordOptions` from `PasswordPolicy`, and lockout at 5 consecutive failures for 15 minutes with lockout enabled for new accounts
- [X] T021 Configure the session cookie in `backend/src/VoxLib.Api/Program.cs`: `HttpOnly`, `SameSite=Strict`, `SecurePolicy` always outside development and same-as-request inside it, `ExpireTimeSpan` of 30 days with `SlidingExpiration`. Replace the redirect events with status codes, so a challenge answers 401 and a refusal answers 403 rather than redirecting to a Razor page that does not exist
- [X] T022 Set `SecurityStampValidatorOptions.ValidationInterval` to `TimeSpan.Zero` in `backend/src/VoxLib.Api/Program.cs`. The default is a thirty-minute timer, which would leave a session working for up to half an hour after the account it belongs to was recovered by its real owner. R2, and it is what SC-006 measures
- [X] T023 Register data protection in `backend/src/VoxLib.Api/Program.cs` with `PersistKeysToDbContext<VoxLibDbContext>()` and `SetApplicationName` pinned to a constant, so purpose strings stay stable across restarts and across instances
- [X] T024 Register the two authorization policies in `backend/src/VoxLib.Api/Program.cs`: `SignedIn`, requiring an authenticated principal, and `VerifiedBeneficiary`, requiring the `voxlib:beneficiary` claim. Attach neither to any endpoint in this feature. Registering them now unused is what makes the audio feature's change one line, per R4
- [X] T025 Register `AddAntiforgery` with `HeaderName` set to `X-XSRF-TOKEN`, and `AddRateLimiter`, in `backend/src/VoxLib.Api/Program.cs`, with the two policies from R7: `by-address`, a fixed window partitioned by the submitted email address, and `by-client`, a fixed window partitioned by the remote address. A rejected request answers 429 with `Retry-After`, the same response a lockout gives
- [X] T026 Add an endpoint filter or middleware in `backend/src/VoxLib.Api/Account/` that sends `X-Robots-Tag: noindex` on every response under `/api/account/`, per FR-028 and Principle IV
- [X] T027 Add `GET /api/account/antiforgery-token` in `backend/src/VoxLib.Api/Account/AccountEndpoints.cs`, calling `IAntiforgery.GetAndStoreTokens` and writing the request token into a readable `XSRF-TOKEN` cookie. Anonymous, unlike the sample in the Microsoft documentation: registering and signing in are state-changing requests made by someone with no session, and sign-in is itself a target of this attack
- [X] T028 Add `GET /api/account/password-policy` in `backend/src/VoxLib.Api/Account/AccountEndpoints.cs`, serving `PasswordPolicy` so the frontend states the same numbers the API enforces. This is what makes FR-002 a property of the system rather than the same numbers written twice, per R12
- [X] T029 Create `RecordingEmailSender` in `backend/tests/VoxLib.Api.Tests/Infrastructure/RecordingEmailSender.cs`, replacing `IEmailSender` in the test host and keeping the messages it was asked to send, so a test can extract a link and follow it
- [X] T030 Create `AccountApiFixture` in `backend/tests/VoxLib.Api.Tests/Infrastructure/AccountApiFixture.cs`, extending the existing `ApiFixture` and sharing one `CookieContainer` across `HttpClient` instances, so throwing a client away and building another means "closed the browser and reopened it". Expose a helper that fetches the antiforgery token once and attaches it to every state-changing request
- [X] T031 [P] Create the accessible form primitives in `frontend/src/components/FormField.tsx` and `frontend/src/components/StatusRegion.tsx`: a native `<label>` bound to a native control, `aria-invalid` and `aria-describedby` pointing at the field's own message when it is rejected, and a polite live region for state changes. Every screen in this feature uses both, and FR-031 and FR-033 are properties of these two files
- [X] T032 [P] Create `frontend/src/styles/account.css` with a visible focus indicator on every control and no rule that hides content from assistive technology, per FR-030
- [X] T033 Create `frontend/src/api/account.ts` with the fetch wrapper for account calls: it obtains the antiforgery token once, echoes it in `X-XSRF-TOKEN` on every state-changing request, and surfaces failures as typed results in the style `frontend/src/api/client.ts` already uses. Each story adds its own calls to this file
- [X] T034 Register the five account routes in `frontend/src/routes.ts` and confirm none of them is added to the prerender list in `frontend/react-router.config.ts`. They are personal to one person and are served by the single-page fallback the build already emits, per R10, which also gives FR-028 its first half: a page that was never generated cannot be indexed

**Checkpoint**: Identity, the session, the mail path and the accessible form primitives all
exist. Start User Story 1.

---

## Phase 3: User Story 1 - Create an account (Priority: P1) 🎯 MVP

**Goal**: A visitor gives an email address and chooses a password, and an account exists
afterwards, created with a screen reader or the keyboard alone.

**Independent Test**: Open the sign-up page with a screen reader and with the keyboard only,
create an account, and confirm afterwards that the account exists and that its password cannot
be read back from storage. No audio is involved.

### Tests for User Story 1

- [X] T035 [P] [US1] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/RegistrationTests.cs`: a well formed submission answers 202 and creates one account; a password below the policy answers 400 naming the password field; an address longer than 254 characters is rejected rather than truncated; a password written entirely in Cyrillic and one containing an emoji are both accepted
- [X] T036 [P] [US1] Endpoint test in `backend/tests/VoxLib.Api.Tests/Account/RegistrationTests.cs` for FR-006: the same registration submitted twice, including concurrently, leaves exactly one account and answers 202 both times
- [X] T037 [P] [US1] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/EnumerationTests.cs`: registering an address that already has an account produces a response identical in status, headers and body to registering a new one, and the two are not separable by the time taken
- [X] T038 [P] [US1] Endpoint test in `backend/tests/VoxLib.Api.Tests/Account/PasswordStorageTests.cs` for FR-003 and SC-013: the stored row carries a hash and no readable password, and neither the response body nor anything the application logged contains the password that was submitted
- [X] T039 [P] [US1] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/AntiforgeryTests.cs`: a state-changing call with no `X-XSRF-TOKEN` header is refused, one with a token from a different browser is refused, and the token endpoint itself is reachable anonymously
- [X] T040 [P] [US1] Endpoint test in `backend/tests/VoxLib.Api.Tests/Account/AccountHeaderTests.cs` asserting `X-Robots-Tag: noindex` on every response under `/api/account/`, per FR-028
- [X] T041 [P] [US1] Endpoint test in `backend/tests/VoxLib.Api.Tests/Account/RateLimitTests.cs`: registration beyond the client limit answers 429 with `Retry-After`

### Implementation for User Story 1

- [X] T042 [US1] Implement `Registration` in `backend/src/VoxLib.Orchestrator/Account/Registration.cs`: an address that already has an account produces the same outcome as a new one and a different message, and submitting twice leaves exactly one account. The decision that the caller is never told which case occurred lives here, not in the endpoint
- [X] T043 [US1] Implement `IAccountMessages` in `backend/src/VoxLib.Orchestrator/Account/AccountMessages.cs` with the Ukrainian text for the confirmation message and for the "an account already exists" message, the second offering recovery. Both go out through `IEmailSender`, and both build their link from the base address configured in T005
- [X] T044 [P] [US1] Add `RegistrationRequest` to `backend/src/VoxLib.Api/Account/Contracts/Requests/`, matching `contracts/accounts.yaml`
- [X] T045 [US1] Add `POST /api/account/registrations` in `backend/src/VoxLib.Api/Account/AccountEndpoints.cs`, applying both rate limit policies and answering 202 for every well formed submission. Validation failures answer 400 with the per-field `errors` shape from the contract, because FR-031 needs each failure tied to the field it concerns. No business rule in the handler
- [X] T046 [US1] Run `pnpm --filter frontend gen:api-types` and commit `frontend/src/api/schema.d.ts`
- [X] T047 [US1] Add the registration call to `frontend/src/api/account.ts`, and a call that reads the password policy from T028
- [X] T048 [US1] Build the registration screen in `frontend/src/routes/register.tsx` with Ukrainian text: native form controls, `autocomplete="username"` on the address and `autocomplete="new-password"` on the password so a password manager fills the form and offers to save the result, per FR-032. Declare `<meta name="robots" content="noindex">` through the route's `meta` export
- [X] T049 [US1] State the password requirements in `frontend/src/routes/register.tsx` before the form is submitted, rendered from the policy fetched in T047 rather than written as prose, per FR-002. On rejection, name what is wrong on the field itself through `FormField`
- [X] T050 [US1] Announce submitting, failing and succeeding through `StatusRegion` in `frontend/src/routes/register.tsx`, and on success show the instruction to check the inbox, with focus landing where the person can read it. FR-033, and it is the whole of acceptance scenario 1
- [X] T051 [P] [US1] Automated accessibility run in `frontend/tests/a11y/account-register.spec.ts`: zero critical and zero serious violations, the form completed by keyboard alone with a visible focus indicator and no trap, and a rejected field announced and tied to its own message
- [ ] T052 [US1] Manually register with VoiceOver on iOS and TalkBack on Android: the password requirements readable before submitting, a rejected field announced with the reason, the outcome announced, and a password manager offering to save. Record the outcome and the versions tested in `specs/002-user-accounts/manual-verification.md`

**Checkpoint**: An account can be created, accessibly, and nothing about the response reveals
whether the address was already known. The account cannot yet be used, which is deliberate.

---

## Phase 4: User Story 2 - Confirm the email address (Priority: P2)

**Goal**: The person follows the link in the message they were sent, and the account becomes
usable.

**Independent Test**: Register an account, take the link from the sent message, follow it, and
confirm the account's state changes and that sign-in becomes possible. Follow the link a second
time and after its expiry, and confirm both are handled.

### Tests for User Story 2

- [X] T053 [P] [US2] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/EmailConfirmationTests.cs`: the link captured by `RecordingEmailSender` confirms the address and answers `confirmed`; following it a second time answers 200 with `alreadyConfirmed` rather than an error, per FR-008; a token past its lifetime answers 410
- [X] T054 [P] [US2] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/EmailConfirmationTests.cs` for FR-009: another confirmation message can be requested, requests beyond the per-address limit answer 429, and the request answers 202 whether or not the address has an unconfirmed account
- [X] T055 [P] [US2] Endpoint test in `backend/tests/VoxLib.Api.Tests/Account/EmailConfirmationTests.cs` asserting the order of checks: an already confirmed address answers `alreadyConfirmed` even when the token it was given has expired, which is what tells "already confirmed" apart from "expired" per R8

### Implementation for User Story 2

- [X] T056 [US2] Implement `EmailConfirmation` in `backend/src/VoxLib.Orchestrator/Account/EmailConfirmation.cs`, checking whether the address is already confirmed before validating the token, because confirming does not rotate the security stamp and that order is what makes the two outcomes distinguishable
- [X] T057 [P] [US2] Add `TokenRequest`, `EmailOnlyRequest` and the `ConfirmationResult` response to `backend/src/VoxLib.Api/Account/Contracts/`, matching `contracts/accounts.yaml`
- [X] T058 [US2] Add `POST /api/account/confirmations` and `POST /api/account/confirmation-requests` in `backend/src/VoxLib.Api/Account/AccountEndpoints.cs`, the second under the per-address rate limit so the endpoint cannot be used to send mail repeatedly to someone who did not ask for it
- [X] T059 [US2] Regenerate `frontend/src/api/schema.d.ts` and commit the result, then add both calls to `frontend/src/api/account.ts`
- [X] T060 [US2] Build the confirmation screen in `frontend/src/routes/confirm.tsx`, reading the account id and token from the query string and posting them. Announce the outcome, and on an expired link offer another message from the same screen. `meta` declares noindex
- [X] T061 [US2] On success in `frontend/src/routes/confirm.tsx`, place the way onward to signing in where focus lands, per acceptance scenario 5, rather than leaving the person to find it
- [X] T062 [P] [US2] Automated accessibility run in `frontend/tests/a11y/account-confirm.spec.ts`: zero critical and zero serious violations, the outcome announced, and the route onward reachable from where focus lands
- [ ] T063 [US2] Manually follow a confirmation link with VoiceOver on iOS and TalkBack on Android, including a second visit and an expired one. Record the outcome in `specs/002-user-accounts/manual-verification.md`

**Checkpoint**: An account created in Story 1 can be brought to life. Sign-in is still not built.

---

## Phase 5: User Story 3 - Sign in and stay signed in (Priority: P3)

**Goal**: A person whose account is confirmed signs in once and is still recognised the next
day on the same device.

**Independent Test**: Sign in with a seeded confirmed account, close the browser entirely,
reopen it, and confirm the person is still recognised. Repeat with a screen reader and with the
keyboard alone.

### Tests for User Story 3

- [X] T064 [P] [US3] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/SignInTests.cs`: a confirmed account signs in and the session says who it is; a wrong password answers 401; the correct password on an unconfirmed address answers 403 saying the address must be confirmed and offering another message, per FR-011
- [X] T065 [P] [US3] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/EnumerationTests.cs`, extending the file from T037: an unknown address and a known address with the wrong password produce identical responses, and are not separable by the time taken, which is the dummy-hash verification from R5
- [X] T066 [P] [US3] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/LockoutTests.cs`: five consecutive failures lock the account, the sixth answers 429 with `Retry-After`, and the same number of attempts against an address with no account answers the same 429 with the same shape. That second assertion is the one that keeps FR-016 from breaking FR-005
- [X] T067 [P] [US3] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/SessionLifetimeTests.cs`: the session cookie is `HttpOnly`, `SameSite=Strict` and persistent for 30 days, per FR-013; a new `HttpClient` over the same cookie container is still signed in, which is what "closed the browser and reopened it" means and is SC-004
- [X] T068 [P] [US3] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/SessionTests.cs`: `GET /api/account/session` answers 200 with `signedIn` false for an anonymous browser rather than 401, and carries the address and the beneficiary status, false by default, when signed in. FR-025 and FR-026
- [X] T069 [P] [US3] Endpoint test in `backend/tests/VoxLib.Api.Tests/Catalogue/AnonymousAccessTests.cs`, extending the existing file: every catalogue endpoint still answers with no cookie of any kind now that authentication exists. FR-027 and SC-012

### Implementation for User Story 3

- [X] T070 [US3] Implement `SignIn` in `backend/src/VoxLib.Orchestrator/Account/SignIn.cs`, deciding between the four outcomes and verifying a password against a fixed dummy hash when the address is unknown, so an unknown address costs the same work as a wrong one. `EmailNotConfirmed` is reachable only once the password has verified, which is FR-011 sitting inside FR-005
- [X] T071 [P] [US3] Add `SignInRequest` and the `Session` response to `backend/src/VoxLib.Api/Account/Contracts/`, matching `contracts/accounts.yaml`
- [X] T072 [US3] Add `POST /api/account/session` and `GET /api/account/session` in `backend/src/VoxLib.Api/Account/SessionEndpoints.cs`, mapping the four outcomes to 200, 401, 403 and 429. The 429 must be identical to the one the rate limiter produces, headers included
- [X] T073 [US3] Regenerate `frontend/src/api/schema.d.ts` and commit the result, then add the sign-in and session calls to `frontend/src/api/account.ts`
- [X] T074 [US3] Build the session hook in `frontend/src/account/useSession.ts`, reading `GET /api/account/session` after the page loads. The account pages are not generated ahead of time, so whether someone is signed in is established after load rather than built into the document, which is also why `SameSite=Strict` costs nothing here
- [X] T075 [US3] Build the sign-in screen in `frontend/src/routes/sign-in.tsx` with Ukrainian text, `autocomplete="username"` and `autocomplete="current-password"`, failures announced through `StatusRegion` without the person having to search the page, and the unconfirmed-address case offering another message. `meta` declares noindex
- [X] T076 [US3] Detect a browser that is not keeping the session in `frontend/src/routes/sign-in.tsx`: after a successful sign-in, read the session again, and if it comes back anonymous say so plainly rather than returning the person silently to the form. FR-019, and it is the private-browsing edge case
- [X] T077 [US3] Show who is signed in from `frontend/src/components/SessionMenu.tsx`, rendered in `frontend/src/root.tsx`, so a person on a shared device can tell at a glance and a screen reader can be told. This answers the spec's edge case about a shared device
- [X] T078 [P] [US3] Automated accessibility run in `frontend/tests/a11y/account-sign-in.spec.ts`: zero critical and zero serious violations, the whole form operable by keyboard, and a failed sign-in announced
- [ ] T079 [US3] Manually sign in with VoiceOver on iOS and TalkBack on Android, close the browser, reopen it and confirm the person is still recognised and can tell that they are. Record the outcome in `specs/002-user-accounts/manual-verification.md`

**Checkpoint**: An account is usable. A person can register, confirm and sign in, and stays
signed in across a browser restart.

---

## Phase 6: User Story 4 - Recover a forgotten password (Priority: P4)

**Goal**: A person who cannot remember their password, or has locked themselves out, gets back
in and chooses a new one.

**Independent Test**: Lock an account by failing sign-in repeatedly, request recovery, follow
the link, set a new password, and confirm the person can sign in, that the lockout is gone, and
that any session that existed beforehand no longer works.

### Tests for User Story 4

- [X] T080 [P] [US4] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/PasswordRecoveryTests.cs`: a recovery request answers 202 whether or not the address has an account; the link captured by `RecordingEmailSender` sets a new password; the person signs in with it immediately
- [X] T081 [P] [US4] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/PasswordRecoveryTests.cs` for FR-021, FR-022 and FR-023: setting a password through recovery clears a lockout that was in force, records the address as confirmed even if it never was, and refuses a session that existed beforehand on its very next request rather than after any interval. That last assertion is SC-006 and is what T022 exists for
- [X] T082 [P] [US4] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/PasswordRecoveryTests.cs` for FR-024: a recovery link that has been used answers 410, one past its lifetime answers 410, and both offer a replacement. A used link fails because setting the password rotated the security stamp the token carries, per R8
- [X] T083 [P] [US4] Endpoint test in `backend/tests/VoxLib.Api.Tests/Account/EnumerationTests.cs`, extending it again: a recovery request for an address with no account is identical to one for an address that has one

### Implementation for User Story 4

- [X] T084 [US4] Implement `PasswordRecovery` in `backend/src/VoxLib.Orchestrator/Account/PasswordRecovery.cs`, holding FR-021, FR-022 and FR-023 in one place: setting the password clears the lockout, ends every existing session, and confirms the address, because following a link sent there proves what confirmation proves
- [X] T085 [US4] Add the Ukrainian recovery message to `backend/src/VoxLib.Orchestrator/Account/AccountMessages.cs`, built from the same base address as the confirmation message
- [X] T086 [P] [US4] Add `PasswordResetRequest` to `backend/src/VoxLib.Api/Account/Contracts/Requests/`, matching `contracts/accounts.yaml`
- [X] T087 [US4] Add `POST /api/account/recovery-requests` and `POST /api/account/recoveries` in `backend/src/VoxLib.Api/Account/AccountEndpoints.cs`, the first under both rate limits, the second answering 410 for a used or expired link
- [X] T088 [US4] Regenerate `frontend/src/api/schema.d.ts` and commit the result, then add both calls to `frontend/src/api/account.ts`
- [X] T089 [P] [US4] Build the request screen in `frontend/src/routes/forgot-password.tsx` with Ukrainian text, reachable from the sign-in screen, always reporting that a message has been sent. `meta` declares noindex
- [X] T090 [US4] Build the reset screen in `frontend/src/routes/reset-password.tsx`, reading the account id and token from the query string, stating the password requirements from the policy before submission as T049 does, and announcing the outcome. `meta` declares noindex
- [X] T091 [P] [US4] Automated accessibility run in `frontend/tests/a11y/account-recover.spec.ts` over both screens: zero critical and zero serious violations, and both forms operable by keyboard with failures announced
- [ ] T092 [US4] Manually recover a locked-out account with VoiceOver on iOS and TalkBack on Android, end to end, and confirm a session open elsewhere stops working. Record the outcome in `specs/002-user-accounts/manual-verification.md`

**Checkpoint**: A forgotten password is no longer a destroyed account, which is what makes
lockout and an unrecoverable hash safe to ship.

---

## Phase 7: User Story 5 - Sign out (Priority: P5)

**Goal**: The person ends the session so the next person at that browser is anonymous.

**Independent Test**: Sign in, sign out, and confirm that the following request is anonymous
and that the session cannot be reused even if it was captured beforehand.

### Tests for User Story 5

- [X] T093 [P] [US5] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Account/SignOutTests.cs`: the request after signing out is anonymous; a cookie captured before signing out is refused when replayed afterwards; a second device signed in to the same account is unaffected; signing out with no session answers 204 rather than failing
- [X] T094 [P] [US5] Endpoint test in `backend/tests/VoxLib.Api.Tests/Account/AntiforgeryTests.cs`, extending T039: signing out without the `X-XSRF-TOKEN` header is refused, since it is a state-changing request like any other

### Implementation for User Story 5

- [X] T095 [US5] Add `DELETE /api/account/session` in `backend/src/VoxLib.Api/Account/SessionEndpoints.cs`, clearing the cookie and answering 204 whether or not there was a session, so signing out twice is not an error
- [X] T096 [US5] Regenerate `frontend/src/api/schema.d.ts` and commit the result, then add the call to `frontend/src/api/account.ts`
- [X] T097 [US5] Add the sign-out control to `frontend/src/components/SessionMenu.tsx` as a native button in a form, never a link, because no request that changes state may use a method a browser follows from a link. Announce the change of state through `StatusRegion` rather than only showing it, per FR-033
- [X] T098 [P] [US5] Automated accessibility run in `frontend/tests/a11y/account-sign-out.spec.ts`: zero critical and zero serious violations, the control reachable by keyboard, and the change of state announced
- [ ] T099 [US5] Manually sign out with VoiceOver on iOS and TalkBack on Android and confirm the change is announced. Record the outcome in `specs/002-user-accounts/manual-verification.md`

**Checkpoint**: All five stories complete. A person can register, confirm, sign in, recover and
sign out, without sight or a mouse.

---

## Phase 8: Polish and Cross-Cutting Concerns

- [X] T100 [P] Create `specs/002-user-accounts/manual-verification.md` with the same shape as the one in 001, so T052, T063, T079, T092 and T099 have somewhere to record their outcome
- [X] T101 [P] Update `README.md` and `CLAUDE.md` with the Mailpit prerequisite, the container rebuild step, and the fact that account screens are served by the single-page fallback rather than prerendered. `CLAUDE.md` is owned by Prettier, so run `pnpm exec prettier --write CLAUDE.md README.md` after editing
- [X] T102 [P] Add a build output assertion in `frontend/tests/prerender/prerender.spec.ts`, extending the file from 001: no document exists for any account route, and `frontend/build/client/__spa-fallback.html` does exist. R10, and it is the check that the account screens will load in production
- [X] T103 [P] Add an endpoint test in `backend/tests/VoxLib.Api.Tests/Account/NoAudioExposureTests.cs` asserting that no account response contains an audio location, a storage key or any field resembling one, matching what the catalogue already asserts
- [ ] T104 Run every scenario in `specs/002-user-accounts/quickstart.md` and correct whatever does not match
- [X] T105 Run the continuous integration parity commands: `dotnet format backend/VoxLib.slnx --verify-no-changes`, `dotnet test backend/VoxLib.slnx`, and `pnpm format:check && pnpm lint && pnpm build`. `ci.yml` itself is unchanged by this feature, deliberately: its job names are the status check contexts the branch protection ruleset matches
- [ ] T106 Rebuild the dev container from `.devcontainer/docker-compose.yml` from scratch and confirm the database, Mailpit, the migrations and both dev servers come up, and that an existing session survives the rebuild, which is what T013 is for. Nothing in continuous integration builds the container, so this is the only check on T001

---

## Outstanding

99 of 106 tasks are complete. The seven below are not, and none of them can be done from
inside the dev container.

| Task | Why it is outstanding |
| --- | --- |
| T052, T063, T079, T092, T099 | The manual screen reader passes. They need an iOS device with VoiceOver and an Android device with TalkBack. `specs/002-user-accounts/manual-verification.md` has a table waiting for each one. Principle I is non-negotiable, so **this feature is not done until they are recorded**, whatever the automated runs say |
| T104 | Every scenario in `quickstart.md` was run and passes, except the ones that read a message out of Mailpit. Those need T106 first |
| T106 | Rebuilding the dev container, which cannot be done from inside it. Mailpit has never started, so the SMTP path has never run against a real server. The endpoint tests substitute the sender, so they prove the flows and not the transport |

What the automated runs do cover, as of the last full pass: 163 endpoint tests, 59 accessibility
checks across all five account screens and the three catalogue screens, and 7 assertions about
what the build emits. `dotnet format`, `dotnet test`, `pnpm format:check`, `pnpm lint` and
`pnpm build` are all green.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup. Blocks all user stories
- **User Stories (Phases 3 to 7)**: Sequential, in priority order
- **Polish (Phase 8)**: Depends on the stories you intend to ship

### Why the stories are sequential

Two reasons, and either one alone would be enough. They share files: `Program.cs`,
`AccountEndpoints.cs`, `SessionEndpoints.cs`, `frontend/src/api/account.ts` and the generated
`schema.d.ts` are each touched by three or more stories, and running them concurrently would
mean several writers per file. And the spec's own ordering is a chain: an account has to exist
before it can be confirmed, be confirmed before it can be signed in to, and there has to be a
session before signing out or evicting one means anything.

### Within Setup

- T002 before T003 is not required, but T002 must precede any task that references `VoxLib.Platform`
- T004 and T005 both edit the two `appsettings` files. Only T004 is marked parallel; run them in sequence or fold them into one edit

### Within Foundational

- T006 to T008 are independent of each other and precede T009, which names them
- T009 before T015 to T019, since each implements one of its interfaces
- T010 before T011 before T012 and T013, and all three before T014, since the migration is generated from the finished model
- T020 to T028 all edit `Program.cs` and `AccountEndpoints.cs`. None is marked parallel; treat them as one sequence over two files
- T014 and T020 before T030, since the fixture migrates and boots the real application
- T031 to T034 are frontend and independent of the backend sequence above

### Within Each Story

- Tests before implementation, and they must fail first
- Store or links before orchestrator before endpoint
- Endpoint before type generation before the frontend that consumes the types
- The frontend screen before its automated accessibility run
- The manual screen reader pass last, once the story is otherwise complete

### The three tasks nothing else can substitute for

- **T022**, the validation interval. Without it every test in T081 that asserts a session dies
  on its next use will pass locally within thirty minutes of a fresh sign-in and fail
  unpredictably otherwise, and SC-006 will be false in production
- **T013**, the key ring in the database. Without it a container rebuild signs everyone out and
  voids every unfollowed link, and the symptom looks like a bug in this feature
- **T066**, the assertion that an unknown address is refused exactly as a locked-out one is.
  It is the only place the FR-016 and FR-005 tension is actually held down

---

## Parallel Example: Foundational

```bash
# Four independent domain and infrastructure files:
Task: "Listener in backend/src/VoxLib.Model/Account/Listener.cs"
Task: "PasswordPolicy in backend/src/VoxLib.Model/Account/PasswordPolicy.cs"
Task: "Outcome types in backend/src/VoxLib.Model/Account/"
Task: "SmtpEmailSender in backend/src/VoxLib.Platform/Email/SmtpEmailSender.cs"

# Three independent frontend files, none touching the backend sequence:
Task: "FormField and StatusRegion in frontend/src/components/"
Task: "Account styles in frontend/src/styles/account.css"
Task: "Route registration in frontend/src/routes.ts"
```

## Parallel Example: User Story 1

```bash
# Seven independent endpoint test files, all written before implementation:
Task: "Registration behaviour in RegistrationTests.cs"
Task: "Idempotent registration in RegistrationTests.cs"
Task: "Address enumeration in EnumerationTests.cs"
Task: "Password storage and logs in PasswordStorageTests.cs"
Task: "Antiforgery in AntiforgeryTests.cs"
Task: "Robots header in AccountHeaderTests.cs"
Task: "Rate limit in RateLimitTests.cs"
```

---

## Implementation Strategy

### MVP First

1. Phase 1 Setup
2. Phase 2 Foundational
3. Phase 3, User Story 1, ending with the manual screen reader pass in T052
4. Stop and validate

Be honest about what that MVP is. An account that exists but cannot be signed in to is not
shippable on its own; it is a checkpoint that proves registration, the mail path, the
antiforgery layer and the accessible form primitives all work. The first shippable increment
is Story 3, because Stories 1 to 3 together are the smallest set where a person gets something
they can use.

### Incremental Delivery

Setup and Foundational, then Stories 1 to 5 in order, validating each before the next.
Stories 4 and 5 are each small and independently valuable once Story 3 exists.

### If the feature is too large

Story 4 is the one that looks trimmable and is not. Lockout in FR-016 and an unrecoverable
hash in FR-003 are both in Story 1 and Story 3, and shipping those without a way back in means
a forgotten password destroys an account for good, which for a person navigating by screen
reader is a likely event rather than a rare one. The spec's first clarification says exactly
this. If the feature has to shrink, the honest cut is to move Story 5 out, since a session can
be ended by clearing cookies until there is a control for it, and the automated accessibility
runs can go. The manual passes in T052, T063, T079, T092 and T099 cannot: Principle I is
non-negotiable.

---

## Notes

- `[P]` means different files and no dependency on incomplete work
- Every behaviour change carries the endpoint test that proves it, as Principle VI requires
- Automated checks catch violations and regressions; only the manual passes prove the product
  is usable by the people it exists for
- Nothing in this feature reads or writes audio, and nothing attaches either authorization
  policy to an endpoint. Both are deliberate and both are the audio feature's work
- Commit after each task or logical group. Squash merging means commit granularity on the
  branch does not matter
- Stop at any checkpoint to validate a story
