# Specification Quality Checklist: Accounts and Sessions

**Purpose**: Validate specification completeness and quality before proceeding to planning

**Created**: 2026-09-08

**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

Both clarifications are resolved and recorded in the spec's Clarifications section. Password
recovery is in scope, and confirming the address is required before the first sign-in.

Two consequences of those answers are worth carrying into planning rather than discovering
during it. Requiring confirmation puts mail delivery on the critical path, so an account is
unusable until a message arrives and the provider choice stops being incidental. And it
collides with the no-enumeration rule, because telling someone their address is unconfirmed
admits the address has an account. FR-011 resolves the collision by giving that reason only
after the correct password has been supplied, and FR-005 and SC-007 both carry the matching
exception.

The stack choices that prompted this feature (an established identity component, a session
held in a browser cookie and checked server side, permission decided from recorded account
attributes rather than group membership) are recorded in Assumptions with the reasoning that
led to each, not stated as requirements. That follows how `001-browse-catalogue` records its
rendering and storage decisions, and keeps the requirements themselves about observable
behaviour. Three of them are load-bearing and are also expressed behaviourally so they can be
tested: FR-013 rules out holding the session anywhere a page script can read it, and FR-014
with FR-022 rule out a session whose validity cannot be withdrawn before it expires.

The one deliberate exception to "no implementation details" is the word cookie in the
Assumptions section. It names a web platform primitive rather than a framework, and writing
around it would obscure the decision instead of deferring it.
