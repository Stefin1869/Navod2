using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Navod2.Core.Models;

namespace Navod2.Core.Parsers;

/// <summary>
/// Parsuje PDF soubor (alternativní zdroj k XML/ZIP).
/// </summary>
public class PdfDocumentParser
{
    public async Task<DocumentNode> ParseAsync(string pdfPath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            var root = new DocumentNode { Id = "root", Title = "Dokument" };

            using var pdf = PdfDocument.Open(pdfPath);
            int total = pdf.NumberOfPages;

            for (int i = 1; i <= total; i++)
            {
                var page = pdf.GetPage(i);
                var text = string.Join(" ", page.GetWords().Select(w => w.Text)).Trim();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    var node = new DocumentNode
                    {
                        Id = $"page-{i}",
                        IoId = $"page-{i}",
                        Title = $"Strana {i}",
                        Text = text,
                        NodePath = $"Strana {i}",
                        Parent = root
                    };
                    root.Children.Add(node);
                }

                progress?.Report((int)(i * 100.0 / total));
            }

            return root;
        });
    }
}
