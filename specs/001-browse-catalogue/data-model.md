# Data Model: Browse the Book Catalogue

**Feature**: `specs/001-browse-catalogue/` | **Date**: 2026-09-08

Three homes, as the constitution requires. The domain model below lives in `VoxLib.Model`
and holds behaviour. `VoxLib.Dal` holds the persistence shape and maps to the domain once.
`VoxLib.Api` holds the wire contracts, which are described in `contracts/catalogue.yaml`.

## Domain model (`VoxLib.Model`)

### Book

| Field              | Type                | Notes                                          |
| ------------------ | ------------------- | ---------------------------------------------- |
| `Id`               | `Guid`              | Identity. Never appears in an address.          |
| `Slug`             | `string`            | Unique, lowercase, address-safe. Carries the title into search results. |
| `Title`            | `string`            | Required. Sorted under Ukrainian collation.     |
| `Description`      | `string?`           | Optional. A book may have none.                 |
| `CoverArtUrl`      | `string?`           | Optional. Absent means the placeholder is shown. |
| `Language`         | `string`            | Language tag for this book's metadata, so its page can declare the language a screen reader should read it in. Defaults to Ukrainian. |
| `PublicationState` | `PublicationState`  | `Draft` or `Published`.                         |
| `Authors`          | `IReadOnlyList<Author>`  | At least one for a published book.         |
| `Chapters`         | `IReadOnlyList<Chapter>` | Ordered by `Position`. May be empty.       |

Behaviour on the entity, not in a service:

- `IsListed` returns true only when `PublicationState` is `Published`. Every read path goes
  through it, so an unpublished book cannot leak into a list or a detail view by a caller
  that forgot to filter.
- `TotalRunningTime` is the sum of its chapters' running times, and is `TimeSpan.Zero`
  when it has no chapters yet.
- `ChapterCount` is the number of chapters.

### Author

| Field  | Type     | Notes                                             |
| ------ | -------- | ------------------------------------------------- |
| `Id`   | `Guid`   | Identity.                                          |
| `Name` | `string` | Required. Sorted and matched under Ukrainian collation. |

An author is credited on many books, and a book credits one or more authors.

### Chapter

| Field         | Type       | Notes                                        |
| ------------- | ---------- | -------------------------------------------- |
| `Id`          | `Guid`     | Identity.                                     |
| `BookId`      | `Guid`     | Owning book. A chapter belongs to exactly one. |
| `Title`       | `string`   | Required.                                     |
| `Position`    | `int`      | Starts at 1. Unique within a book.             |
| `RunningTime` | `TimeSpan` | Non-negative.                                  |

### PublicationState

`Draft` or `Published`. This feature only reads. Transitions belong to the administrative
feature that publishes books, so no transition logic is written here. An unpublished book
behaves exactly as if it did not exist: absent from lists, and not found by address.

### Shared primitives (`VoxLib.Model/Common/`)

- `PagedResult<T>` carries `Items`, `Page`, `PageSize`, `TotalCount` and `PageCount`.
  `PageCount` is zero for an empty catalogue.
- `PageRequest` carries a one-based page number and rejects anything below 1. Page size is
  fixed at 20 and is not a caller-supplied value in this feature. A page above the last page
  of a non-empty result set is not found rather than empty, per FR-020.

### Interfaces declared in `VoxLib.Model`

- `IBookRepository` reads published books: a page of them, a page matching a search term,
  and one by slug. Implemented in `VoxLib.Dal`.
- `IBookCatalogue` is the application service the endpoints call. Implemented in
  `VoxLib.Orchestrator`. It owns paging bounds, the published-only rule and ordering.

## Persistence model (`VoxLib.Dal`)

Tables mirror the entities: `books`, `authors`, `book_authors`, `chapters`. The data access
objects are separate types from the domain entities and are mapped once, in `VoxLib.Dal`.

Constraints and indexes that carry requirements:

| Rule                                                     | Enforced by                                   | Requirement |
| -------------------------------------------------------- | --------------------------------------------- | ----------- |
| A slug identifies at most one book                        | Unique index on `books.slug`                   | R8          |
| Titles sort as a Ukrainian reader expects                 | Collation `uk-UA-x-icu` on `books.title`       | FR-015      |
| Author names sort and match the same way                  | Collation `uk-UA-x-icu` on `authors.name`      | FR-015      |
| Listing and ordering stay cheap as the catalogue grows    | Index on `books (publication_state, title, slug)` | FR-005   |
| Paging never repeats or skips a book                      | Order by `title` then `slug`, a total order    | FR-005      |
| Chapters are ordered and unambiguous within a book        | Unique index on `chapters (book_id, position)` | FR-008      |
| A chapter cannot outlive its book                         | Cascade delete from `books` to `chapters`      |             |

The collation is deterministic, which is what keeps substring matching available for
search. Case-insensitive matching is done explicitly with `ILIKE` rather than by folding
case in the collation.

## Validation rules

Drawn from the specification, each one testable:

- `Slug` is required, unique, lowercase, and contains no whitespace. It is also the
  secondary sort key, so it must be present on every book for the order to be total.
- `Language` is required and carries a language tag, so that FR-018 can be satisfied for a
  book whose metadata is not in the interface language.
- `Title` is required and not blank.
- `Description` and `CoverArtUrl` are optional and may be absent on a published book.
- A published book credits at least one author.
- `Position` starts at 1 and is contiguous within a book.
- `RunningTime` is never negative.
- A page request below 1, or above the last page of a non-empty catalogue, is not valid and
  produces a not-found response rather than an empty page.

## What is deliberately absent

No audio field appears on any entity or in any contract. Not a file location, not a
duration that could be mistaken for one, not a storage key. FR-010 is a property of the
model itself here, not something the endpoints have to remember to strip.
