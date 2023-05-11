using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLRCompanion.Migrations
{
    /// <inheritdoc />
    public partial class webhookid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "WebhookId",
                table: "Bots",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebhookId",
                table: "Bots");
        }
    }
}
