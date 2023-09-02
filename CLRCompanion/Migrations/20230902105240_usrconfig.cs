using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLRCompanion.Migrations
{
    /// <inheritdoc />
    public partial class usrconfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DontPing",
                table: "UserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DontPing",
                table: "UserPreferences");
        }
    }
}
