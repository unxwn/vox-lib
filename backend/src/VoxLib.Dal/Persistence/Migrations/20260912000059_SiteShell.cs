using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoxLib.Dal.Persistence.Migrations
{
    /// <summary>
    /// Authors gain an address and a sort order, books gain a narrator, an
    /// arrival date and a cover key in place of a cover URL, and the join
    /// between a book and a person gains the role it is credited under.
    /// <para>
    /// Hand written past the scaffold, because the scaffold is correct only
    /// against an empty database: it drops <c>cover_art_url</c> rather than
    /// renaming it, defaults the arrival date to year one, and adds two required
    /// columns with an empty string, which a unique index on the slug then
    /// refuses. What is here instead is written to be correct against a database
    /// that already has rows. Replacing the catalogue's contents is the seeder's
    /// job and never this file's.
    /// </para>
    /// </summary>
    public partial class SiteShell : Migration
    {
        /// <summary>
        /// The transliteration <c>AuthorSlug.From</c> applies, in SQL, so that
        /// rows written before this migration get the address they would have
        /// been given had the column always existed.
        /// <para>
        /// It is a copy of a rule that lives in the domain, which is worth it
        /// exactly once: the alternative is a required unique column backfilled
        /// with something arbitrary, and an arbitrary address is permanent.
        /// The order matters — silent letters go first so an apostrophe cannot
        /// make the letter after it look word-initial, the five positional
        /// letters are resolved while the text is still Cyrillic, and зг is
        /// spent before з becomes z.
        /// </para>
        /// </summary>
        private const string SlugExpression = """
            trim(both '-' from regexp_replace(
              translate(
                replace(replace(replace(replace(replace(replace(replace(
                  replace(replace(replace(replace(replace(
                    regexp_replace(regexp_replace(regexp_replace(regexp_replace(regexp_replace(
                      translate(lower(name), 'ь''’ʼ', ''),
                    '\mє', 'ye', 'g'), '\mї', 'yi', 'g'), '\mй', 'y', 'g'),
                    '\mю', 'yu', 'g'), '\mя', 'ya', 'g'),
                  'є','ie'), 'ї','i'), 'й','i'), 'ю','iu'), 'я','ia'),
                'зг','zgh'), 'щ','shch'), 'ж','zh'), 'х','kh'), 'ц','ts'), 'ч','ch'), 'ш','sh'),
                'абвгґдеизіклмнопрстуф', 'abvhgdeyziklmnoprstuf'),
              '[^a-z0-9]+', '-', 'g'))
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. The author's address and sort order. Added nullable, backfilled,
            //    then made required, which is the only way to add a required
            //    unique column to a table that already has rows.
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "authors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sort_name",
                table: "authors",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                collation: "uk-UA-x-icu");

            // The sort name cannot be derived — no rule separates a surname from
            // a given name reliably — so an existing row takes its own name and
            // is corrected by hand if it is ever shown. New rows carry the
            // authored value from authors.json.
            migrationBuilder.Sql("UPDATE authors SET sort_name = name WHERE sort_name IS NULL;");

            // A suffix here is not the numbering FR-038 forbids. That rule is
            // about creating an author: a collision between two new rows is a
            // defect and must stop the seed. This is a one-time backfill of rows
            // that already exist, where refusing would leave the database
            // unmigratable and the defect unfixable. The order is by id, so the
            // same database always produces the same addresses.
            migrationBuilder.Sql(
                $"""
                WITH transliterated AS (
                    SELECT id, {SlugExpression} AS slug FROM authors
                ),
                settled AS (
                    SELECT
                        id,
                        slug,
                        row_number() OVER (PARTITION BY slug ORDER BY id) AS ordinal
                    FROM transliterated
                )
                UPDATE authors
                SET slug = CASE
                    WHEN settled.ordinal = 1 THEN settled.slug
                    ELSE settled.slug || '-' || settled.ordinal
                END
                FROM settled
                WHERE settled.id = authors.id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "authors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sort_name",
                table: "authors",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                collation: "uk-UA-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true,
                oldCollation: "uk-UA-x-icu");

            migrationBuilder.CreateIndex(
                name: "ix_authors_slug",
                table: "authors",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_authors_sort_name_slug",
                table: "authors",
                columns: new[] { "sort_name", "slug" });

            // 2. The narrator: a name, optional, and nothing to do with a file.
            migrationBuilder.AddColumn<string>(
                name: "narrator",
                table: "books",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                collation: "uk-UA-x-icu");

            // 3. The arrival date, required. The default backfills every existing
            //    row to the moment this runs, which is the most honest answer
            //    available for a book that arrived before anyone recorded when.
            //    The default is then dropped, because the model does not declare
            //    one and a database default the model does not know about drifts.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "added_to_catalogue_at",
                table: "books",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.Sql(
                "ALTER TABLE books ALTER COLUMN added_to_catalogue_at DROP DEFAULT;");

            // 4. The cover stops being a URL and becomes the key stem the
            //    prepared objects share. A rename rather than a drop and an add:
            //    it costs nothing and keeps the migration honest about what
            //    happened to the column.
            migrationBuilder.RenameColumn(
                name: "cover_art_url",
                table: "books",
                newName: "cover_key");

            migrationBuilder.Sql(
                """
                UPDATE books
                SET cover_key = regexp_replace(cover_key, '^/covers/(.+)\.svg$', '\1')
                WHERE cover_key ~ '^/covers/.+\.svg$';
                """);

            // Narrowed only after the rewrite, when nothing is long enough to
            // refuse it.
            migrationBuilder.AlterColumn<string>(
                name: "cover_key",
                table: "books",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            // 5. The credit role, on the link between a book and a person. The
            //    default is Author, so every pairing that already exists keeps
            //    the meaning it already had.
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "book_authors",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Author");

            // The join stops being a shadow entity now that it carries the role,
            // which reverses the key's column order and moves the covering index
            // onto the other side.
            migrationBuilder.DropPrimaryKey(name: "PK_book_authors", table: "book_authors");

            migrationBuilder.DropIndex(name: "IX_book_authors_book_id", table: "book_authors");

            migrationBuilder.AddPrimaryKey(
                name: "PK_book_authors",
                table: "book_authors",
                columns: new[] { "book_id", "author_id" });

            migrationBuilder.CreateIndex(
                name: "IX_book_authors_author_id",
                table: "book_authors",
                column: "author_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_book_authors", table: "book_authors");

            migrationBuilder.DropIndex(name: "IX_book_authors_author_id", table: "book_authors");

            migrationBuilder.AddPrimaryKey(
                name: "PK_book_authors",
                table: "book_authors",
                columns: new[] { "author_id", "book_id" });

            migrationBuilder.CreateIndex(
                name: "IX_book_authors_book_id",
                table: "book_authors",
                column: "book_id");

            migrationBuilder.DropColumn(name: "role", table: "book_authors");

            migrationBuilder.AlterColumn<string>(
                name: "cover_key",
                table: "books",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // Back to a URL, for the two rows that ever carried one.
            migrationBuilder.Sql(
                """
                UPDATE books
                SET cover_key = '/covers/' || cover_key || '.svg'
                WHERE cover_key IS NOT NULL AND cover_key !~ '^/';
                """);

            migrationBuilder.RenameColumn(
                name: "cover_key",
                table: "books",
                newName: "cover_art_url");

            migrationBuilder.DropColumn(name: "added_to_catalogue_at", table: "books");

            migrationBuilder.DropColumn(name: "narrator", table: "books");

            migrationBuilder.DropIndex(name: "ix_authors_slug", table: "authors");

            migrationBuilder.DropIndex(name: "ix_authors_sort_name_slug", table: "authors");

            migrationBuilder.DropColumn(name: "slug", table: "authors");

            migrationBuilder.DropColumn(name: "sort_name", table: "authors");
        }
    }
}
