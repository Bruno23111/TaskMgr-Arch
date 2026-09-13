using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.CleanArch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "AspNetUsers");
        }
    }
}
