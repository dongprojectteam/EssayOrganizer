using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using EssayOrganizer.App.Models;
using EssayOrganizer.App.Utils;
using EssayOrganizer.App.Views;

namespace EssayOrganizer.App.Services;

public class PrintService : IPrintService
{
    private static readonly Size A4Size = new(793.7, 1122.5);

    public void Print(Essay essay, FontSettings settings)
    {
        var document = BuildDocument(essay, settings);
        try
        {
            var preview = new PrintPreviewWindow(document) { Owner = Application.Current.MainWindow };
            preview.ShowDialog();
        }
        catch (InvalidOperationException)
        {
            PrintDirect(document);
        }
    }

    private static FlowDocument BuildDocument(Essay essay, FontSettings settings)
    {
        var bodySize = Math.Clamp(settings.BodyFontSize, FontSettings.MinSize, FontSettings.MaxSize);
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(48), PageWidth = A4Size.Width, PageHeight = A4Size.Height,
            ColumnWidth = double.PositiveInfinity, FontSize = bodySize,
            FontFamily = new System.Windows.Media.FontFamily("Malgun Gothic")
        };
        doc.Blocks.Add(new Paragraph(new Run(essay.Title))
        {
            FontSize = bodySize * 1.35, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 18)
        });
        AppendFormattedBody(doc, essay, bodySize);
        EssayDocumentAppearance.ApplyDisplayTheme(doc);
        return doc;
    }

    private static void AppendFormattedBody(FlowDocument target, Essay essay, double bodySize)
    {
        if (!string.IsNullOrWhiteSpace(essay.FormattedBody))
        {
            try
            {
                var source = new FlowDocument();
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(essay.FormattedBody));
                new TextRange(source.ContentStart, source.ContentEnd).Load(stream, DataFormats.Rtf);
                while (source.Blocks.FirstBlock is Block block)
                {
                    source.Blocks.Remove(block);
                    target.Blocks.Add(block);
                }

                EssayDocumentAppearance.ApplyLegacyFormattedBodyPolicy(target);
                return;
            }
            catch (ArgumentException)
            {
                // Fall back to plain text when an older formatting record is invalid.
            }
        }

        var lines = essay.Body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (var line in lines)
        {
            target.Blocks.Add(new Paragraph(new Run(line)) { FontSize = bodySize, Margin = new Thickness(0) });
        }
    }

    private static void PrintDirect(FlowDocument document)
    {
        EssayDocumentAppearance.ApplyPrintOutputTheme(document);
        if (document is not IDocumentPaginatorSource source) return;
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            if (dialog.PrintTicket is not null)
                dialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
            source.DocumentPaginator.PageSize = A4Size;
            dialog.PrintDocument(source.DocumentPaginator, "논술 출력");
        }
    }
}
