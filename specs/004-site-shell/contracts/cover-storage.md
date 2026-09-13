# Contract: Cover storage

**Feature**: `specs/004-site-shell/` | **Date**: 2026-09-11

What the catalogue may ask of object storage, and what it may not. The point of writing it down
is the second half: this interface is deliberately incapable of expressing a private object, so
the audio feature cannot arrive by widening it.

## The interface

`VoxLib.Model/Storage/ICoverStorage.cs`. Implemented once, by `S3CoverStorage` in
`VoxLib.Platform/Storage/`, against MinIO in development and Cloudflare R2 in production.

```csharp
public interface ICoverStorage
{
    /// The public, unsigned, cacheable address of one prepared width.
    /// Pure composition from configuration: touches no network and cannot fail.
    Uri PublicUrl(string coverKey, int width);

    /// Whether an object is already stored. Used by seeding, never by a read path.
    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken);

    /// Stores one prepared image. Used by seeding, never by a read path.
    Task PutAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken);
}
```

Three members, and the absences are the contract:

- **No signing.** Covers are public catalogue metadata under Principle IV. A cover has to be
  cacheable, indexable and usable as a preview when a link is shared, and all three stop being
  true the moment the URL expires.
- **No delete, no list.** Nothing in this feature removes or enumerates a cover. An
  administrative surface is out of scope, and an interface that can already do the thing is how
  an out-of-scope feature arrives by accident.
- **No bucket administration.** Creating the bucket and making it public-read is done outside the
  application, because it is the one thing every provider does differently: `PutBucketPolicy`
  works on MinIO, is not supported by R2, and SeaweedFS reads public read from an identity file.
  An implementation that set the policy itself would carry a development-only branch inside the
  production adapter, which is the exact shape the interface exists to prevent.
- **No `GetAsync`.** The API never serves cover bytes. The browser fetches them from storage
  directly, which is the same division of labour Principle III draws for audio.

`IAudioStorage` is a separate interface that does not exist yet. It is not a generalisation of
this one: covers are public and cacheable, audio is signed and must never be cached, and one
interface serving both would have to be told on every call which it is holding.

## Objects

| | |
| --- | --- |
| Bucket | `vox-lib-covers` |
| Key | `covers/{cover-key}-{width}.webp` |
| Widths | 160, 320 |
| Content type | `image/webp` |
| Access | public read, anonymous, never signed |

`{cover-key}` is the stem stored on the book row, which is the book's slug for every cover
committed with this feature. It is not a URL: a stored URL would make changing provider a data
migration instead of a configuration change.

Two widths rather than three: three of the four source images are narrower than 640 pixels, so a
640 object would be an upscale carrying no more detail than the 320. The third width returns when
a source wide enough to justify it does.

## Configuration

| Key | Development | Production |
| --- | --- | --- |
| `Covers:ServiceUrl` | `http://localhost:9000` | the account's R2 endpoint |
| `Covers:Bucket` | `vox-lib-covers` | `vox-lib-covers` |
| `Covers:PublicBaseUrl` | `http://localhost:9000/vox-lib-covers` | the bucket's public hostname |
| `Covers:AccessKey` / `SecretKey` | the compose file's MinIO root credentials | R2 API token |
| `Covers:ForcePathStyle` | `true` | `false` |

`PublicBaseUrl` is separate from `ServiceUrl` because they are genuinely different addresses in
production: writes go to the R2 endpoint with credentials, reads come from a public hostname with
none.

Development values are committed. They are a container-local MinIO with well-known root
credentials on a private network namespace, exactly as the Mailpit credentials are; no production
secret is ever in the repository.

## Seeding

`CoverSeeder`, in `VoxLib.Dal/Seed/`, called from the composition root's startup scope beside
`CatalogueSeeder`.

For each cover embedded in the assembly: `ExistsAsync`, and `PutAsync` only if it is absent. Per
object, not per run — the catalogue and the bucket are separate volumes with separate lifetimes,
and a container rebuilt with a surviving database and an empty bucket must still get its covers.

The prepared images are committed under `backend/src/VoxLib.Dal/Seed/covers/` as embedded
resources, alongside `books.json`. Nothing is ever served from the repository: they are input to
the upload and are never a fallback for it.

## Failure

| What fails | What happens |
| --- | --- |
| storage unreachable when a page renders | every page renders; covers are replaced by the placeholder |
| one object missing | that book shows the placeholder; every other book is unaffected |
| storage unreachable during seeding | startup logs it and continues; the catalogue still serves |

Composing a URL touches no network, so a catalogue response never fails because of storage
(FR-051). The browser's `<img>` does fail, and a failed `<img>` is the broken-image icon SC-015
forbids, so `CoverArt.tsx` keeps an `onError` that swaps to the placeholder — the same designed
state a book with no cover already gets.
