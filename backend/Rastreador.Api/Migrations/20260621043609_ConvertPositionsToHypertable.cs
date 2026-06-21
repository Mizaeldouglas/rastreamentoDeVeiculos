using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rastreador.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPositionsToHypertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TimescaleDB exige que qualquer constraint única inclua a coluna de particionamento (Timestamp).
            // A PK simples em "Id" precisa ser removida antes de converter a tabela em hypertable.
            migrationBuilder.Sql("ALTER TABLE \"Positions\" DROP CONSTRAINT \"PK_Positions\";");
            migrationBuilder.Sql(
                "SELECT create_hypertable('\"Positions\"', 'Timestamp', if_not_exists => TRUE, migrate_data => TRUE);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Positions\" ADD CONSTRAINT \"PK_Positions\" PRIMARY KEY (\"Id\");");
        }
    }
}
