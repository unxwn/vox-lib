using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VoxLib.Dal.Account;
using VoxLib.Dal.Book;

namespace VoxLib.Dal.Persistence;

/// <summary>
/// The schema. The collation and the indexes configured here are not tuning:
/// they are where several requirements are actually enforced, so the migration
/// they generate is worth reading.
/// <para>
/// It derives from the user variant of the identity context rather than the full
/// one, so the three role tables a claims-based access model never reads are not
/// created. Permission here is decided from what is recorded on the account, not
/// from membership of a named group.
/// </para>
/// <para>
/// It also holds the data protection key ring, which encrypts the session cookie
/// and every confirmation and recovery link. The default key store is on the
/// container filesystem, which is erased on rebuild and is not shared between
/// instances, so losing it would sign everyone out and void every unfollowed
/// link. Here it lives beside the accounts it protects.
/// </para>
/// </summary>
public sealed class VoxLibDbContext(DbContextOptions<VoxLibDbContext> options)
    : IdentityUserContext<AccountDao, Guid>(options), IDataProtectionKeyContext
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

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<SessionDao> Sessions => Set<SessionDao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // First, so the identity configuration is in place before the renaming
        // below moves its tables and columns to this schema's conventions.
        base.OnModelCreating(modelBuilder);

        ConfigureAccounts(modelBuilder);

        modelBuilder.Entity<SessionDao>(session =>
        {
            session.ToTable("sessions");
            session.HasKey(s => s.Id);
            session.Property(s => s.Id).HasColumnName("id").HasMaxLength(64);
            session.Property(s => s.Payload).HasColumnName("payload").IsRequired();
            session.Property(s => s.ExpiresAt).HasColumnName("expires_at");

            // Expired rows are swept opportunistically when a session is
            // created, and this is what keeps that sweep cheap.
            session.HasIndex(s => s.ExpiresAt).HasDatabaseName("ix_sessions_expires_at");
        });

        modelBuilder.Entity<DataProtectionKey>(key =>
        {
            key.ToTable("data_protection_keys");
            key.Property(k => k.Id).HasColumnName("id");
            key.Property(k => k.FriendlyName).HasColumnName("friendly_name");
            key.Property(k => k.Xml).HasColumnName("xml");
        });

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

    /// <summary>
    /// Moves the identity tables and columns onto this schema's conventions, and
    /// adds the one index that carries a requirement.
    /// </summary>
    private static void ConfigureAccounts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountDao>(account =>
        {
            account.ToTable("accounts");

            account.Property(a => a.IsVerifiedBeneficiary).HasDefaultValue(false);

            // FR-006: one account per address, however many times the same
            // registration arrives, including two that race. The unique index is
            // what settles a race; a read-then-write check in the application
            // cannot.
            //
            // Deliberately not given the Ukrainian collation the catalogue's
            // title and author columns carry. An address is compared for exact
            // equality and never sorted for a reader, and a linguistic collation
            // on an identity column is how two addresses that differ become one
            // account.
            account
                .HasIndex(a => a.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("ix_accounts_normalized_email");
        });

        // Identity's own table names are AspNetUsers and its columns are
        // PascalCase. The catalogue tables next to them are snake_case, and one
        // database with two conventions is a database people misread.
        var identityTables = new Dictionary<Type, string>
        {
            [typeof(AccountDao)] = "accounts",
            [typeof(IdentityUserClaim<Guid>)] = "account_claims",
            [typeof(IdentityUserLogin<Guid>)] = "account_logins",
            [typeof(IdentityUserToken<Guid>)] = "account_tokens",
        };

        foreach (var (clrType, tableName) in identityTables)
        {
            var entity = modelBuilder.Entity(clrType).Metadata;

            entity.SetTableName(tableName);

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        var builder = new System.Text.StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                builder.Append(name[i]);
            }
        }

        return builder.ToString();
    }
}
