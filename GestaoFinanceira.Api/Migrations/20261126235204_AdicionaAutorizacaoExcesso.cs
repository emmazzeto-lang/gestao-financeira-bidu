using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceira.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAutorizacaoExcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutorizadoEm",
                table: "Despesas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AutorizadoPorId",
                table: "Despesas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificativaExcesso",
                table: "Despesas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_AutorizadoPorId",
                table: "Despesas",
                column: "AutorizadoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Despesas_Usuarios_AutorizadoPorId",
                table: "Despesas",
                column: "AutorizadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Despesas_Usuarios_AutorizadoPorId",
                table: "Despesas");

            migrationBuilder.DropIndex(
                name: "IX_Despesas_AutorizadoPorId",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "AutorizadoEm",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "AutorizadoPorId",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "JustificativaExcesso",
                table: "Despesas");
        }
    }
}
