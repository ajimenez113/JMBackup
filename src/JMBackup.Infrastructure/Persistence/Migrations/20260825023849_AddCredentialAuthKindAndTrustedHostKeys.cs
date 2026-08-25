using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMBackup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialAuthKindAndTrustedHostKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthKind",
                table: "Credentials",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedPassphrase",
                table: "Credentials",
                type: "BLOB",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrustedHostKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Host = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Algorithm = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedHostKeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrustedHostKeys_Host_Port",
                table: "TrustedHostKeys",
                columns: new[] { "Host", "Port" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrustedHostKeys");

            migrationBuilder.DropColumn(
                name: "AuthKind",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "EncryptedPassphrase",
                table: "Credentials");
        }
    }
}
