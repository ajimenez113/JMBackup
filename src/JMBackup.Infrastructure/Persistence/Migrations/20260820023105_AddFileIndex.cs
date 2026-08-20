using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMBackup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFileIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileIndex",
                columns: table => new
                {
                    TaskName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LastBackedUpAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileIndex", x => new { x.TaskName, x.RelativePath });
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileIndex_TaskName",
                table: "FileIndex",
                column: "TaskName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileIndex");
        }
    }
}
