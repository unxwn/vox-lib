namespace VoxLib.Model.Book;

/// <summary>
/// Whether a book may be seen by the public. This feature only reads the state;
/// the transitions belong to the administrative feature that publishes books.
/// </summary>
public enum PublicationState
{
    Draft = 0,
    Published = 1,
}
