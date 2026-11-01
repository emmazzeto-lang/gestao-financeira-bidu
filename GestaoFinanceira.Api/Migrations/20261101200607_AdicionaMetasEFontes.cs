using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestaoFinanceira.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaMetasEFontes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MetaId",
                table: "Despesas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FontesRecurso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FontesRecurso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Metas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ValorPrevisto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlanoTrabalhoId = table.Column<int>(type: "integer", nullable: false),
                    FonteRecursoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Metas_FontesRecurso_FonteRecursoId",
                        column: x => x.FonteRecursoId,
                        principalTable: "FontesRecurso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metas_PlanosTrabalho_PlanoTrabalhoId",
                        column: x => x.PlanoTrabalhoId,
                        principalTable: "PlanosTrabalho",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_MetaId",
                table: "Despesas",
                column: "MetaId");

            migrationBuilder.CreateIndex(
                name: "IX_FontesRecurso_Nome",
                table: "FontesRecurso",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Metas_FonteRecursoId",
                table: "Metas",
                column: "FonteRecursoId");

            migrationBuilder.CreateIndex(
                name: "IX_Metas_PlanoTrabalhoId",
                table: "Metas",
                column: "PlanoTrabalhoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Despesas_Metas_MetaId",
                table: "Despesas",
                column: "MetaId",
                principalTable: "Metas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Despesas_Metas_MetaId",
                table: "Despesas");

            migrationBuilder.DropTable(
                name: "Metas");

            migrationBuilder.DropTable(
                name: "FontesRecurso");

            migrationBuilder.DropIndex(
                name: "IX_Despesas_MetaId",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "MetaId",
                table: "Despesas");
        }
    }
}
