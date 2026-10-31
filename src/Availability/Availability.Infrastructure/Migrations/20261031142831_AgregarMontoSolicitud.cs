using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Availability.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMontoSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Monto",
                table: "Solicitudes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Monto",
                table: "Solicitudes");
        }
    }
}
