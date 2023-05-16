using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLRCompanion.Migrations
{
    /// <inheritdoc />
    public partial class ignorepings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IgnorePings",
                table: "Bots",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnorePings",
                table: "Bots");
        }
    }
}
