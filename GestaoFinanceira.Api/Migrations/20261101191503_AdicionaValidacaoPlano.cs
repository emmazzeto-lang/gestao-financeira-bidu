using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceira.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaValidacaoPlano : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataValidacao",
                table: "PlanosTrabalho",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Validado",
                table: "PlanosTrabalho",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataValidacao",
                table: "PlanosTrabalho");

            migrationBuilder.DropColumn(
                name: "Validado",
                table: "PlanosTrabalho");
        }
    }
}
