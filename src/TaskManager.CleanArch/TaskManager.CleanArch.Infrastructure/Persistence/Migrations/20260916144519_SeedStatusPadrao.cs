using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskManager.CleanArch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedStatusPadrao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "StatusTarefa",
                columns: new[] { "Id", "Categoria", "Codigo", "Nome", "Protegido", "Versao" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Pendente", "PENDENTE", "Pendente", true, 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "EmAndamento", "EM_ANDAMENTO", "Em andamento", true, 1 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Concluida", "CONCLUIDA", "Concluída", true, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StatusTarefa",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "StatusTarefa",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "StatusTarefa",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
        }
    }
}
