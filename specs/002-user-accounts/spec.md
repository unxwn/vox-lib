# Feature Specification: Accounts and Sessions

**Feature Branch**: `feature/user-accounts`

**Created**: 2026-09-08

**Status**: Draft

**Input**: User description: "Accounts and authentication. Open registration with email and
password, sign in, sign out, and a session the browser keeps. Cookie-based sessions using
ASP.NET Core Identity for the user store, password hashing, email confirmation and lockout,
but not Identity UI and not MapIdentityApi. Authorization by policy over claims rather than
roles. A Listener concept in VoxLib.Model carrying beneficiary status, kept separate from the
Identity user type in VoxLib.Dal. No audio and no beneficiary verification flow in this
feature: it establishes identity so that later audio access has something to authorise.
Catalogue metadata stays readable without an account."

## Clarifications

### Session 2026-09-08

- Q: Is recovering a forgotten password part of this feature, or a later one?
  A: Part of this feature. Locking an account after repeated failures while holding the
  password in a form it cannot be recovered from means that, without a way to set a new one,
  a person who forgets their password loses their account permanently. The two halves have to
  ship together.
- Q: May a person sign in before confirming their email address?
  A: No. Confirmation is required before the first sign-in. This makes mail delivery a
  dependency on the critical path, which is why every flow that waits on a message can ask
  for another one.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create an account (Priority: P1)

A visitor has browsed the catalogue and wants to be able to listen. They give an email
address and choose a password, and an account exists afterwards. They can do all of this
with a screen reader or with the keyboard alone.

**Why this priority**: Nothing else in this feature exists without it. It is the only story
that creates the identity every later capability authorises against, and on its own it
already turns an anonymous visitor into a known person.

**Independent Test**: Open the sign-up page with a screen reader and with the keyboard only,
create an account, and confirm afterwards that the account exists and that its password
cannot be read back from storage. No audio is involved.

**Acceptance Scenarios**:

1. **Given** a visitor with no account, **When** they submit a well formed email address and
   a password that meets the stated requirements, **Then** an account is created and they
   are told to check their inbox before they can sign in.
2. **Given** a visitor filling in the form, **When** they reach the password field, **Then**
   the requirements the password must meet are available before they submit, not only after
   a failure.
3. **Given** a visitor using a screen reader, **When** a field is rejected, **Then** the
   reason is announced and is tied to the field it concerns, so they know which field to
   correct without hunting.
4. **Given** a visitor using only a keyboard, **When** they move through the form, **Then**
   focus follows reading order, the focused field is visibly marked, and nothing traps focus.
5. **Given** a visitor whose password manager offers to fill the form, **When** it does,
   **Then** the email and password fields are filled correctly and the offer to save the new
   password appears.
6. **Given** an email address that already has an account, **When** a visitor submits it,
   **Then** the response is indistinguishable from the response for a new address, and the
   person is told to check their inbox.
7. **Given** a visitor who submits the form twice because the connection is slow, **When**
   both submissions arrive, **Then** exactly one account exists afterwards.

---

### User Story 2 - Confirm the email address (Priority: P2)

The person opens the message sent when they registered and follows the link in it. The
address is recorded as confirmed and the account becomes usable.

**Why this priority**: It is required before anyone can sign in, so the account created in P1
is inert until it happens. It also proves the address belongs to the person, which is what
makes the address usable for recovery and for the beneficiary verification this feature
deliberately does not build.

**Independent Test**: Register an account, take the link from the sent message, follow it,
and confirm the account's state changes and that sign-in becomes possible. Follow the link a
second time and after its expiry, and confirm both are handled.

**Acceptance Scenarios**:

1. **Given** an account whose address is not confirmed, **When** the person follows the link
   they were sent, **Then** the address is recorded as confirmed, they are told so, and they
   can sign in.
2. **Given** a link that has already been used, **When** it is followed again, **Then** the
   page says the address is already confirmed rather than reporting an error.
3. **Given** a link older than its stated lifetime, **When** it is followed, **Then** the
   person is told it has expired and is offered a new one.
4. **Given** a person who never received the message, **When** they ask for another,
   **Then** one is sent, and repeated requests are limited so the feature cannot be used to
   send mail repeatedly to someone else.
5. **Given** a person using a screen reader, **When** the address is confirmed, **Then** the
   outcome is announced and the way onward to signing in is reachable from where focus lands.

---

### User Story 3 - Sign in and stay signed in (Priority: P3)

A person whose account is confirmed comes back. They sign in once and are still recognised
the next day without signing in again, on the same device.

**Why this priority**: It is what makes an account worth creating. It depends on P1 and P2
existing, and without it every visit would start over.

**Independent Test**: Sign in with a seeded confirmed account, close the browser entirely,
reopen it, and confirm the person is still recognised. Repeat with a screen reader and with
the keyboard alone.

**Acceptance Scenarios**:

1. **Given** a person with a confirmed account, **When** they submit the correct email and
   password, **Then** they are signed in and can tell from the page that they are.
2. **Given** a person who signed in earlier, **When** they close the browser and reopen the
   site, **Then** they are still signed in and are not asked for their password again.
3. **Given** a wrong password, **When** they submit it, **Then** they are told the
   combination was not recognised, in wording that does not reveal whether the address has an
   account.
4. **Given** the correct password for an account whose address is not confirmed, **When**
   they submit it, **Then** they are told the address must be confirmed first and are offered
   another message.
5. **Given** several consecutive failed attempts on the same account, **When** the limit is
   reached, **Then** further attempts are refused for a stated period and the person is told
   when they may try again.
6. **Given** a person using a screen reader, **When** sign-in fails, **Then** the failure is
   announced without them having to search the page for it.
7. **Given** a person whose browser refuses to keep the session, **When** they sign in,
   **Then** they are told plainly that the browser is not keeping them signed in, rather than
   being returned silently to the sign-in page.
8. **Given** a person with no account at all, **When** they browse the catalogue, **Then**
   every piece of catalogue metadata is still readable.

---

### User Story 4 - Recover a forgotten password (Priority: P4)

A person cannot remember their password, or has locked themselves out by guessing. They ask
for a way back in, follow the link they are sent, and choose a new password.

**Why this priority**: Without it, a forgotten password is permanent loss of the account,
because the password cannot be read back and repeated guessing locks the account. It is
ranked above signing out because losing an account for good is worse than not being able to
end a session early.

**Independent Test**: Lock an account by failing sign-in repeatedly, request recovery, follow
the link, set a new password, and confirm the person can sign in, that the lockout is gone,
and that any session that existed beforehand no longer works.

**Acceptance Scenarios**:

1. **Given** a person who cannot sign in, **When** they ask to recover their account,
   **Then** a message is sent to the address, carrying a single-use, time-limited link.
2. **Given** an address with no account, **When** recovery is requested for it, **Then** the
   response is indistinguishable from the response for an address that has one.
3. **Given** a valid recovery link, **When** the person follows it and sets a password that
   meets the stated requirements, **Then** they can sign in with it immediately.
4. **Given** an account that was locked out, **When** its password is set through recovery,
   **Then** the lockout no longer applies.
5. **Given** an account signed in elsewhere, **When** its password is set through recovery,
   **Then** every session that existed beforehand stops working.
6. **Given** a recovery link that has been used or has expired, **When** it is followed,
   **Then** the person is told so and is offered a new one.
7. **Given** an account whose address was never confirmed, **When** its password is set
   through recovery, **Then** the address is also recorded as confirmed.

---

### User Story 5 - Sign out (Priority: P5)

The person finishes, on their own device or on a shared one, and ends the session so that
the next person at that browser is anonymous.

**Why this priority**: It is short and it depends on nothing above it beyond a session
existing, so it is the last slice that can be cut without breaking the others. It cannot be
dropped, because without it a session on a borrowed device cannot be ended at all.

**Independent Test**: Sign in, sign out, and confirm that the following request is anonymous
and that the session cannot be reused even if it was captured beforehand.

**Acceptance Scenarios**:

1. **Given** a signed-in person, **When** they sign out, **Then** the next request is
   anonymous and the page shows them as signed out.
2. **Given** a session that was captured before sign-out, **When** it is replayed afterwards,
   **Then** it is refused.
3. **Given** a person signed in on two devices, **When** they sign out on one, **Then** the
   other stays signed in.
4. **Given** a person using a screen reader, **When** they sign out, **Then** the change of
   state is announced rather than only being visible.

---

### Edge Cases

- What happens when the confirmation message never arrives, or is filtered as spam, given
  that nobody can sign in until it does?
- What happens when the mail provider is unavailable, so that no new account can be confirmed
  and no password can be recovered?
- What happens when a confirmation or recovery link is followed twice, or after it expired?
- What happens when someone registers an address that already has an unconfirmed account?
- What happens when someone registers, or requests recovery for, an address belonging to
  another person?
- What happens when a recovery link is followed after the person remembered their password
  and already signed in?
- What happens when a session expires while the person has a page open in front of them?
- What happens when the password is correct but the account has been disabled?
- What happens when the browser blocks or discards the session, as in private browsing?
- What happens when someone tries a few sign-in attempts against very many different
  addresses, so that per-account lockout alone does not stop them?
- What happens when a password is written entirely in Cyrillic, or contains an emoji?
- What happens when an email address is far longer than the space for it?
- How does a screen reader user learn that a field was rejected, and which one?
- How does a person on a shared device tell whether they are still signed in?

## Requirements *(mandatory)*

### Functional Requirements

#### Registering

- **FR-001**: System MUST allow anyone to create an account with an email address and a
  password, without an invitation, an approval step, or a waiting list.
- **FR-002**: System MUST state what a password must satisfy before it is submitted, and MUST
  reject one that does not, naming what is wrong.
- **FR-003**: System MUST NOT store a password in any form from which the original can be
  recovered, and MUST NOT include a password in any log, error message, or diagnostic output.
- **FR-004**: System MUST send a message to the address given, carrying a single-use,
  time-limited link that records the address as confirmed when followed.
- **FR-005**: System MUST NOT disclose, through the wording of any response, the shape of any
  response, or the time taken to produce it, whether a given email address has an account.
  The single exception is FR-011, where the correct password for that account has already
  been given.
- **FR-006**: System MUST result in exactly one account when the same registration is
  submitted more than once.

#### Confirming the address

- **FR-007**: System MUST record for every account whether its address has been confirmed,
  and MUST record it as confirmed when the link sent to that address is followed.
- **FR-008**: System MUST stop a confirmation link working once it has been used or has
  expired, and MUST distinguish the two outcomes to the person: an address already confirmed
  is not an error, an expired link is offered a replacement.
- **FR-009**: Users MUST be able to request another confirmation message, and the system MUST
  limit how often one is sent to a given address, so the feature cannot be used to send mail
  repeatedly to someone who did not ask for it.

#### Signing in and staying signed in

- **FR-010**: Users MUST be able to sign in with the email address and password they
  registered, once the address is confirmed.
- **FR-011**: System MUST refuse sign-in for an account whose address is not confirmed, MUST
  say that this is the reason, and MUST offer another confirmation message. This reason MUST
  be given only once the password supplied is correct, so that it cannot be used to discover
  whether an address has an account.
- **FR-012**: System MUST keep a signed-in person recognised across page loads and across
  restarting the browser, until they sign out or the session reaches the end of its life.
- **FR-013**: System MUST hold the session so that scripts running in the page cannot read
  it, so that a script injected into a page cannot carry the session elsewhere.
- **FR-014**: System MUST be able to end any session from the server and have that take
  effect on the session's next use, without waiting for the session to expire on its own.
- **FR-015**: System MUST give the same response to an unknown email address as to a known
  address with the wrong password.
- **FR-016**: System MUST refuse further sign-in attempts on an account after a stated number
  of consecutive failures, for a stated period, and MUST tell the person what happened and
  when they may try again.
- **FR-017**: System MUST limit the rate at which registration, sign-in, message-resend, and
  recovery requests are accepted from a single source, so that per-account lockout is not the
  only defence against attempts spread across many addresses.
- **FR-018**: System MUST end the session on sign out, so that the next request is anonymous
  and the ended session is refused if it is presented again.
- **FR-019**: System MUST tell a person plainly when their browser is not keeping them signed
  in, rather than silently returning them to the sign-in page.

#### Recovering a forgotten password

- **FR-020**: Users MUST be able to request a single-use, time-limited link, sent to their
  address, that lets them set a new password without knowing the old one.
- **FR-021**: System MUST clear any lockout in force on an account when its password is set
  through recovery.
- **FR-022**: System MUST end every existing session for an account when its password is set
  through recovery, so that recovering an account taken by someone else evicts them.
- **FR-023**: System MUST record the address as confirmed when a password is set through
  recovery, because following a link sent to that address proves what confirmation proves.
- **FR-024**: System MUST stop a recovery link working once it has been used or has expired,
  and MUST offer a replacement in both cases.

#### What an account is for

- **FR-025**: System MUST record, for every account, whether it has been verified as
  belonging to a person entitled to accessible-format copies, defaulting to not verified.
- **FR-026**: System MUST make an account's identity and that recorded status available to
  access decisions, so that a later capability can be gated on them without changing how
  accounts are stored.
- **FR-027**: System MUST NOT require an account to view any catalogue metadata.
- **FR-028**: System MUST keep every page that belongs to an account out of search engine
  indexes.

#### Accessibility and language

- **FR-029**: System MUST make every account screen fully operable with a screen reader
  alone, including registering, confirming, signing in, recovering, and signing out.
- **FR-030**: System MUST make every account screen fully operable with the keyboard alone,
  with a visible focus indicator and no focus traps.
- **FR-031**: System MUST tie every validation failure to the field it concerns and announce
  it, so that a screen reader user learns which field to correct without searching the page.
- **FR-032**: System MUST identify the email and password fields in the conventional way, so
  that browser autofill and password managers fill and save them correctly.
- **FR-033**: System MUST announce every state change a sighted person would notice, including
  submitting, failing, succeeding, confirming, and signing out.
- **FR-034**: System MUST present every account screen and every message it sends in
  Ukrainian.

### Key Entities

- **Account**: The credential record for one person. Carries an email address, whether that
  address has been confirmed, a password held only in a form from which the original cannot
  be recovered, the count of consecutive failed sign-in attempts, any lockout in force, and
  when it was created.
- **Listener**: The identity that access decisions are made about. Carries whether the person
  is signed in and whether they have been verified as entitled to accessible-format copies.
  It is a separate thing from the credential record, so that how credentials are stored can
  change without touching the rules that use it.
- **Session**: One browser's active sign-in. Belongs to one account, has an end of life, and
  can be ended early by the person, by a password recovery, or by the system.
- **Single-use link**: A time-limited proof that a person controls an email address, issued
  for one purpose, either confirming the address or setting a new password. It belongs to one
  account and stops working once used or expired.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A screen reader user registers, confirms the address, signs in, recovers a
  password and signs out without sighted assistance, in 100% of test runs, on both of the
  screen readers the product targets.
- **SC-002**: An accessibility audit of every account screen reports zero critical and zero
  serious violations.
- **SC-003**: A keyboard-only person completes the same journey with no focus traps, in 100%
  of test runs.
- **SC-004**: A person who signed in and then closed their browser is still recognised when
  they return, for at least 30 days without signing in again.
- **SC-005**: A session presented after its owner signed out is refused in 100% of attempts.
- **SC-006**: A session belonging to an account that has been disabled, or whose password has
  been recovered, stops working on its next use, in 100% of attempts.
- **SC-007**: No response, message, or measurable difference in response time reveals whether
  an email address has an account, across every account screen and endpoint, except where the
  correct password for that account has already been supplied.
- **SC-008**: An account whose address is not confirmed cannot be signed in to, in 100% of
  attempts.
- **SC-009**: A person completes registration in under 2 minutes.
- **SC-010**: A person who has forgotten their password regains access without contacting
  anyone, within 5 minutes of asking, assuming the message reaches them.
- **SC-011**: No page belonging to an account appears in public search engine results.
- **SC-012**: Every piece of catalogue metadata remains reachable with no account, across
  every page the catalogue serves.
- **SC-013**: No password is recoverable from the system's stored data, its logs, or its error
  output, verified across the whole of both.

## Assumptions

- The catalogue specified in `001-browse-catalogue` exists. This feature adds an identity
  beside it and changes nothing a visitor without an account can already see.
- Audio is entirely out of scope. This feature exists so that the audio feature has something
  to authorise against, and it never plays, offers, or reveals the location of audio.
- Beneficiary verification under the Marrakesh VIP Treaty is deliberately not built, because
  there is no organisation to verify against yet. Every account is recorded as unverified. The
  field exists now so that adding the rule later is a change to one access rule rather than a
  migration and an audit of every call site.
- Requiring confirmation before the first sign-in puts message delivery on the critical path.
  Nobody can use a new account until a message arrives, so the choice of mail provider is a
  decision with real consequences at plan time, and every flow that waits on a message offers
  a way to ask for another.
- Password recovery is in scope precisely because lockout and an unrecoverable password are.
  Shipping those two without a way back in would mean a forgotten password destroys an
  account, which for a person navigating by screen reader is a likely event rather than a
  rare one.
- Confirmation and recovery are the same mechanism used for two purposes: a single-use,
  time-limited link sent to the address. Building them as one thing is why recovery costs
  little on top of confirmation.
- Password hashing, lockout counting, and link generation come from an established identity
  component of the platform rather than being written by hand. The screens themselves are
  built as part of the product's own accessible interface, because the ready-made screens
  that ship with such components do not meet the accessibility bar this product is held to.
- The session is held in a browser cookie that page scripts cannot read, and its validity is
  checked against server-held state on every use. A self-contained token that carries its own
  validity was rejected because FR-014 and FR-022 both require ending a session before it
  expires, and because beneficiary status can be withdrawn. The frontend and the API are
  served from one origin, so this costs nothing in complexity.
- Because the session is attached by the browser automatically, every request that changes
  state must be protected against being triggered from another site, and no request that
  changes state is made with a method a browser will follow from a link.
- Sessions last 30 days and are extended by use. That is long by general standards and is
  chosen deliberately: for a person navigating by screen reader, retyping an email address
  and a password is an expensive interruption, and a short session turns every visit into one.
- Permission is decided from what is recorded on the account rather than from membership of a
  named group. There is exactly one distinction today, and a group mechanism for a single
  distinction is the kind of ceremony the constitution defers until something concrete demands
  it.
- An account is an email address and a password. There is no display name, no avatar, no
  profile, and no preferences. Changing the email address on an existing account is out of
  scope.
- Signing in with an account held elsewhere, such as Google or Apple, is out of scope here. It
  is worth revisiting for this audience specifically, because it removes password entry, but
  it is a separate decision with its own dependency.
- There is no administrative capability. Nobody can list, disable, or inspect accounts through
  the product. FR-014 requires only that ending a session is possible, not that a screen
  exists to do it from.
- Messages are sent through an external mail provider chosen when this feature is planned.
  Delivery is outside the system's control, so every flow that depends on a message also
  offers a way to request another.
- The interface and every message are Ukrainian only, matching the catalogue. No second
  language and no translation machinery.
- The pages in this feature are personal to one person and are not generated ahead of time as
  the catalogue pages are, so whether someone is signed in is established after the page loads
  rather than being built into it.
