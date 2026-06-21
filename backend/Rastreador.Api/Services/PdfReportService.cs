using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Rastreador.Api.Services;

public record AlertReportRow(string VehiclePlate, string Type, string Message, DateTime Timestamp);

public class PdfReportService
{
    public byte[] GenerateAlertsReport(string companyName, DateTime from, DateTime to, IReadOnlyList<AlertReportRow> rows)
    {
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("Relatório de Alertas").FontSize(18).Bold();
                    column.Item().Text(companyName).FontSize(12);
                    column.Item().Text($"Período: {from:dd/MM/yyyy HH:mm} — {to:dd/MM/yyyy HH:mm}").FontSize(9);
                    column.Item().PaddingTop(10);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Veículo").Bold();
                        header.Cell().Text("Tipo").Bold();
                        header.Cell().Text("Mensagem").Bold();
                        header.Cell().Text("Data/Hora").Bold();
                    });

                    foreach (var row in rows)
                    {
                        table.Cell().Text(row.VehiclePlate);
                        table.Cell().Text(row.Type);
                        table.Cell().Text(row.Message);
                        table.Cell().Text(row.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"));
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Gerado em ");
                    text.Span(DateTime.UtcNow.ToLocalTime().ToString("dd/MM/yyyy HH:mm")).Bold();
                });
            });
        });

        return document.GeneratePdf();
    }
}
