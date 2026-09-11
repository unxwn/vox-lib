# Specification Quality Checklist: Visual Identity for the Catalogue

**Purpose**: Validate specification completeness and quality before proceeding to planning

**Created**: 2026-09-10

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

The description was detailed enough that no clarification markers were needed. One scope
question was raised in the completion report and answered in the Clarifications section: the
restyle covers every page the site serves today, the five account screens included, not only the
catalogue pages the original description listed. FR-001 now carries that list, FR-021 to FR-024
carry what the forms bring with them, and the assumption that limited composition work to the
catalogue has been removed.

The typeface is now settled rather than assumed, and it went the opposite way to the first
draft. Fixel was chosen over e-Ukraine on licensing before aesthetics: Fixel is under the SIL
Open Font License, while e-Ukraine is offered under CC-BY 4.0, which is not a font licence and
which Creative Commons themselves advise against for fonts. For a product that intends to ship,
that difference outweighs the choice between two competent grotesques. Fixel also carries a
complete Ukrainian layout, which is the failure mode that quietly breaks Cyrillic interfaces: a
face missing ґ or ї substitutes a glyph from another family and the mismatch is easy to
overlook. SC-011 exists to catch exactly that, and SC-012 to catch the other common regression,
a page that shows nothing at all while the typeface is still arriving.

FR-024 came out of checking how reversed text behaves rather than out of the description. Light
text on a dark ground blooms, which is uncomfortable generally and painful for readers with
astigmatism, so the highest available contrast is not the most readable setting. That is worth
flagging because it pulls against the instinct FR-006 creates.
Three requirements are phrased to stay testable where the description was qualitative. FR-003
turns "reads as texture rather than as a headline" into two checkable statements about size and
placement. FR-006 pins "measured over the decorated ground" to the lightest and darkest point
actually rendered behind the text, so the check has a defined worst case rather than an average.
FR-012 fixes "large enough to hit without precision" at 44 by 44 device-independent pixels, the
larger of the two thresholds in common use, chosen because the product's audience already
depends on assistive technology.

SC-010 exists to stop this feature quietly regressing an earlier commitment: a generated grain
and a repeated watermark are exactly the kind of decoration that costs load time, and
`001-browse-catalogue` promised the catalogue is readable in 3 seconds on a slow connection.
