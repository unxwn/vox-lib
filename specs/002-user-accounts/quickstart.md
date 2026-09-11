# Quickstart: Accounts and Sessions

**Feature**: `specs/002-user-accounts/` | **Date**: 2026-09-09

How to run the feature and prove it works. Shapes, field names and status codes are in
`contracts/accounts.yaml` and `data-model.md` rather than repeated here.

## Prerequisites

The dev container gains a Mailpit service in this feature, so rebuild it once before anything
below will work. Confirmation is required before the first sign-in, so without somewhere for
mail to go, not one flow can be walked by hand.

> Dev Containers: Rebuild Container

Confirm Mailpit is reachable on the shared network stack. Both must answer; if the first one
refuses the connection, the service is missing `network_mode: service:db` and the API will
fail at the moment it sends a message.

```bash
curl -sf http://localhost:8025/api/v1/info > /dev/null && echo 'mailpit web ok'
timeout 2 bash -c 'cat < /dev/null > /dev/tcp/localhost/1025' && echo 'mailpit smtp ok'
```

## Run it

```bash
pnpm dev
```

The API applies pending migrations at startup, which now creates the account tables and the
data protection key ring alongside the catalogue. The site is at http://localhost:5173, the
API at http://localhost:5080, and the mailbox at http://localhost:8025.

## Walk the whole journey by hand

This is the journey SC-001 measures, and it is worth doing once with the screen reader on.

1. Open http://localhost:5173/register and create an account.
2. Open http://localhost:8025, open the message, follow the confirmation link.
3. Sign in at http://localhost:5173/sign-in.
4. Close the browser completely, reopen it, and load the site. You are still signed in.
5. Sign out, then use "forgot password", follow that link, set a new password, sign in.

## Prove the API behaves

Each command maps to a requirement, and each is also covered by an endpoint test so the
behaviour is enforced rather than observed once. The session is a cookie, so these use a
cookie jar; the antiforgery token has to be fetched first, because every state-changing call
requires it.

```bash
J=$(mktemp)
TOKEN=$(curl -s -c "$J" -o /dev/null -D - localhost:5080/api/account/antiforgery-token \
  | sed -n 's/.*XSRF-TOKEN=\([^;]*\).*/\1/p')

post() { curl -s -b "$J" -c "$J" -H 'Content-Type: application/json' \
  -H "X-XSRF-TOKEN: $TOKEN" -X POST -d "$2" -o /dev/null -w '%{http_code}\n' "localhost:5080$1"; }
```

```bash
# FR-001: registering is accepted
post /api/account/registrations '{"email":"someone@example.com","password":"довгий-пароль-2026"}'

# FR-005, FR-006: the same address again is byte-identical, and still one account
post /api/account/registrations '{"email":"someone@example.com","password":"інший-пароль-2026"}'

# FR-002: a password below the policy is rejected, naming the field
curl -s -b "$J" -H 'Content-Type: application/json' -H "X-XSRF-TOKEN: $TOKEN" \
  -X POST -d '{"email":"short@example.com","password":"короткий"}' \
  localhost:5080/api/account/registrations | jq '.errors'

# FR-011: the correct password on an unconfirmed address says so, and only then
post /api/account/session '{"email":"someone@example.com","password":"довгий-пароль-2026"}'   # 403

# FR-015: an unknown address and a wrong password are the same response
post /api/account/session '{"email":"nobody@example.com","password":"довгий-пароль-2026"}'    # 401
post /api/account/session '{"email":"someone@example.com","password":"не той пароль тут"}'    # 401

# FR-016, FR-017, R5: repeated failures answer 429 with Retry-After, and an address with
# no account answers the same way, so the lockout discloses nothing
for _ in $(seq 1 6); do
  post /api/account/session '{"email":"nobody@example.com","password":"не той пароль тут"}'
done
```

Confirm the address by taking the link out of Mailpit rather than the database, because that
is the path a person takes:

```bash
curl -s 'localhost:8025/api/v1/messages?limit=1' | jq -r '.messages[0].ID' \
  | xargs -I{} curl -s localhost:8025/api/v1/message/{} | grep -o 'http://[^"]*confirm[^"]*'
```

Then, with the address confirmed:

```bash
# FR-010, FR-012, FR-013: signing in sets an HttpOnly, SameSite=Strict, 30-day cookie
curl -s -b "$J" -c "$J" -D - -o /dev/null -H 'Content-Type: application/json' \
  -H "X-XSRF-TOKEN: $TOKEN" -X POST \
  -d '{"email":"someone@example.com","password":"довгий-пароль-2026"}' \
  localhost:5080/api/account/session | grep -i 'set-cookie'

# FR-026: the session says who this is and whether they are a verified beneficiary
curl -s -b "$J" localhost:5080/api/account/session | jq

# FR-018: signing out, then the same cookie replayed, is anonymous
curl -s -b "$J" -c "$J" -H "X-XSRF-TOKEN: $TOKEN" -X DELETE localhost:5080/api/account/session
curl -s -b "$J" localhost:5080/api/account/session | jq '.signedIn'   # false

# FR-027, SC-012: the catalogue is unchanged without an account
curl -s localhost:5080/api/books | jq '.totalCount'
curl -s localhost:5080/api/books | grep -iE 'audio|mp3|opus|storage|signed' && echo 'FAIL' || echo 'clean'

# FR-028: every account response says noindex
curl -s -D - -o /dev/null localhost:5080/api/account/session | grep -i 'x-robots-tag'
```

## Prove the three things that are easy to get wrong

These are the ones where a mistake is invisible until it matters, so each is an endpoint test
as well as a command here.

```bash
# FR-022, SC-006: recovering a password refuses the session that existed beforehand, on its
# very next request rather than in thirty minutes. Sign in, keep the jar, recover, then ask
# again with the same jar.
curl -s -b "$J" localhost:5080/api/account/session | jq '.signedIn'   # false afterwards

# FR-003, SC-013: no password is recoverable from stored data or from the log
psql "$VOXLIB_DB_CONNECTION" -c 'select email, password_hash from accounts limit 5;'
grep -riE 'довгий-пароль' /tmp/api.log && echo 'FAIL' || echo 'clean'

# R6: a state-changing call without the antiforgery header is refused
curl -s -b "$J" -o /dev/null -w '%{http_code}\n' -H 'Content-Type: application/json' \
  -X POST -d '{"email":"someone@example.com","password":"довгий-пароль-2026"}' \
  localhost:5080/api/account/session   # 400
```

## Prove the account screens are not indexable and not prerendered

FR-028 and R10. The build must emit no document for an account route, and must emit the
single-page fallback that serves them instead.

```bash
pnpm --filter frontend build
test ! -e frontend/build/client/sign-in/index.html && echo 'not prerendered, correct'
test -e frontend/build/client/__spa-fallback.html && echo 'fallback present'
```

## Prove it is usable without sight or a mouse

```bash
pnpm --filter frontend test:a11y
```

The run covers registering, confirming, signing in, recovering and signing out, and asserts:

- zero critical and zero serious accessibility violations on all five screens, SC-002
- the whole journey completed by keyboard with a visible focus indicator and no trap, SC-003
- a rejected field carries `aria-invalid` and is named by its own message, FR-031
- submitting, failing, succeeding, confirming and signing out are each announced, FR-033
- the email and password fields carry the conventional autocomplete tokens, FR-032

## Run what continuous integration runs

```bash
dotnet format backend/VoxLib.slnx --verify-no-changes
dotnet test backend/VoxLib.slnx
pnpm format:check && pnpm lint && pnpm build
```

The backend tests replace the mail sender in the test host, so they need a database but not
Mailpit, and `ci.yml` is unchanged by this feature. That matters: the job names in it are the
status-check contexts the branch-protection ruleset matches.

## Check the two halves still agree

```bash
pnpm --filter frontend gen:api-types
git diff --exit-code frontend/src/api/schema.d.ts
```

## Which test proves what

| Requirement | Endpoint test |
| --- | --- |
| FR-001, FR-002 | `RegistrationTests`: an account is created; a password below the policy is rejected naming the field |
| FR-003 | `RegistrationTests`: the stored row has a hash and no readable password, and the log holds neither |
| FR-005, FR-015 | `EnumerationTests`: registering, recovering, and both sign-in failures give identical responses for a known and an unknown address |
| FR-006 | `RegistrationTests`: the same registration twice leaves exactly one account |
| FR-004, FR-007, FR-008, FR-009 | `EmailConfirmationTests`: the link confirms; a second visit says already confirmed; an expired one is told apart and offered a replacement; resends are limited |
| FR-010, FR-011 | `SignInTests`: a confirmed account signs in; the correct password on an unconfirmed one says so and only then |
| FR-012, SC-004 | `SessionLifetimeTests`: a new client over the same cookie container is still signed in |
| FR-013 | `SessionLifetimeTests`: the session cookie is HttpOnly, SameSite=Strict and persistent |
| FR-014, FR-022, SC-006 | `PasswordRecoveryTests`: rotating the stamp refuses the earlier cookie on its next request |
| FR-016, FR-017 | `LockoutTests`, `RateLimitTests`: consecutive failures answer 429 with Retry-After, and an unknown address answers the same |
| FR-018 | `SignOutTests`: the next request is anonymous, the captured cookie is refused, another device is unaffected |
| FR-020, FR-021, FR-023 | `PasswordRecoveryTests`: the link sets a password, clears the lockout, and confirms the address |
| FR-024 | `PasswordRecoveryTests`: a used or expired recovery link answers 410 |
| FR-025, FR-026 | `SessionTests`: the session response carries the beneficiary status, false by default |
| FR-027, SC-012 | `AnonymousAccessTests`: every catalogue endpoint still answers with no cookie |
| FR-028 | `AccountHeaderTests`: every response under `/api/account/` carries `X-Robots-Tag: noindex` |
| R6 | `AntiforgeryTests`: a state-changing call without the header is refused |
| FR-029 to FR-034 | `frontend/tests/a11y/account.spec.ts` |

## Known gaps at this stage

- The per-address attempt counter is in memory, so it is per instance. Correct for one instance
  and recorded here rather than solved speculatively; a second instance needs a shared store.
  Sessions and data protection keys are already in the database, so those are not affected.
- Expired session rows are swept when a new session is created rather than by a scheduled job.
  That is right at this size and wants revisiting if sign-ins ever become rare relative to the
  number of sessions outstanding.
- Nothing in the repository serves the built frontend yet, and R10 adds a requirement for
  whatever eventually does: unknown paths must be served `__spa-fallback.html` rather than a
  404, or the account screens will not load on a fresh page load in production.
- The mail provider is deliberately not chosen. `Smtp:*` configuration points at Mailpit in
  development, and at a real host when there is somewhere to deploy. Delivery is outside the
  system's control, which is why every flow that waits on a message offers another one.
- The dev container change is not verified by continuous integration, because nothing in the
  pipeline builds the container. Rebuild locally before merging.
- Beneficiary verification is not built, by decision. Every account is unverified, and the
  policy that reads the claim is registered but attached to no endpoint until audio arrives.
