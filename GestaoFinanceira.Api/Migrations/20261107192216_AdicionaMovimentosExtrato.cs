using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestaoFinanceira.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaMovimentosExtrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovimentosExtrato",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdBanco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContaBancariaId = table.Column<int>(type: "integer", nullable: false),
                    DespesaId = table.Column<int>(type: "integer", nullable: true),
                    ReceitaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosExtrato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosExtrato_ContasBancarias_ContaBancariaId",
                        column: x => x.ContaBancariaId,
                        principalTable: "ContasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimentosExtrato_Despesas_DespesaId",
                        column: x => x.DespesaId,
                        principalTable: "Despesas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosExtrato_Receitas_ReceitaId",
                        column: x => x.ReceitaId,
                        principalTable: "Receitas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosExtrato_ContaBancariaId_IdBanco",
                table: "MovimentosExtrato",
                columns: new[] { "ContaBancariaId", "IdBanco" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosExtrato_DespesaId",
                table: "MovimentosExtrato",
                column: "DespesaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosExtrato_ReceitaId",
                table: "MovimentosExtrato",
                column: "ReceitaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentosExtrato");
        }
    }
}
