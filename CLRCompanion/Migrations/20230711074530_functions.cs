using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLRCompanion.Migrations
{
    /// <inheritdoc />
    public partial class functions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimerId",
                table: "Bots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseTemplateId",
                table: "Bots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OpenAIFunctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Template = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenAIFunctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenAIFunctionParam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Enum = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Required = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpenAIFunctionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenAIFunctionParam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenAIFunctionParam_OpenAIFunctions_OpenAIFunctionId",
                        column: x => x.OpenAIFunctionId,
                        principalTable: "OpenAIFunctions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bots_PrimerId",
                table: "Bots",
                column: "PrimerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bots_ResponseTemplateId",
                table: "Bots",
                column: "ResponseTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenAIFunctionParam_OpenAIFunctionId",
                table: "OpenAIFunctionParam",
                column: "OpenAIFunctionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bots_OpenAIFunctions_PrimerId",
                table: "Bots",
                column: "PrimerId",
                principalTable: "OpenAIFunctions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bots_OpenAIFunctions_ResponseTemplateId",
                table: "Bots",
                column: "ResponseTemplateId",
                principalTable: "OpenAIFunctions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bots_OpenAIFunctions_PrimerId",
                table: "Bots");

            migrationBuilder.DropForeignKey(
                name: "FK_Bots_OpenAIFunctions_ResponseTemplateId",
                table: "Bots");

            migrationBuilder.DropTable(
                name: "OpenAIFunctionParam");

            migrationBuilder.DropTable(
                name: "OpenAIFunctions");

            migrationBuilder.DropIndex(
                name: "IX_Bots_PrimerId",
                table: "Bots");

            migrationBuilder.DropIndex(
                name: "IX_Bots_ResponseTemplateId",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "PrimerId",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "ResponseTemplateId",
                table: "Bots");
        }
    }
}
