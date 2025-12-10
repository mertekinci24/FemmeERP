using System;
using System.IO;
using System.Text;

namespace InventoryERP.Infrastructure.Services;

internal static class PdfSimpleGenerator
{
    // Generates a minimal valid PDF with given lines of text placed at fixed positions.
    public static byte[] Generate(params string[] lines)
    {
        // Build content stream with simple text operations
        var sb = new StringBuilder();
        sb.AppendLine("BT");
        sb.AppendLine("/F1 12 Tf");
        int y = 750;
        foreach (var line in lines)
        {
            var txt = (line ?? string.Empty).Replace("(", "\\(").Replace(")", "\\)");
            sb.AppendLine($"72 {y} Td ({txt}) Tj");
            y -= 16;
        }
        sb.AppendLine("ET");
        var content = Encoding.ASCII.GetBytes(sb.ToString());

        using var ms = new MemoryStream();
        using var w = new StreamWriter(ms, Encoding.ASCII, 1024, leaveOpen: true);
        w.NewLine = "\n";
        w.WriteLine("%PDF-1.4");

        long pos1, pos2, pos3, pos4, pos5;

        // 1: Catalog
        pos1 = ms.Position; w.WriteLine("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");
        // 2: Pages
        pos2 = ms.Position; w.WriteLine("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj");
        // 3: Page
        pos3 = ms.Position; w.WriteLine("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >> endobj");
        // 4: Contents
        pos4 = ms.Position; w.WriteLine($"4 0 obj << /Length {content.Length} >> stream");
        w.Flush();
        ms.Write(content, 0, content.Length);
        w.WriteLine("endstream endobj");
        // 5: Font
        pos5 = ms.Position; w.WriteLine("5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj");

        // xref
        var xrefPos = ms.Position;
        w.WriteLine("xref");
        w.WriteLine("0 6");
        w.WriteLine("0000000000 65535 f ");
        w.WriteLine(OffsetLine(pos1));
        w.WriteLine(OffsetLine(pos2));
        w.WriteLine(OffsetLine(pos3));
        w.WriteLine(OffsetLine(pos4));
        w.WriteLine(OffsetLine(pos5));
        // trailer
        w.WriteLine("trailer << /Size 6 /Root 1 0 R >>");
        w.WriteLine("startxref");
        w.WriteLine(xrefPos.ToString());
        w.WriteLine("%%EOF");
        w.Flush();
        return ms.ToArray();
    }

    private static string OffsetLine(long pos)
        => pos.ToString("D10") + " 00000 n ";
}
