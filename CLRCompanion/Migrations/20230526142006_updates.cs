using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLRCompanion.Migrations
{
    /// <inheritdoc />
    public partial class updates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ModelType",
                table: "Bots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PromptSuffix",
                table: "Bots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StopToken",
                table: "Bots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("UPDATE Bots Set ModelType = 1");

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserMessagePreference = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ModelType",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "PromptSuffix",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "StopToken",
                table: "Bots");
        }
    }
}
