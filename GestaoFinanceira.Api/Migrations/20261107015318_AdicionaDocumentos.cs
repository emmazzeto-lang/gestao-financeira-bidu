using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestaoFinanceira.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Numero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NomeArquivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TipoConteudo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tamanho = table.Column<long>(type: "bigint", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DespesaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Despesas_DespesaId",
                        column: x => x.DespesaId,
                        principalTable: "Despesas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_DespesaId",
                table: "Documentos",
                column: "DespesaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documentos");
        }
    }
}
