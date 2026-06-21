using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rastreador.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionsCompressionAndRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Habilita compressão nativa do TimescaleDB: chunks antigos são comprimidos por veículo,
            // ordenados por timestamp — reduz drasticamente o espaço em disco de dados históricos.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Positions"" SET (
                    timescaledb.compress,
                    timescaledb.compress_segmentby = '""VehicleId""',
                    timescaledb.compress_orderby = '""Timestamp"" DESC'
                );
            ");

            // Comprime automaticamente chunks com mais de 7 dias.
            migrationBuilder.Sql(@"
                SELECT add_compression_policy('""Positions""', INTERVAL '7 days');
            ");

            // Descarta posições brutas com mais de 180 dias (6 meses) — ajuste conforme a necessidade
            // de retenção do negócio. Relatórios de longo prazo devem usar agregados, não dados brutos.
            migrationBuilder.Sql(@"
                SELECT add_retention_policy('""Positions""', INTERVAL '180 days');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SELECT remove_retention_policy('""Positions""');");
            migrationBuilder.Sql(@"SELECT remove_compression_policy('""Positions""');");
            migrationBuilder.Sql(@"ALTER TABLE ""Positions"" SET (timescaledb.compress = false);");
        }
    }
}
