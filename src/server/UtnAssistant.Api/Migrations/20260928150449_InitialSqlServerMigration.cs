using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtnAssistant.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServerMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Career",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Career", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Subject",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    semester = table.Column<int>(type: "int", nullable: false),
                    credits = table.Column<int>(type: "int", nullable: true),
                    isIntegrator = table.Column<bool>(type: "bit", nullable: false),
                    isElective = table.Column<bool>(type: "bit", nullable: false),
                    careerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subject", x => x.id);
                    table.ForeignKey(
                        name: "FK_Subject_Career_careerId",
                        column: x => x.careerId,
                        principalTable: "Career",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Correlative",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    subjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    requiredSubjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    isTransient = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Correlative", x => x.id);
                    table.ForeignKey(
                        name: "FK_Correlative_Subject_requiredSubjectId",
                        column: x => x.requiredSubjectId,
                        principalTable: "Subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Correlative_Subject_subjectId",
                        column: x => x.subjectId,
                        principalTable: "Subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyllabusChunk",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    subjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    embedding = table.Column<SqlVector<float>>(type: "vector(1536)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusChunk", x => x.id);
                    table.ForeignKey(
                        name: "FK_SyllabusChunk_Subject_subjectId",
                        column: x => x.subjectId,
                        principalTable: "Subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSubjectProgress",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    userId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    subjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    grade = table.Column<double>(type: "float", nullable: true),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubjectProgress", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserSubjectProgress_Subject_subjectId",
                        column: x => x.subjectId,
                        principalTable: "Subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSubjectProgress_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Career_code",
                table: "Career",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Correlative_requiredSubjectId",
                table: "Correlative",
                column: "requiredSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Correlative_subjectId_requiredSubjectId_type",
                table: "Correlative",
                columns: new[] { "subjectId", "requiredSubjectId", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subject_careerId_code",
                table: "Subject",
                columns: new[] { "careerId", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subject_year_semester",
                table: "Subject",
                columns: new[] { "year", "semester" });

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusChunk_subjectId",
                table: "SyllabusChunk",
                column: "subjectId");

            migrationBuilder.CreateIndex(
                name: "IX_User_email",
                table: "User",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubjectProgress_subjectId",
                table: "UserSubjectProgress",
                column: "subjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubjectProgress_userId_subjectId",
                table: "UserSubjectProgress",
                columns: new[] { "userId", "subjectId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Correlative");

            migrationBuilder.DropTable(
                name: "SyllabusChunk");

            migrationBuilder.DropTable(
                name: "UserSubjectProgress");

            migrationBuilder.DropTable(
                name: "Subject");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Career");
        }
    }
}
