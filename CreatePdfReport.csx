#load "ViewModels.csx"

#r "System.Drawing"

using iTextSharp;
using iTextSharp.text;
using PdfRpt.Core.Contracts;
using PdfRpt.Core.Helper;
using PdfRpt.FluentInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

public static byte[] CreatePdfReport(OrderViewModel order, string fileName, TraceWriter log)
{
    var pdfReportData = new PdfReport().DocumentPreferences(doc =>
    {
        log.Info("PDF started...");
        log.Info($"OrderId =  {order.OrderId}");

        doc.RunDirection(PdfRunDirection.LeftToRight);
        doc.Orientation(PageOrientation.Portrait);
        doc.PageSize(PdfPageSize.A4);
        doc.DocumentMetadata(new DocumentMetadata { Author = "Contoso Finance", Application = "ContosoFinance.Apps", Subject = "Contoso Finance Application", Title = "Application" });
        doc.Compression(new CompressionSettings
        {
            EnableCompression = true,
            EnableFullCompression = true
        });
    })
    .DefaultFonts(fonts =>
    {
        fonts.Path(System.IO.Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "fonts\\arial.ttf"),
                   System.IO.Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "fonts\\verdana.ttf"));
        fonts.Size(9);
        fonts.Color(System.Drawing.Color.Black);
    })
    .PagesFooter(footer =>
    {
        footer.DefaultFooter(DateTime.Now.ToString("MM/dd/yyyy"));
    })
    .PagesHeader(header =>
    {
        header.CacheHeader(cache: true); // It's a default setting to improve the performance.
        header.DefaultHeader(defaultHeader =>
        {
            defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
            // defaultHeader.ImagePath(GetImage("logo.png"));
            defaultHeader.Message("Contoso Finance - Application");
            log.Info("in header...");
        });
    })
    .MainTableTemplate(template =>
    {
        template.BasicTemplate(BasicTemplate.ClassicTemplate);
    })
    .MainTablePreferences(table =>
    {
        table.ColumnsWidthsType(TableColumnWidthType.Relative);
    })
    .MainTableDataSource(dataSource =>
    {
        dataSource.StronglyTypedList(order.OrderDetails);
    })
    .MainTableSummarySettings(summarySettings =>
    {
        summarySettings.OverallSummarySettings("Summary");
        summarySettings.PreviousPageSummarySettings("Previous Page Summary");
        summarySettings.PageSummarySettings("Page Summary");
    })
    .MainTableColumns(columns =>
    {
        columns.AddColumn(column =>
        {
            column.PropertyName<OrderDetailViewModel>(o => o.ProductName);
            column.CellsHorizontalAlignment(HorizontalAlignment.Left);
            column.IsVisible(true);
            column.Order(0);
            column.Width(4);
            column.HeaderCell("Product");
        });

        columns.AddColumn(column =>
        {
            column.PropertyName<OrderDetailViewModel>(o => o.Rate);
            column.CellsHorizontalAlignment(HorizontalAlignment.Right);
            column.IsVisible(true);
            column.Order(2);
            column.Width(2);
            column.HeaderCell("Rate");
            column.ColumnItemsTemplate(template =>
            {
                template.TextBlock();
                template.DisplayFormatFormula(obj => obj == null || string.IsNullOrEmpty(obj.ToString())
                                                    ? string.Empty : obj.ToString());
            });
        });

    })
    .MainTableEvents(events =>
    {
        events.DataSourceIsEmpty(message: "There are no products to display.");

        events.MainTableAdded(args =>
        {
            var summaryTable = new PdfGrid(args.Table.RelativeWidths); // Create a clone of the MainTable's structure                   
            summaryTable.WidthPercentage = args.Table.WidthPercentage;
            summaryTable.SpacingBefore = args.Table.SpacingBefore;
            /*
            summaryTable.AddSimpleRow(
                null, null,
                (data, cellProperties) =>
                {
                    data.Value = "Total";
                    cellProperties.PdfFont = args.PdfFont;
                    cellProperties.HorizontalAlignment = HorizontalAlignment.Right;
                },
                (data, cellProperties) =>
                {
                    data.Value = string.Format("{0:c}", total);
                    cellProperties.PdfFont = args.PdfFont;
                    cellProperties.BorderColor = BaseColor.LIGHT_GRAY;
                    cellProperties.ShowBorder = true;
                });
            */
            args.PdfDoc.Add(summaryTable);
        });
    })
    .Export(export =>
    {
        export.ToExcel();
    })
    .Generate(data => data.AsPdfStream(new MemoryStream()));
    log.Info("PDF streamed...");
    return ((MemoryStream)pdfReportData.PdfStreamOutput).ToArray();
}