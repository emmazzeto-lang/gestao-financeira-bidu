using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceira.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFonteNaReceita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FonteRecursoId",
                table: "Receitas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_FonteRecursoId",
                table: "Receitas",
                column: "FonteRecursoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Receitas_FontesRecurso_FonteRecursoId",
                table: "Receitas",
                column: "FonteRecursoId",
                principalTable: "FontesRecurso",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receitas_FontesRecurso_FonteRecursoId",
                table: "Receitas");

            migrationBuilder.DropIndex(
                name: "IX_Receitas_FonteRecursoId",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "FonteRecursoId",
                table: "Receitas");
        }
    }
}
