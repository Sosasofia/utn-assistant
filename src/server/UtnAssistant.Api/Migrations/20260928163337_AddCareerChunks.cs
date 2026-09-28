using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtnAssistant.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerChunk",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    careerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    sectionTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    embedding = table.Column<SqlVector<float>>(type: "vector(1536)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerChunk", x => x.id);
                    table.ForeignKey(
                        name: "FK_CareerChunk_Career_careerId",
                        column: x => x.careerId,
                        principalTable: "Career",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerChunk_careerId",
                table: "CareerChunk",
                column: "careerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerChunk");
        }
    }
}
