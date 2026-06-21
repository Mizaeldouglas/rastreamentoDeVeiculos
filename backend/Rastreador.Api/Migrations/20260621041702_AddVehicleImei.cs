using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rastreador.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleImei : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Imei",
                table: "Vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Imei",
                table: "Vehicles",
                column: "Imei",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_Imei",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Imei",
                table: "Vehicles");
        }
    }
}
