using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using RecipeApp.Models;
using System.IO;

namespace RecipeApp.Helpers
{
    public static class PdfExporter
    {
        public static byte[] ExportRecipeToPdf(Recipe recipe)
        {
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
            var subFont = new XFont("Arial", 12);
            var y = 40;

            void DrawText(string label, string content)
            {
                gfx.DrawString(label, titleFont, XBrushes.Black, new XPoint(40, y));
                y += 25;
                gfx.DrawString(content, subFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 200), XStringFormats.TopLeft);
                y += 80;
            }

            DrawText("Title:", recipe.Title);
            DrawText("Description:", recipe.Description);
            DrawText("Ingredients:", recipe.Ingredients);
            DrawText("Steps:", recipe.Steps);

            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }
    }
}
