<!--
Sync Impact Report
==================
Version change: unversioned template → 1.0.0 (initial ratification)
Modified principles: none (initial ratification; all seven principles are new)
Added sections:
  - Core Principles: I. Accessibility Is the Product; II. The Dependency Rule;
    III. The API Never Proxies Audio; IV. Access Model; V. Resource-Shaped HTTP;
    VI. Behaviour over Mocks; VII. Earn Every Abstraction
  - Platform & Product Decisions
  - Governance (scope split with CLAUDE.md, delivery, amendments, versioning,
    Constitution Check gates)
Removed sections: none
Templates reviewed (not modified by this command):
  - .specify/templates/plan-template.md: its Constitution Check reads the gates listed
    under Governance at plan time; no edit required
  - .specify/templates/spec-template.md: no edit required
  - .specify/templates/tasks-template.md: no edit required
Sources of truth: CLAUDE.md, docs/ARCHITECTURE.md, .github/workflows/ci.yml
Follow-up TODOs: none
-->

# vox-lib Constitution

This constitution encodes the rules the repository already follows. Its sources of truth
are `CLAUDE.md`, `docs/ARCHITECTURE.md`, and `.github/workflows/ci.yml`. It does not
invent standards beyond them.

## Core Principles

### I. Accessibility Is the Product (NON-NEGOTIABLE)

Target users rely on screen readers (VoiceOver on iOS, TalkBack on Android). A feature
that is not operable with a screen reader is not done.

- Semantic HTML only: native `<button>`, `<nav>`, `<main>` and their kin. No interactive
  `div` elements.
- Every interactive element MUST have a descriptive accessible name, through its visible
  text or an `aria-label` that matches it (for example "Play audiobook: The Diary of
  Sotnyk Ustim").
- State changes such as chapter transitions MUST be announced through
  `aria-live="polite"` regions.
- `navigator.mediaSession` is mandatory, so playback is controllable from lock screens
  and headphone buttons.

### II. The Dependency Rule

`VoxLib.Model` references no other VoxLib project. `.Dal`, `.Platform`, and
`.Orchestrator` depend on it and implement interfaces declared there. `.Api` depends on
`.Model` and `.Orchestrator`, and references `.Dal` and `.Platform` only at the
composition root, to register implementations.

- Domain logic never lives in endpoint handlers.
- Wire contracts live in `.Api/<Feature>/Contracts/`, domain models in `.Model`,
  persistence models in `.Dal`. One class does not serve two of those roles, and each
  boundary maps exactly once: DAO to domain in `.Dal`, domain to contract in `.Api`.
- There are no DTOs in `.Model`; the wire contracts in `.Api` already fill that role.
- Today only `VoxLib.Api` exists. The other projects are created when the first domain
  logic arrives, not before.

Rationale: follow the one rule and the storage provider, the database, and the web
framework all stay replaceable details at the edge.

### III. The API Never Proxies Audio

The API authorises and returns a short-lived signed URL. Object storage serves the bytes
with native HTTP Range support.

- Storage is reached through an interface in `.Model` with the implementation in
  `.Platform`, so changing provider stays a configuration change.
- Service workers do not intercept audio URLs. A handler that replays a cached `200`
  breaks seeking, and signed URLs are not usefully cacheable anyway. Offline listening
  is an explicit download feature designed later, not incidental caching.

Rationale: egress is the dominant cost of streaming audio, and the line between "who may
listen" (application logic) and "serving bytes" (infrastructure) stays clean.

### IV. Access Model

Catalogue metadata is public and indexable. Audio requires a signed-in account and a
short-lived signed URL, and the expiry is what makes the gate real.

- App routes carry `X-Robots-Tag: noindex`. `robots.txt` is advisory and is never the
  protection; authentication and URL expiry are.
- Registration is open. Beneficiary verification is deferred by decision until there is
  an NGO to verify against. When it arrives it is an access rule in `.Model`, not a new
  subsystem.

Rationale: the catalogue must be findable by the people it exists for, while a
long-lived audio URL is shareable and turns the gate into decoration.

### V. Resource-Shaped HTTP

Browser-facing routes live under `/api/`. `/health` stays at the root as the liveness
probe. The frontend calls relative paths only, and Vite proxies them to the API.

Hardcoding `http://localhost:5080` in frontend code breaks the Vite proxy and
reintroduces CORS.

Rationale: the browser talks to one origin, and the catalogue, chapters, libraries, and
playback positions on the roadmap land as resources rather than as one-off RPC calls.

### VI. Behaviour over Mocks

API behaviour is verified end to end with `WebApplicationFactory<Program>` over real
HTTP. Focused unit tests are added for domain logic once it is non-trivial. Mock-heavy
unit tests of endpoints are not the house style.

Behaviour changes ship with tests added or updated, as the pull request template's test
plan requires.

Rationale: endpoint tests catch the wiring mistakes that mocked tests hide.

### VII. Earn Every Abstraction

Most of the catalogue is genuinely CRUD and stays CRUD: plain entities with a repository,
nothing more.

- Aggregate roots, domain events, specifications, CQRS, MediatR, and HLS are deliberately
  deferred until a concrete problem demands them. A plan that adopts one MUST justify it
  in its Complexity Tracking table.
- Rules that do exist live on the entity rather than in a service, so every caller goes
  through them. `Book.IsAudioAudibleBy(listener)` is the current example.
- When a quick hack and a sound approach are close in cost, take the sound one.

Rationale: none of the deferred patterns has a problem to solve at this size, and a rule
written as an `if` inside a service is the one the next caller forgets.

## Platform & Product Decisions

- **Progressive Web App, not native apps.** The app handles copyrighted audiobooks ahead
  of a formal NGO under the Marrakesh VIP Treaty, so app store publication risks
  takedowns and account bans. A PWA is independent of store moderation, installs to the
  home screen, and can move hosting without users updating anything.
- **Stack.** ASP.NET Core minimal APIs on .NET 10 in `backend/`; React 19 with Vite and
  TypeScript in `frontend/`; pnpm; PostgreSQL per the MVP roadmap; Cloudflare R2 for
  audio, chosen for zero egress fees rather than for takedown resistance.
- **Monorepo in a Dev Container.** One repository holds both halves so humans and AI
  coding assistants see the whole context, and all development runs inside
  `.devcontainer/`.

## Governance

**Scope.** `CLAUDE.md` remains the operational manual: commands, container gotchas, git
workflow, CI checks. This constitution governs product and design decisions. Where the
two disagree, this file wins and `CLAUDE.md` is corrected in the same pull request.

**Delivery.** Every change ships through a pull request into `main` (trunk-based, no
`develop` branch), squash merged, with the PR title in Conventional Commits. The CI jobs
`backend (build + test)` and `frontend (lint + build)` must be green before merge.

**Amendments.** An amendment is a pull request that edits this file, updates the Sync
Impact Report at the top, bumps the version, and states what changed and why.

**Versioning.** Semantic versioning. MAJOR when a principle is removed or redefined
incompatibly, MINOR when a principle or section is added or materially expanded, PATCH
for clarifications and wording.

**Constitution Check.** Every `plan.md` answers these before Phase 0 research and again
after Phase 1 design. A "no" blocks the plan until the design changes or the violation
is justified in the Complexity Tracking table.

1. Accessibility: is every UI story operable by screen reader and keyboard, with
   announcements and media session covered wherever playback is involved?
2. Dependency rule: does `.Model` stay free of outward references, and is every business
   rule outside the endpoint handlers?
3. Audio and access: does any path move audio bytes through the API, hand out a
   long-lived URL, expose audio without a signed-in account, or let a service worker
   cache audio?
4. HTTP: are new routes resources under `/api/`, called by relative path?
5. Tests: does every behaviour change name the endpoint test that proves it?
6. Abstractions: is each deferred item from Principle VII that the plan adopts justified
   in Complexity Tracking?

**Version**: 1.0.0 | **Ratified**: 2026-09-08 | **Last Amended**: 2026-09-08
