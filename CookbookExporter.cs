using RecipeApp.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Text;

public static class CookbookExporter
{
    public static byte[] ExportCookbook(List<Recipe> recipes, string title = "My Recipe Cookbook")
    {
        var document = new PdfDocument();
        // Use standard PDF fonts
        var font = new XFont("Helvetica", 14, XFontStyle.Regular);
        var titleFont = new XFont("Helvetica", 18, XFontStyle.Bold);
        var headerFont = new XFont("Helvetica", 16, XFontStyle.Bold);

        foreach (var recipe in recipes)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            double y = 30;

            try
            {
                // Title
                gfx.DrawString(title, titleFont, XBrushes.DarkRed, new XRect(0, y, page.Width, 30), XStringFormats.TopCenter);
                y += 40;

                // Recipe Title
                gfx.DrawString(recipe.Title ?? "(No Title)", headerFont, XBrushes.Black, new XRect(20, y, page.Width, 30), XStringFormats.TopLeft);
                y += 30;

                // Description
                gfx.DrawString($"Description: {recipe.Description ?? "(No Description)"}", font, XBrushes.Black, new XRect(20, y, page.Width - 40, page.Height), XStringFormats.TopLeft);
                y += 50;

                // Ingredients
                gfx.DrawString("Ingredients:", headerFont, XBrushes.DarkBlue, new XRect(20, y, page.Width, page.Height), XStringFormats.TopLeft);
                y += 25;
                gfx.DrawString(recipe.Ingredients ?? "(No Ingredients)", font, XBrushes.Black, new XRect(20, y, page.Width - 40, page.Height), XStringFormats.TopLeft);
                y += 80;

                // Steps
                gfx.DrawString("Steps:", headerFont, XBrushes.DarkGreen, new XRect(20, y, page.Width, page.Height), XStringFormats.TopLeft);
                y += 25;
                gfx.DrawString(recipe.Steps ?? "(No Steps)", font, XBrushes.Black, new XRect(20, y, page.Width - 40, page.Height), XStringFormats.TopLeft);
            }
            catch (Exception ex)
            {
                // Optionally, add some text to the PDF page explaining the error
                gfx.DrawString($"Error rendering recipe: {ex.Message}", font, XBrushes.Red, new XRect(20, y, page.Width - 40, page.Height), XStringFormats.TopLeft);
            }
        }

        using var ms = new MemoryStream();
        document.Save(ms);
        return ms.ToArray();
    }
}