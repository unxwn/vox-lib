using Microsoft.EntityFrameworkCore;
using VoxLib.Dal.Book;

namespace VoxLib.Dal.Persistence;

/// <summary>
/// The catalogue's schema. The collation and the indexes configured here are not
/// tuning: they are where several requirements are actually enforced, so the
/// migration they generate is worth reading.
/// </summary>
public sealed class VoxLibDbContext(DbContextOptions<VoxLibDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Deterministic ICU collation. Deterministic is the point: it gives the sort
    /// order FR-015 asks for while leaving pattern matching available, which a
    /// non-deterministic collation would forbid and so would break the substring
    /// search in FR-013.
    /// </summary>
    public const string UkrainianCollation = "uk-UA-x-icu";

    public DbSet<BookDao> Books => Set<BookDao>();

    public DbSet<AuthorDao> Authors => Set<AuthorDao>();

    public DbSet<ChapterDao> Chapters => Set<ChapterDao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookDao>(book =>
        {
            book.ToTable("books");
            book.HasKey(b => b.Id);
            book.Property(b => b.Id).HasColumnName("id");
            book.Property(b => b.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
            book.Property(b => b.Title)
                .HasColumnName("title")
                .HasMaxLength(500)
                .UseCollation(UkrainianCollation)
                .IsRequired();
            book.Property(b => b.Description).HasColumnName("description");
            book.Property(b => b.CoverArtUrl).HasColumnName("cover_art_url").HasMaxLength(2000);
            book.Property(b => b.Language).HasColumnName("language").HasMaxLength(35).IsRequired();
            book.Property(b => b.PublicationState)
                .HasColumnName("publication_state")
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            // A slug identifies at most one book, which is what makes it safe to
            // put in an address.
            book.HasIndex(b => b.Slug).IsUnique().HasDatabaseName("ix_books_slug");

            // Covers the listing exactly as it is read: published books ordered
            // by title and settled by slug. The slug is in the index because the
            // order has to be total, not merely sorted.
            book.HasIndex(b => new { b.PublicationState, b.Title, b.Slug })
                .HasDatabaseName("ix_books_publication_state_title_slug");
        });

        modelBuilder.Entity<AuthorDao>(author =>
        {
            author.ToTable("authors");
            author.HasKey(a => a.Id);
            author.Property(a => a.Id).HasColumnName("id");
            author.Property(a => a.Name)
                .HasColumnName("name")
                .HasMaxLength(300)
                .UseCollation(UkrainianCollation)
                .IsRequired();

            author.HasIndex(a => a.Name).HasDatabaseName("ix_authors_name");
        });

        modelBuilder.Entity<ChapterDao>(chapter =>
        {
            chapter.ToTable("chapters");
            chapter.HasKey(c => c.Id);
            chapter.Property(c => c.Id).HasColumnName("id");
            chapter.Property(c => c.BookId).HasColumnName("book_id");
            chapter.Property(c => c.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            chapter.Property(c => c.Position).HasColumnName("position");
            chapter.Property(c => c.RunningTime).HasColumnName("running_time");

            chapter
                .HasOne(c => c.Book)
                .WithMany(b => b.Chapters)
                .HasForeignKey(c => c.BookId)
                // A chapter cannot outlive its book.
                .OnDelete(DeleteBehavior.Cascade);

            // Chapters are ordered and unambiguous within a book: no two share a
            // position, so "in order" is a fact about the data rather than a hope.
            chapter
                .HasIndex(c => new { c.BookId, c.Position })
                .IsUnique()
                .HasDatabaseName("ix_chapters_book_id_position");
        });

        modelBuilder
            .Entity<BookDao>()
            .HasMany(b => b.Authors)
            .WithMany(a => a.Books)
            .UsingEntity(join =>
            {
                join.ToTable("book_authors");
                join.Property<Guid>("BooksId").HasColumnName("book_id");
                join.Property<Guid>("AuthorsId").HasColumnName("author_id");
            });
    }
}
