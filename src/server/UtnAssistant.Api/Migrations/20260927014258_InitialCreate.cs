using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;
using UtnAssistant.API.Enums;

#nullable disable

namespace UtnAssistant.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:CorrelativeType", "approved,attended")
                .Annotation("Npgsql:Enum:CorrelativeType.correlative_type", "approved,attended")
                .Annotation("Npgsql:Enum:ProgressStatus", "approved,attended,enrolled,not_enrolled")
                .Annotation("Npgsql:Enum:ProgressStatus.progress_status", "not_enrolled,enrolled,attended,approved")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "Career",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Career", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Subject",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    semester = table.Column<int>(type: "integer", nullable: false),
                    credits = table.Column<int>(type: "integer", nullable: true),
                    isIntegrator = table.Column<bool>(type: "boolean", nullable: false),
                    isElective = table.Column<bool>(type: "boolean", nullable: false),
                    careerId = table.Column<string>(type: "text", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    id = table.Column<string>(type: "text", nullable: false),
                    subjectId = table.Column<string>(type: "text", nullable: false),
                    requiredSubjectId = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<CorrelativeType>(type: "\"CorrelativeType\"", nullable: false),
                    isTransient = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Correlative", x => x.id);
                    table.ForeignKey(
                        name: "FK_Correlative_Subject_requiredSubjectId",
                        column: x => x.requiredSubjectId,
                        principalTable: "Subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Correlative_Subject_subjectId",
                        column: x => x.subjectId,
                        principalTable: "Subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyllabusChunk",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    subjectId = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(1536)", nullable: true)
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
                    id = table.Column<string>(type: "text", nullable: false),
                    userId = table.Column<string>(type: "text", nullable: false),
                    subjectId = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<ProgressStatus>(type: "\"ProgressStatus\"", nullable: false),
                    grade = table.Column<double>(type: "double precision", nullable: true),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
