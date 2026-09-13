namespace VoxLib.Api.Book.Contracts.Responses;

/// <summary>
/// A book's cover as the set of widths that were prepared for it. The caller
/// picks one; nothing is resized on request, which is FR-045.
/// <para>
/// A set rather than the single URL version 1 carried, because a browser can
/// choose better than a server can guess: it knows the viewport, the pixel
/// density and the layout the image landed in. Every URL is public, unsigned
/// and cacheable, which is what lets a cover be indexed and used as a preview
/// when a link is shared.
/// </para>
/// </summary>
/// <param name="Sources">At least one. Never empty: a book with no cover has no
/// <see cref="Cover"/> at all rather than one with nothing in it.</param>
public sealed record Cover(IReadOnlyList<CoverSource> Sources);

/// <param name="Url">Public, unsigned and absolute; object storage serves it,
/// not this API.</param>
/// <param name="Width">The intrinsic width of the prepared image, in pixels.</param>
public sealed record CoverSource(string Url, int Width);
