using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGHR.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEncomendaWithEstadoString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoAtual",
                table: "Encomendas");

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Encomendas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Encomendas");

            migrationBuilder.AddColumn<bool>(
                name: "EstadoAtual",
                table: "Encomendas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
