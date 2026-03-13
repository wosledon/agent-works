using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace DotnetBlog.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add search vector column for PostgreSQL full-text search
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Posts",
                type: "tsvector",
                nullable: true);

            // Create GIN index for fast full-text search
            migrationBuilder.CreateIndex(
                name: "IX_Posts_SearchVector",
                table: "Posts",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            // Create function to automatically update search vector
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_posts_search_vector()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.""SearchVector"" := 
                        setweight(to_tsvector('simple', COALESCE(NEW.""Title"", '')), 'A') ||
                        setweight(to_tsvector('simple', COALESCE(NEW.""Content"", '')), 'B') ||
                        setweight(to_tsvector('simple', COALESCE(NEW.""Excerpt"", '')), 'C');
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;");

            // Create trigger to update search vector on insert/update
            migrationBuilder.Sql(@"
                CREATE TRIGGER update_posts_search_vector_trigger
                BEFORE INSERT OR UPDATE ON ""Posts""
                FOR EACH ROW
                EXECUTE FUNCTION update_posts_search_vector();");

            // Update existing rows to populate search vector
            migrationBuilder.Sql(@"
                UPDATE ""Posts"" 
                SET ""SearchVector"" = 
                    setweight(to_tsvector('simple', COALESCE(""Title"", '')), 'A') ||
                    setweight(to_tsvector('simple', COALESCE(""Content"", '')), 'B') ||
                    setweight(to_tsvector('simple', COALESCE(""Excerpt"", '')), 'C');");

            // Add indexes for common queries (performance optimization)
            migrationBuilder.CreateIndex(
                name: "IX_Posts_Status_PublishedAt",
                table: "Posts",
                columns: new[] { "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Status_PostId",
                table: "Comments",
                columns: new[] { "Status", "PostId" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PostId_ParentId",
                table: "Comments",
                columns: new[] { "PostId", "ParentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigger
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS update_posts_search_vector_trigger ON \"Posts\";");

            // Drop function
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_posts_search_vector();");

            // Drop index and column
            migrationBuilder.DropIndex(
                name: "IX_Posts_SearchVector",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Posts");

            // Drop performance indexes
            migrationBuilder.DropIndex(
                name: "IX_Posts_Status_PublishedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Comments_Status_PostId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_PostId_ParentId",
                table: "Comments");
        }
    }
}
