# Manual Screen Reader Verification: Visual Identity

**Feature**: `specs/003-visual-identity/` | **Requirement**: SC-003, FR-008, Constitution Principle I

Automated checks catch violations and regressions. They do not prove the product is usable by
someone who cannot see it. Principle I says a feature that is not operable with a screen reader
is not done, so this feature ends with a pass on real assistive technology and the result is
recorded here before it is called complete.

Record the assistive technology version, the operating system version and the browser, because
behaviour differs between them and a pass on one is not a pass on another.

This feature is unusual in what it asks a listener for. Everywhere else the question is whether
something is announced correctly. Here the question is whether something is announced **at all**,
and the answer has to be no. The wordmark "ZALIZNA biblioteka" repeats dozens of times on every
page. If it is reachable it will not be subtle, and it will not be a defect anyone has to look
for: it will be unmistakable. What is being listened for is silence.

## What the automated run already establishes (T020, T021, T024)

Recorded here so that the manual pass can concentrate on what it alone can decide, and so that
whoever runs it knows what has and has not already been ruled out.

- `contrast.spec.ts` asserts, on every one of the ten pages in scope, that the ground element
  carries `aria-hidden="true"`, that nothing inside it re-exposes itself, that nothing inside it
  carries a role, that the string `ZALIZNA` does not appear anywhere in the browser's own
  accessibility tree, and that the ground contains no focusable node.
- `contrast.spec.ts` also asserts that removing the decoration from the document changes neither
  what the keyboard reaches nor the order it reaches it in.
- `target-size.spec.ts` walks every interactive element on every page in scope and asserts a
  44 by 44 box and a visible focus indicator at every tab stop.
- The eight specs that existed before this feature pass **unmodified**, which is SC-008: what
  each page says, announces and does is what it said, announced and did before.

None of that is a substitute for the table below. A tree that looks right in Chromium is not the
same as a page that reads correctly under VoiceOver, and no automated check can hear a page.

## The pass (T025)

Follow the procedure in `specs/001-browse-catalogue/manual-verification.md` over the same
journeys, and compare what is announced against what that document records. Any difference at
all is a failure: FR-015 says this feature changes nothing about what a page says.

**Status: outstanding.** It needs an iOS device with VoiceOver and an Android device with
TalkBack, neither of which is available to the environment this feature was built in.

| Field | Value |
| --- | --- |
| Date | |
| VoiceOver, iOS version, browser | |
| TalkBack, Android version, browser | |
| The wordmark is never announced, on any page | |
| The grain is never announced, on any page | |
| Nothing decorative takes a swipe or a tab stop | |
| Announcements identical to the 001 record, catalogue and book | |
| Announcements identical to the 001 record, search and the account screens | |
| Focus indicator visible at every stop on the dark ground | |
| Outcome | |
| Notes and defects raised | |
