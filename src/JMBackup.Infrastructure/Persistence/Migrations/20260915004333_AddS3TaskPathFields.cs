using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMBackup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddS3TaskPathFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "TaskPaths",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ServerSideEncryption",
                table: "TaskPaths",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StorageClass",
                table: "TaskPaths",
                type: "TEXT",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Region",
                table: "TaskPaths");

            migrationBuilder.DropColumn(
                name: "ServerSideEncryption",
                table: "TaskPaths");

            migrationBuilder.DropColumn(
                name: "StorageClass",
                table: "TaskPaths");
        }
    }
}
