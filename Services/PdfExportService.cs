using System.IO;
using System.Text;

namespace ExpenseManager.Services;

public static class PdfExportService
{
    public static void Export(string filePath, string title, IEnumerable<string> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 18 Tf");
        content.AppendLine("50 780 Td");
        content.AppendLine($"({Escape(title)}) Tj");
        content.AppendLine("/F1 10 Tf");
        content.AppendLine("0 -28 Td");

        foreach (var line in lines.Take(38))
        {
            content.AppendLine($"({Escape(line)}) Tj");
            content.AppendLine("0 -18 Td");
        }

        content.AppendLine("ET");
        var stream = Encoding.ASCII.GetBytes(content.ToString());
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };

        AddObject(pdf, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
        AddObject(pdf, offsets, 2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        AddObject(pdf, offsets, 3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>");
        AddObject(pdf, offsets, 4, $"<< /Length {stream.Length} >>\nstream\n{content}endstream");
        AddObject(pdf, offsets, 5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.AppendLine("xref");
        pdf.AppendLine("0 6");
        pdf.AppendLine("0000000000 65535 f ");
        for (var i = 1; i < offsets.Count; i++) pdf.AppendLine($"{offsets[i]:D10} 00000 n ");
        pdf.AppendLine("trailer");
        pdf.AppendLine("<< /Size 6 /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(xref.ToString());
        pdf.AppendLine("%%EOF");

        File.WriteAllBytes(filePath, Encoding.ASCII.GetBytes(pdf.ToString()));
    }

    private static void AddObject(StringBuilder pdf, List<int> offsets, int number, string body)
    {
        offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
        pdf.AppendLine($"{number} 0 obj");
        pdf.AppendLine(body);
        pdf.AppendLine("endobj");
    }

    private static string Escape(string value)
    {
        var ascii = new string(value.Select(c => c is >= ' ' and <= '~' ? c : '?').ToArray());
        return ascii.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
