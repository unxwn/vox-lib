# Architectural Proposal: Audio Library for the Visually Impaired

This document outlines the foundational architecture for an audiobook application designed for visually impaired users. It is structured as a Request for Comments (RFC) to serve as a baseline for technical discussions, refinements, and team decisions.

## 1. Platform Strategy: Why PWA over Native?

Because the application will handle copyrighted audiobooks (prior to establishing a formal NGO to leverage the Marrakesh VIP Treaty), publishing native apps on the App Store or Google Play carries a critical risk of immediate DMCA takedowns and developer account bans.

- **Decision:** Develop a Progressive Web App (PWA).
- **Advantages:** Complete independence from app store moderation. Users can add the site to their home screens, where it behaves like a native application. If domain blocking occurs, the backend and frontend can be migrated to bulletproof hosting without requiring users to download app updates.

## 2. Technology Stack (For Discussion)

A monolithic architecture is unviable because audio playback must remain uninterrupted during UI navigation (requiring a Single Page Application approach).

| Component           | Proposed Technology             | Status                                                                                                                                                 |
| :------------------ | :------------------------------ | :----------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Backend**         | ASP.NET Core Web API            | **Decided.** PostgreSQL is the system of record, created with Ukrainian collation. Migration tooling (Liquibase vs. EF Core Code-First) is still open. |
| **Frontend**        | React (Vite)                    | **Decided.** Vite SPA with `vite-plugin-pwa`. Next.js was rejected; see the rendering note below.                                                      |
| **Audio Player**    | Native `<audio>` or `howler.js` | Still open. `howler.js` provides better cross-browser compatibility for background audio, but native `<audio>` with pure JS might be lighter.          |
| **Package Manager** | pnpm                            | **Decided.** Chosen for disk efficiency and strict dependency resolution.                                                                              |

**Decision: Vite, with catalogue pages generated at publish time.** The public catalogue has
to be indexable by search engines, so a catalogue or book page must arrive as a complete
document rather than the empty root element a client-rendered app ships. That is satisfied
by generating those pages whenever content is published and hydrating them into the
client-side application, whose in-page navigation is what keeps playback uninterrupted.
Next.js was rejected because its server rendering would put a Node process in production
alongside the .NET API, which a catalogue of this size does not justify. The cost of the
choice is that publishing a book has to trigger a regeneration of the pages it appears on,
so a new book becomes visible after that regeneration rather than instantly. Search result
pages depend on what the visitor typed and are deliberately not indexable.

## 3. Accessibility (a11y): Core Priority

Since the target audience relies heavily on screen readers (VoiceOver on iOS, TalkBack on Android), the UI/UX must be built strictly adhering to WAI-ARIA standards.

- **Semantic HTML:** Strict use of native tags (`<button>`, `<nav>`, `<main>`). No interactive `div` elements.
- **Descriptive Attributes:** Every interactive element must utilize `aria-label` (e.g., "Play audiobook: The Diary of Sotnyk Ustim").
- **Dynamic Announcements:** Implement `aria-live="polite"` for non-intrusive screen reader state updates (e.g., announcing chapter transitions).
- **OS Integration:** Mandatory implementation of `navigator.mediaSession` to allow playback control from lock screens and hardware headphone buttons.

## 4. Audio Hosting and Storage Strategy

Audio is served from S3-compatible object storage. The .NET API never proxies audio
bytes: it checks access and returns a short-lived signed URL, and storage serves the
bytes with native HTTP Range support. This keeps streaming bandwidth off the API and
draws a clean line between "who may listen", which is application logic, and "serving
bytes", which is infrastructure.

Access is behind an interface in `VoxLib.Model` with the implementation in
`VoxLib.Platform`, so the provider can change without touching anything above it.

**Decision: Cloudflare R2 from the start.** Zero egress fees, and egress is the
dominant cost for audio streaming. Telegram as a CDN was considered and rejected:
the Bot API 20MB limit forces an MTProto userbot, which puts a ban-prone user account
on the request path, makes the API proxy every byte, and requires mapping HTTP Range
onto Telegram's chunked download API by hand.

**Redis was considered and rejected for audio.** It is an in-memory store with a
practical value ceiling around 512MB, so holding 50MB+ blobs in RAM is the wrong
shape. Redis may still earn a place later for small hot values: session state, access
lookups, catalogue query caching, rate limiting, and coalescing playback position
writes.

**HLS is deferred.** Range requests against a sanely encoded file cover the MVP. HLS
earns its place when adaptive bitrate is needed for users on poor mobile connections,
and it will be generated by an `ffmpeg` background worker at that point, not by hand.
Note that HLS supports `#EXT-X-BYTERANGE`, so segments can be byte ranges into a
single file rather than thousands of physical chunks.

**Encoding matters more than chunking.** HLS does not reduce total bytes. Spoken word
needs far less than music: 64 kbps mono MP3 is roughly 29 MB per hour, 32 kbps mono
roughly 14 MB, and Opus at 24 to 32 kbps is transparent for speech at roughly 11 to
14 MB. A ten hour book should land near 150 to 300 MB in total. Keep received masters,
serve a derived speech-bitrate rendition.

**On jurisdiction, stated plainly so it is not assumed.** Cloudflare R2 and Backblaze
B2 are US providers that process DMCA takedowns for content they host. They are chosen
here for cost, not for takedown resistance. Hosts such as FlokiNET or AlexHost are a
different category, answering to local court orders rather than DMCA notices. If
takedown resistance becomes the priority, that is a separate migration, and the storage
adapter exists so it stays a configuration change rather than a rewrite.

## 5. Repository Structure and Environment

The codebase will utilize a **Monorepo** architecture. This is critical for maintaining complete context when working with AI coding assistants (like Claude Code) inside VS Code DevContainers.

- **Root Directory:** `vox-lib` (kebab-case).
- **Frontend:** `/frontend` directory utilizing JS ecosystem conventions (camelCase/kebab-case).
- **Backend:** `/backend` directory utilizing strict C# PascalCase conventions (e.g., `VoxLib.Api`, `VoxLib.Dal`, `VoxLib.Model`, `VoxLib.Orchestrator`, `VoxLib.Platform`).
- **Environment:** VS Code with Docker-based DevContainers to ensure AI agents run in isolated sandboxes.
- **AI Management:** Tiered `CLAUDE.md` files located at the root, frontend, and backend levels to provide granular, domain-specific instructions to AI agents.

## 6. Product Roadmap

- **MVP:** React PWA, ASP.NET Core, PostgreSQL. Cloudflare R2 storage behind signed URLs. Public landing and catalogue, registration-gated audio. Core accessibility and MediaSession setup.
- **V2:** Background audio processing pipelines (Message Queues/Background Workers) and automated `ffmpeg` HLS generation.
- **V3:** Model Context Protocol (MCP) integration, autonomous AI Agent deployment for maintenance, and robust observability.
- **V4:** Voice-interactive AI assistants for users, intelligent search, and semantic content recommendations based on listening habits.

## 7. Known Sharp Edges

Decisions to make deliberately before the relevant code exists, recorded here so
they are not discovered late.

### Service workers and HTTP Range requests

A naive service worker `fetch` handler that caches audio will break seeking. Range
requests must be honoured with `206 Partial Content`; a handler that replays a full
cached `200` leaves the player unable to seek, and on some browsers unable to play
at all. This is the most common cause of "audio works in dev, breaks once installed
as a PWA".

Two viable options:

- **Bypass.** Exclude audio URLs from the service worker entirely. Simplest, and
  correct while audio is served from short-lived signed object storage URLs, which
  are not usefully cacheable anyway. Offline listening then becomes an explicit
  download feature rather than incidental caching.
- **Range-aware handler.** Intercept audio, serve from Cache Storage or IndexedDB,
  and slice the stored blob to satisfy `Range`, returning 206 with correct
  `Content-Range` and `Accept-Ranges` headers.

**MVP decision: bypass.** Explicit offline downloads move to V2, where they are
designed as a feature rather than inherited from cache behaviour.

### Offline playback position

Position updates written while offline must not be lost. Queue them in IndexedDB
and flush on reconnect via the Background Sync API, with a fallback flush on next
app start where Background Sync is unavailable (notably Safari, which matters given
the VoiceOver audience). Server side, resolve with last write wins on a client
`updatedAt` timestamp: one user on two devices is the normal case, not a genuine
conflict needing merge.

## 8. Access Model

Three separable decisions, decided independently.

**Public:** landing page and catalogue metadata (title, author, cover, description,
chapter list). Indexable by search engines, because the users this exists for have to
be able to find it.

**Private:** audio. Reachable only by a logged-in account, and only through a
short-lived signed URL. The URL expiry is what makes the gate real; a long-lived URL
is shareable and turns the gate into decoration.

**Registration is open.** Anyone may sign up. Beneficiary verification under the
Marrakesh VIP Treaty is deliberately not implemented yet, because there is no NGO to
verify against. This is a known and accepted exposure, revisited when the NGO exists.
At that point verification becomes an access rule in `VoxLib.Model` rather than a new
subsystem.

App routes carry `X-Robots-Tag: noindex`. Note that `robots.txt` is advisory and is
never the protection: authentication and URL expiry are.

## 9. Backend Structure

Five projects. The dependency rule below is what makes this an onion rather than a
stack of layers.

| Project               | Holds                                                                   |
| --------------------- | ----------------------------------------------------------------------- |
| `VoxLib.Api`          | Endpoints, auth, DI composition root, wire contracts under `Contracts/` |
| `VoxLib.Model`        | Domain entities, value objects, every interface, shared primitives      |
| `VoxLib.Orchestrator` | Application services implementing the interfaces in `.Model`            |
| `VoxLib.Dal`          | DAOs, repositories, mapping, `Persistence/` for DbContexts              |
| `VoxLib.Platform`     | Edge services: object storage, configuration, external providers        |

**The dependency rule: `VoxLib.Model` references no other VoxLib project.** Everything
else points inward at it. `.Dal`, `.Platform` and `.Orchestrator` each depend on
`.Model` and implement interfaces declared there. `.Api` depends on `.Model` and
`.Orchestrator`, and references `.Dal` and `.Platform` only at the composition root to
register implementations. Follow that one rule and the storage provider, the database
and the web framework all stay replaceable details at the edge.

### Three model types, three homes

The mistake to avoid is one class doing all three jobs.

| Type              | Lives in               | Shaped for                   |
| ----------------- | ---------------------- | ---------------------------- |
| Wire contract     | `.Api/Book/Contracts/` | HTTP, versioned with the API |
| Domain model      | `.Model/Book/Book.cs`  | invariants and behaviour     |
| Persistence model | `.Dal/Book/BookDao.cs` | the database schema          |

Each boundary maps exactly once: DAO to domain in `.Dal`, domain to contract in `.Api`.
There are deliberately no DTOs in `.Model`; the wire contracts in `.Api` already fill
that role, and having both means mapping DTO to DTO for no gain.

### Folder layout

```
VoxLib.Api/Book/BookEndpoints.cs
VoxLib.Api/Book/Contracts/Requests/CreateBookRequest.cs
VoxLib.Api/Book/Contracts/Responses/BookListResponse.cs
VoxLib.Model/Book/Book.cs
VoxLib.Model/Book/IBookRepository.cs
VoxLib.Model/Book/IBookOrchestrator.cs
VoxLib.Model/Common/PagedResult.cs
VoxLib.Model/Common/PaginationParams.cs
VoxLib.Orchestrator/Book/BookOrchestrator.cs
VoxLib.Dal/Book/BookDao.cs
VoxLib.Dal/Book/BookRepository.cs
VoxLib.Dal/Book/BookMappingProfile.cs
VoxLib.Dal/Persistence/VoxLibDbContext.cs
VoxLib.Model/Storage/IAudioStorage.cs
VoxLib.Platform/Storage/R2AudioStorage.cs
```

Folder names are singular and scoped by feature; contract folders are plural
(`Requests/`, `Responses/`) with singular type names.

### How much DDD

Selectively, where rules exist, and not as a house style.

Most of the catalogue is genuinely CRUD. Books, authors and chapters have no invariants
worth protecting, so plain entities with a repository are correct and anything more is
ceremony.

Where rules do exist, they belong on the entity rather than in a service. The access
rule is the current example:

```csharp
// VoxLib.Model/Book/Book.cs
public bool IsAudioAudibleBy(Listener listener) => listener.IsRegistered;
```

Written that way, the rule travels with the object and every caller goes through it.
Written as an `if` inside `BookOrchestrator`, the second caller added later (a download
endpoint, a background job, an admin tool) can simply forget it. That is the whole
practical argument for a rich model, and it is the reason `.Model` holds behaviour and
not only interfaces.

Today that rule is one line, because registration is open. It grows teeth when
Marrakesh verification arrives, and the point of putting it here now is that its
growth is then a change to one method rather than an audit of every call site.

**Deliberately not doing yet:** aggregate roots, domain events, specifications, CQRS,
MediatR. None of them have a problem to solve at this size. Add them when something
concrete demands it.
