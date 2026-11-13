using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.IO;
using System.Xml;
using EssayOrganizer.App.Utils;
using Wpf.Ui.Controls;

namespace EssayOrganizer.App.Views;

public partial class PrintPreviewWindow : FluentWindow
{
    private static readonly Size A4Size = new(793.7, 1122.5); // 210mm x 297mm at 96 DPI

    public PrintPreviewWindow(FlowDocument document)
    {
        InitializeComponent();
        PreviewReader.Document = document;
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewReader.Document is not FlowDocument previewDocument)
        {
            return;
        }

        var printDocument = CloneDocument(previewDocument);
        EssayDocumentAppearance.ApplyPrintOutputTheme(printDocument);

        if (printDocument is not IDocumentPaginatorSource source)
        {
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            if (dialog.PrintTicket is not null)
            {
                dialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
            }

            var paginator = source.DocumentPaginator;
            paginator.PageSize = A4Size;
            dialog.PrintDocument(paginator, "Essay Print");
        }
    }

    private static FlowDocument CloneDocument(FlowDocument document)
    {
        var xaml = XamlWriter.Save(document);

        using var stringReader = new StringReader(xaml);
        using var xmlReader = XmlReader.Create(stringReader);

        return (FlowDocument)XamlReader.Load(xmlReader);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
