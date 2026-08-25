using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMBackup.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskPathEncrypted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Encrypted",
                table: "TaskPaths",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Encrypted",
                table: "TaskPaths");
        }
    }
}
