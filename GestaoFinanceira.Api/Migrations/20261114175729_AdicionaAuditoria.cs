using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceira.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Receitas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CriadoPorId",
                table: "Receitas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Despesas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CriadoPorId",
                table: "Despesas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_CriadoPorId",
                table: "Receitas",
                column: "CriadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_CriadoPorId",
                table: "Despesas",
                column: "CriadoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Despesas_Usuarios_CriadoPorId",
                table: "Despesas",
                column: "CriadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receitas_Usuarios_CriadoPorId",
                table: "Receitas",
                column: "CriadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Despesas_Usuarios_CriadoPorId",
                table: "Despesas");

            migrationBuilder.DropForeignKey(
                name: "FK_Receitas_Usuarios_CriadoPorId",
                table: "Receitas");

            migrationBuilder.DropIndex(
                name: "IX_Receitas_CriadoPorId",
                table: "Receitas");

            migrationBuilder.DropIndex(
                name: "IX_Despesas_CriadoPorId",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "CriadoPorId",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "CriadoPorId",
                table: "Despesas");
        }
    }
}
