using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rastreador.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleIgnitionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IgnitionOn",
                table: "Vehicles",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnitionOn",
                table: "Vehicles");
        }
    }
}
