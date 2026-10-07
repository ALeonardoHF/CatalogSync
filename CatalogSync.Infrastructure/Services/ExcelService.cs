using ExcelDataReader;
using CatalogSync.Application.Interfaces;
using CatalogSync.Application.Models;
using OfficeOpenXml;
using System.Data;
using System.Text;

namespace CatalogSync.Infrastructure.Services;

public class ExcelService : IExcelService
{
    static ExcelService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<List<LibroExistencia>> LeerExistenciasAsync(Stream stream, string? hoja = null)
    {
        var lista = new List<LibroExistencia>();
        var table = LeerTabla(stream, hoja);

        for (int i = 1; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var isbnRaw = GetString(row[0]);
            if (string.IsNullOrWhiteSpace(isbnRaw)) continue;

            var (grupo, sello) = SepararEditorial(GetString(row[4]));

            lista.Add(new LibroExistencia
            {
                ISBNOriginal = isbnRaw,
                ISBN = NormalizarISBN(isbnRaw),
                Titulo = GetString(row[1]),
                Autor = GetString(row[2]),
                Descuento = GetString(row[3]),
                Editorial = grupo,
                Sello = sello,
                Costo = GetString(row[5]),
                Inc = GetString(row[6]),
                Precio = GetDecimal(row[7]),
                FechaEntrada = GetString(row[8]),
                CodigoBarra = table.Columns.Count > 9 ? GetString(row[9]) : "",
                Existencia = table.Columns.Count > 10 ? GetString(row[10]) : "",
                Ventas = table.Columns.Count > 11 ? GetString(row[11]) : "",
                FilaOriginal = i + 1
            });
        }

        return await Task.FromResult(lista);
    }

    public async Task<List<LibroProveedor>> LeerProveedorAsync(Stream stream, string nombreProveedor, string? hoja = null)
    {
        var lista = new List<LibroProveedor>();
        var table = LeerTabla(stream, hoja);

        if (table.Rows.Count < 1) return lista;

        var header = table.Rows[0];
        int isbnCol = FindColumn(header, "ISBN");
        int nombreCol = FindColumn(header, "NOMBRE", "TITULO");
        int autorCol = FindColumn(header, "AUTOR");
        int editCol = FindColumn(header, "EDITORIAL", "EDIT");
        int precioCol = FindColumn(header, "PRECIO");
        int costoCol = FindColumn(header, "COSTO");
        int codigoCol = FindColumn(header, "CODIGOBARRA", "CODIGO BARRA", "CODIGO_BARRA", "BARRAS");
        int existCol = FindColumn(header, "EXISTENCIA", "EXIST");
        int ventasCol = FindColumn(header, "VENTAS", "VENTA");

        if (isbnCol < 0 || (precioCol < 0 && costoCol < 0)) return lista;

        int startRow = 1;
        while (startRow < table.Rows.Count &&
               string.IsNullOrWhiteSpace(GetString(table.Rows[startRow][isbnCol])))
            startRow++;

        for (int i = startRow; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var isbnRaw = GetString(row[isbnCol]);
            if (string.IsNullOrWhiteSpace(isbnRaw)) continue;

            var precio = precioCol >= 0 ? GetDecimal(row[precioCol]) : 0;
            var costo = costoCol >= 0 ? GetDecimal(row[costoCol]) : 0;
            var (grupo, sello) = SepararEditorial(editCol >= 0 ? GetString(row[editCol]) : "");

            lista.Add(new LibroProveedor
            {
                ISBN = NormalizarISBN(isbnRaw),
                Nombre = nombreCol >= 0 ? GetString(row[nombreCol]) : "",
                Autor = autorCol >= 0 ? GetString(row[autorCol]) : "",
                Editorial = grupo,
                Sello = sello,
                PrecioUnitario = Math.Max(precio, costo),
                Costo = Math.Min(precio, costo),
                CodigoBarra = codigoCol >= 0 ? GetString(row[codigoCol]) : "",
                Existencia = existCol >= 0 ? GetInt(row[existCol]) : 0,
                Ventas = ventasCol >= 0 ? GetInt(row[ventasCol]) : 0,
                Proveedor = nombreProveedor
            });
        }

        return await Task.FromResult(lista);
    }

    public async Task<byte[]> GenerarExcelActualizadoAsync(List<LibroExistencia> catalogo, Stream streamOriginal, string? hoja = null)
    {
        var tableOriginal = LeerTabla(streamOriginal, hoja);

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Existencias");

        if (tableOriginal.Rows.Count > 0)
        {
            var headerRow = tableOriginal.Rows[0];
            for (int col = 0; col < tableOriginal.Columns.Count; col++)
                ws.Cells[1, col + 1].Value = headerRow[col]?.ToString() ?? "";
        }

        var porFila = catalogo.Where(l => l.FilaOriginal > 0).ToDictionary(l => l.FilaOriginal);

        for (int i = 1; i < tableOriginal.Rows.Count; i++)
        {
            var row = tableOriginal.Rows[i];
            int excelRow = i + 1;
            for (int col = 0; col < tableOriginal.Columns.Count; col++)
                ws.Cells[excelRow, col + 1].Value = row[col];

            if (porFila.TryGetValue(excelRow, out var libro))
                ws.Cells[excelRow, 8].Value = (double)libro.Precio;
        }

        int lastRow = tableOriginal.Rows.Count;
        foreach (var libro in catalogo.Where(l => l.FilaOriginal == 0))
        {
            lastRow++;
            ws.Cells[lastRow, 1].Value = libro.ISBNOriginal;
            ws.Cells[lastRow, 2].Value = libro.Titulo;
            ws.Cells[lastRow, 3].Value = libro.Autor;
            ws.Cells[lastRow, 4].Value = libro.Descuento;
            
            ws.Cells[lastRow, 5].Value = string.IsNullOrWhiteSpace(libro.Sello)
                ? libro.Editorial
                : $"{libro.Editorial} ({libro.Sello})";

            ws.Cells[lastRow, 6].Value = libro.Costo;
            ws.Cells[lastRow, 7].Value = libro.Inc;
            ws.Cells[lastRow, 8].Value = (double)libro.Precio;
            ws.Cells[lastRow, 9].Value = libro.FechaEntrada;
            ws.Cells[lastRow, 10].Value = libro.CodigoBarra;
            ws.Cells[lastRow, 11].Value = libro.Existencia;
            ws.Cells[lastRow, 12].Value = libro.Ventas;
        }

        return await package.GetAsByteArrayAsync();
    }

    // ── Format detection ──────────────────────────────────────────────────────

    private static DataTable LeerTabla(Stream stream, string? hoja = null)
    {
        byte[] buffer;
        using (var ms = new MemoryStream()) { stream.CopyTo(ms); buffer = ms.ToArray(); }

        return EsExcel(buffer)
            ? LeerTablaExcel(new MemoryStream(buffer), hoja)
            : LeerTablaCsv(new MemoryStream(buffer));
    }

    private static bool EsExcel(byte[] data)
    {
        if (data.Length < 4) return false;
        if (data[0] == 0x50 && data[1] == 0x4B) return true;
        if (data[0] == 0xD0 && data[1] == 0xCF && data[2] == 0x11 && data[3] == 0xE0) return true;
        return false;
    }

    private static DataTable LeerTablaExcel(Stream stream, string? hoja)
    {
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var ds = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        if (string.IsNullOrWhiteSpace(hoja)) return ds.Tables[0];
        if (ds.Tables.Contains(hoja)) return ds.Tables[hoja]!;
        if (int.TryParse(hoja, out var idx) && idx >= 0 && idx < ds.Tables.Count)
            return ds.Tables[idx];

        return ds.Tables[0];
    }

    private static DataTable LeerTablaCsv(Stream stream)
    {
        string text;
        using (var sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            text = sr.ReadToEnd();

        var lines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

        if (lines.Count == 0) return new DataTable();

        char delimiter = DetectarDelimitador(lines[0]);
        var rows = lines.Select(l => ParseCsvLine(l, delimiter)).ToList();

        int colCount = rows.Max(r => r.Length);
        var table = new DataTable();
        for (int i = 0; i < colCount; i++) table.Columns.Add(i.ToString());

        foreach (var row in rows)
        {
            var dr = table.NewRow();
            for (int i = 0; i < row.Length; i++) dr[i] = row[i];
            table.Rows.Add(dr);
        }

        return table;
    }

    private static char DetectarDelimitador(string firstLine)
    {
        int commas = firstLine.Count(c => c == ',');
        int semicolons = firstLine.Count(c => c == ';');
        int tabs = firstLine.Count(c => c == '\t');

        if (tabs > commas && tabs > semicolons) return '\t';
        if (semicolons > commas) return ';';
        return ',';
    }

    private static string[] ParseCsvLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == delimiter) { fields.Add(sb.ToString().Trim()); sb.Clear(); }
                else sb.Append(c);
            }
        }

        fields.Add(sb.ToString().Trim());
        return [.. fields];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int FindColumn(DataRow headerRow, params string[] keywords)
    {
        for (int i = 0; i < headerRow.Table.Columns.Count; i++)
        {
            var val = headerRow[i]?.ToString()?.ToUpperInvariant() ?? "";
            if (keywords.Any(k => val.Contains(k.ToUpperInvariant()))) return i;
        }
        return -1;
    }

    private static string GetString(object? cell)
    {
        if (cell == null || cell is DBNull) return "";
        if (cell is double d) return ((long)d).ToString();
        return cell.ToString()?.Trim() ?? "";
    }

    private static decimal GetDecimal(object? cell)
    {
        if (cell == null || cell is DBNull) return 0;
        if (cell is double d) return (decimal)d;
        if (cell is long l) return l;
        if (cell is int i) return i;

        var raw = cell.ToString() ?? "";
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw)
            if (char.IsDigit(c) || c == ',' || c == '.' || c == '-') sb.Append(c);

        var s = sb.ToString().Trim();
        if (s.Length == 0) return 0;

        int lastComma = s.LastIndexOf(',');
        int lastPeriod = s.LastIndexOf('.');

        if (lastComma >= 0 && lastPeriod >= 0)
        {
            s = lastPeriod > lastComma
                ? s.Replace(",", "")
                : s.Replace(".", "").Replace(",", ".");
        }
        else if (lastComma >= 0)
        {
            var afterComma = s[(lastComma + 1)..];
            s = afterComma.Length == 3 && afterComma.All(char.IsDigit)
                ? s.Replace(",", "")
                : s.Replace(",", ".");
        }

        return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static int GetInt(object? cell)
    {
        if (cell == null || cell is DBNull) return 0;
        if (cell is double d) return (int)d;
        return int.TryParse(cell.ToString()?.Trim(), out var v) ? v : 0;
    }

    private static (string Grupo, string Sello) SepararEditorial(string raw)
    {
        var texto = raw.Trim();
        int abre = texto.IndexOf('(');
        int cierra = texto.IndexOf(')');

        if (abre < 0 || cierra < abre) return (texto, "");

        return (texto[..abre].Trim(), texto[(abre + 1)..cierra].Trim());
    }

    private static string NormalizarISBN(string isbn) => isbn.Trim().Trim('*').Trim();
}
