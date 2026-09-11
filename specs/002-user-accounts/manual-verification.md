# Manual Screen Reader Verification: Accounts and Sessions

**Feature**: `specs/002-user-accounts/` | **Requirement**: SC-001, SC-003, Constitution Principle I

Automated checks catch violations and regressions. They do not prove the product is usable by
someone who cannot see it. Principle I says a feature that is not operable with a screen
reader is not done, so each story ends with a pass on real assistive technology and the result
is recorded here before the story is called complete.

Record the assistive technology version, the operating system version and the browser, because
behaviour differs between them and a pass on one is not a pass on another.

This feature carries a second reason to insist on the manual pass. SC-001 asks that a screen
reader user register, confirm, sign in, recover a password and sign out **without sighted
assistance**, and three of those five steps involve leaving the site for a mail client and
coming back. No browser test covers that journey, because the journey is not in one browser.

## Story 1, creating an account (T052)

**Status: outstanding.** Everything else in Story 1 is complete and
`frontend/tests/a11y/account-register.spec.ts` passes: zero critical and serious axe
violations, the password requirements attached to the password field before submission, every
control reachable by keyboard with a visible focus indicator, a rejected field carrying
`aria-invalid` and named by its own message, the conventional autocomplete tokens, and focus
moving to the outcome. None of that is a substitute for this table.

| Field | Value |
| --- | --- |
| Date | |
| VoiceOver, iOS version, browser | |
| TalkBack, Android version, browser | |
| Password requirements readable before submitting | |
| A rejected field announced, with the reason, and identified | |
| The outcome announced and focus following it | |
| A password manager offered to save the new password | |

## Story 2, confirming the address (T063)

**Status: outstanding.** `frontend/tests/a11y/account-confirm.spec.ts` passes.

This is the step that leaves the browser, so it is the one most likely to strand somebody.
Follow the link from the real mail client, not from a pasted address.

| Field | Value |
| --- | --- |
| Date | |
| VoiceOver, iOS version, browser | |
| TalkBack, Android version, browser | |
| The link in the message is reachable and identifiable in the mail client | |
| The outcome announced on arrival | |
| The way onward to signing in reachable from where focus lands | |
| A second visit announced as already confirmed, not as an error | |
| An expired link announced, with a way to ask for another | |

## Story 3, signing in and staying signed in (T079)

**Status: outstanding.** `frontend/tests/a11y/account-sign-in.spec.ts` passes.

| Field | Value |
| --- | --- |
| Date | |
| VoiceOver, iOS version, browser | |
| TalkBack, Android version, browser | |
| A failed sign-in announced without searching the page | |
| A password manager filled the form | |
| Still signed in after closing and reopening the browser | |
| Whether the person is signed in is discoverable from any page | |

## Story 4, recovering a forgotten password (T092)

**Status: outstanding.** `frontend/tests/a11y/account-recover.spec.ts` passes.

Lock the account first by failing sign-in repeatedly, so the pass covers the state most people
will actually be in when they reach this screen.

| Field | Value |
| --- | --- |
| Date | |
| VoiceOver, iOS version, browser | |
| TalkBack, Android version, browser | |
| The recovery request outcome announced | |
| The new password requirements readable before submitting | |
| The change of password announced | |
| Signing in with the new password works, with the lockout gone | |
| A session open on another device stopped working | |

## Story 5, signing out (T099)

**Status: outstanding.** `frontend/tests/a11y/account-sign-out.spec.ts` passes.

| Field | Value |
| --- | --- |
| Date | |
| VoiceOver, iOS version, browser | |
| TalkBack, Android version, browser | |
| The sign-out control reachable and identifiable from any page | |
| The change of state announced rather than only shown | |

## The whole journey, SC-001

The five tables above cover the stories one at a time. SC-001 is about doing all of it in one
sitting, without sighted assistance, which is a different thing: it is where a step that is
individually fine but leads nowhere shows up.

| Field | Value |
| --- | --- |
| Date | |
| Assistive technology and versions | |
| Completed register, confirm, sign in, recover, sign out unaided | |
| Time taken to register, against SC-009's two minutes | |
| Time from asking for recovery to being back in, against SC-010's five minutes | |
