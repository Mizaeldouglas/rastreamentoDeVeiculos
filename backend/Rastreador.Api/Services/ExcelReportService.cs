using ClosedXML.Excel;
using Rastreador.Api.Models;

namespace Rastreador.Api.Services;

public class ExcelReportService
{
    public byte[] GenerateHistoryReport(string vehiclePlate, IReadOnlyList<PositionDto> positions)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Histórico");

        sheet.Cell(1, 1).Value = "Data/Hora";
        sheet.Cell(1, 2).Value = "Latitude";
        sheet.Cell(1, 3).Value = "Longitude";
        sheet.Cell(1, 4).Value = "Velocidade (km/h)";
        sheet.Cell(1, 5).Value = "Direção (°)";
        sheet.Range(1, 1, 1, 5).Style.Font.Bold = true;

        int row = 2;
        foreach (var position in positions)
        {
            sheet.Cell(row, 1).Value = position.Timestamp.ToLocalTime();
            sheet.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
            sheet.Cell(row, 2).Value = position.Latitude;
            sheet.Cell(row, 3).Value = position.Longitude;
            sheet.Cell(row, 4).Value = position.Speed;
            sheet.Cell(row, 5).Value = position.Heading;
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.Name = $"Histórico {vehiclePlate}".Length > 31 ? "Histórico" : $"Histórico {vehiclePlate}";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
