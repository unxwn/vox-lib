# Data Model: Accounts and Sessions

**Feature**: `specs/002-user-accounts/` | **Date**: 2026-09-09

Three homes, as in 001 and as the constitution requires. The domain model lives in
`VoxLib.Model` and holds behaviour. `VoxLib.Dal` holds the persistence shape and maps to the
domain once. `VoxLib.Api` holds the wire contracts, which are described in
`contracts/accounts.yaml`.

The spec names four key entities. Three of them are types this feature writes: `Listener`, the
credential record, and the session. A single-use link is not: it is a token carrying its own
account and security stamp, so it needs no table, and R8 in `research.md` says why.

The session gained a table during implementation, against the design recorded here at plan
time. The reason is in R14: FR-018 requires that a session captured before signing out be
refused afterwards, **and** that signing out on one device leave another signed in. A
self-contained cookie cannot do the first, and the account's security stamp cannot do the
second, because a stamp belongs to an account rather than to a session.

## Domain model (`VoxLib.Model/Account/`)

### Listener

The identity that access decisions are made about. It is deliberately not the credential
record: it carries nothing about how a password is stored, so how credentials are kept can
change without touching the rules that use it. This is FR-026.

| Field                  | Type    | Notes                                                        |
| ---------------------- | ------- | ------------------------------------------------------------ |
| `AccountId`            | `Guid`  | Identity. Never appears in a page address.                   |
| `Email`                | `string`| The confirmed address, for showing who is signed in.         |
| `IsVerifiedBeneficiary`| `bool`  | Whether this person is verified as entitled to accessible-format copies. Always false in this feature, per FR-025. |

Behaviour on the type, not in a service:

- `Anonymous` is a static instance with no account, so "nobody is signed in" is a value the
  callers handle rather than a null they might forget.
- `IsSignedIn` returns whether this is a real account rather than `Anonymous`.

The rule that will read `IsVerifiedBeneficiary` belongs to the audio feature and is not
written here. What this feature guarantees is that the rule has somewhere to read it from.

### PasswordPolicy

| Field                  | Type   | Value | Notes                                        |
| ---------------------- | ------ | ----- | -------------------------------------------- |
| `MinimumLength`        | `int`  | 12    | Counted in characters, over Unicode.         |
| `RequiresDigit`        | `bool` | false | See R12: length alone.                       |
| `RequiresUppercase`    | `bool` | false |                                              |
| `RequiresLowercase`    | `bool` | false |                                              |
| `RequiresNonAlphanumeric` | `bool` | false |                                           |

One source of truth. `VoxLib.Api` configures Identity's `PasswordOptions` from it, and serves
it at `GET /api/account/password-policy` so the frontend states the same numbers, which is
what FR-002 needs.

### Outcomes

Each flow returns an outcome rather than throwing, so the endpoint shapes a response and the
orchestrator holds the decision. Every one of these values is a requirement.

| Type                  | Values                                                              |
| --------------------- | ------------------------------------------------------------------- |
| `RegistrationOutcome` | `Accepted`, `PasswordRejected`                                       |
| `SignInOutcome`       | `Succeeded`, `NotRecognised`, `EmailNotConfirmed`, `LockedOut`       |
| `ConfirmationOutcome` | `Confirmed`, `AlreadyConfirmed`, `LinkExpired`                       |
| `RecoveryOutcome`     | `PasswordSet`, `LinkExpired`, `PasswordRejected`                     |
| `LinkPurpose`         | `ConfirmEmail`, `ResetPassword`                                      |

`RegistrationOutcome` has no `AlreadyExists`. That is the point of FR-005: the orchestrator
knows, the caller never does, and the difference goes to the address itself as a message
rather than into the response.

`SignInOutcome.LockedOut` carries the moment the person may try again, because FR-016 requires
telling them. R5 explains why an address with no account produces the same response.

### Interfaces declared in `VoxLib.Model`

| Interface           | What it does                                                                | Implemented in     |
| ------------------- | --------------------------------------------------------------------------- | ------------------ |
| `IAccountStore`     | Find by address or id, create, verify a password, read and set confirmation, read and record failed attempts and lockout, set a password, end every session for an account | `VoxLib.Dal`       |
| `ISessionStore`     | Create, read, renew and remove one browser's session. See R14                | `VoxLib.Dal`       |
| `IAttemptLimiter`   | How often something may be attempted against one address. See R15            | `VoxLib.Orchestrator` |
| `IAccountLinks`     | Issue and redeem a single-use, time-limited token for a purpose              | `VoxLib.Dal`       |
| `IAccountMessages`  | Send the confirmation, the recovery and the "an account already exists" message, in Ukrainian | `VoxLib.Orchestrator` over `IEmailSender` |
| `IEmailSender`      | Deliver one message                                                          | `VoxLib.Platform`  |
| `IAccountSession`   | Start the current browser's session, end it, read who it belongs to          | `VoxLib.Api`       |

`IAccountSession` is implemented in `VoxLib.Api` and nowhere else, because writing the cookie
needs `HttpContext`. The plan's Constitution Check records that placement and why.

### Rules held in `VoxLib.Orchestrator/Account/`

Not entities, but this is where the behaviour that is not on a type lives, and the data model
is not readable without knowing which decisions happen where.

- `Registration` decides that an address already taken produces the same outcome as a new one
  and a different message, and that submitting twice leaves exactly one account.
- `EmailConfirmation` decides the order of checks that tells "already confirmed" apart from
  "expired", which is FR-008.
- `SignIn` decides which of the four outcomes applies, and that `EmailNotConfirmed` is
  reachable only once the password has verified, which is FR-011 inside FR-005.
- `PasswordRecovery` decides that setting a password clears the lockout, ends every existing
  session and confirms the address, which is FR-021, FR-022 and FR-023 in one place.

## Persistence model (`VoxLib.Dal`)

`VoxLibDbContext` becomes an `IdentityUserContext<AccountDao, Guid>` rather than a plain
`DbContext`. The user variant, not `IdentityDbContext`, so the three role tables a
claims-based model never reads are not created. The catalogue's existing configuration is
untouched, and `base.OnModelCreating` is called first so the Identity configuration is applied
before the renaming below.

Tables, named to match the snake_case convention the catalogue already uses rather than
Identity's defaults:

| Table                  | From                     | Holds                                            |
| ---------------------- | ------------------------ | ------------------------------------------------ |
| `accounts`             | `AspNetUsers`            | The credential record                            |
| `sessions`             | new                      | One row per signed-in browser. See R14           |
| `account_claims`       | `AspNetUserClaims`       | Unused today; kept because the store expects it   |
| `account_logins`       | `AspNetUserLogins`       | Unused today; sign-in with an account held elsewhere is out of scope |
| `account_tokens`       | `AspNetUserTokens`       | Unused today; the links carry their own state    |
| `data_protection_keys` | The key ring             | See R3                                           |

### AccountDao

Derives from `IdentityUser<Guid>`, which supplies most of the spec's Account entity already.

| Column                    | From        | Notes                                                    |
| ------------------------- | ----------- | -------------------------------------------------------- |
| `id`                      | `Id`        | Primary key.                                              |
| `email`, `normalized_email` | base      | The address as given, and the form the unique index uses. |
| `email_confirmed`         | base        | FR-007. False until the link is followed.                 |
| `password_hash`           | base        | Identity's hasher. FR-003: the original is not recoverable from it. |
| `security_stamp`          | base        | Changes when the password is set. This is what ends every session, per FR-022. |
| `concurrency_stamp`       | base        | Optimistic concurrency.                                   |
| `access_failed_count`     | base        | Consecutive failures, FR-016.                             |
| `lockout_end`             | base        | When the person may try again, FR-016.                    |
| `lockout_enabled`         | base        | True for every account created here.                      |
| `is_verified_beneficiary` | new         | FR-025. Defaults to false and nothing in this feature sets it true. |
| `created_at`              | new         | `timestamptz`. The spec's Account entity names it.        |

`user_name` and `normalized_user_name` are set to the email address, because Identity requires
a user name and this product's account is an address and nothing else. `phone_number`,
`phone_number_confirmed`, `two_factor_enabled` and the phone columns beside them come from the
base type, are never written, and are recorded in the plan's Complexity Tracking as the price
of using the Entity Framework store rather than writing one.

### SessionDao

One row per signed-in browser. The cookie carries `id` and nothing else, so deleting one row
ends one session and only that one.

| Column       | Notes                                                                  |
| ------------ | ---------------------------------------------------------------------- |
| `id`         | The key the cookie carries. Opaque and unguessable.                     |
| `payload`    | The encrypted session, opaque to the persistence layer. What a session contains is a matter for the transport that issues it. |
| `expires_at` | When it stops being valid on its own. Expired rows are swept when a session is created, rather than by a background service that would have to be run and watched. |

### Constraints and indexes that carry requirements

| Rule                                                        | Enforced by                                    | Requirement |
| ----------------------------------------------------------- | ---------------------------------------------- | ----------- |
| One account per address, however the same registration arrives | Unique index on `accounts.normalized_email`  | FR-006      |
| A password cannot be read back                               | Only `password_hash` is stored; there is no column for a password | FR-003 |
| Ending every session for an account is one write             | `security_stamp` on `accounts`                  | FR-014, FR-022 |
| A lockout has an end that can be told to the person          | `lockout_end` on `accounts`                     | FR-016      |
| Sessions and links survive a restart and a second instance   | `data_protection_keys`                          | FR-012, R3  |
| A session captured before sign out is refused afterwards     | Deleting its row in `sessions`                  | FR-018, R14 |
| Signing out on one device leaves another signed in           | One row per browser, rather than one stamp per account | FR-018, R14 |

The address is matched through `normalized_email`, which Identity upper-cases with the
invariant culture. It is deliberately not given the Ukrainian collation the catalogue's title
and author columns carry: an address is compared for exact equality, never sorted for a
reader, and a linguistic collation on an identity column is how two addresses that differ
become the same account.

## Mapping

One boundary, mapped once, as Principle II requires.

- `VoxLib.Dal` maps `AccountDao` to `Listener`. Nothing else about the account crosses that
  line: not the hash, not the stamp, not the failed count.
- `VoxLib.Api` maps `Listener` to the `Session` response contract.
- `VoxLib.Dal`'s `ListenerClaimsFactory` puts `IsVerifiedBeneficiary` on the principal as the
  `voxlib:beneficiary` claim, refreshed on every request by the revalidation described in R2.

## Validation rules

Drawn from the specification, each one testable:

- An address must be present and well formed. It is stored as given and matched normalized.
- A password must be at least 12 characters. No character-class rule applies, and a password
  written entirely in Cyrillic or containing an emoji is accepted, since length is counted over
  Unicode.
- A rejected password names what is wrong, tied to the password field, per FR-002 and FR-031.
- Registration is accepted whether or not the address already has an account, and exactly one
  account exists afterwards however many times it is submitted.
- Sign-in requires a confirmed address. The reason is given only after the password verifies.
- A confirmation or recovery token is valid for 24 hours, for one account, for one purpose.
- Setting a password through recovery clears the lockout, confirms the address, and ends every
  session that existed beforehand.

## What is deliberately absent

- No display name, no avatar, no profile, no preferences. An account is an address and a
  password, per the spec's assumptions.
- No table of issued links. A confirmation or recovery token carries its own account and
  security stamp, so a table of issued links would be a second mechanism for something already
  correct.
- No role tables. Access is decided from a claim, per the spec and R4.
- No audio field on anything, and no interface that could reach one. This feature exists so
  that audio has something to authorise against; it never names audio.
- No administrative capability. Nothing here lists, inspects or disables an account, which the
  spec states as an assumption. FR-014 asks only that ending a session is possible, and
  rotating the security stamp is that.
