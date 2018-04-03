#load "StorageMethods.csx"
#load "ViewModels.csx"
#load "CreatePdfReport.csx"
#r "Newtonsoft.Json"


using Newtonsoft.Json;
using System;
using System.Net;
using Dapper;
using System.Linq;
using System.Data;
using System.Data.SqlClient;

public static void Run(string myQueueItem, TraceWriter log)
{
    OrderViewModel Order = null;

    try
    {
        var orderId = Int32.Parse(myQueueItem);

        log.Info($"Querying database - OrderId: {orderId}");
        using (var conn = CreateDbConnection())
        {
            IEnumerable<OrderViewModel> data = conn.Query<OrderViewModel>("SELECT * FROM Orders WHERE OrderId = @orderid", new { orderid = orderId });
            Order = data.First();

            Order.OrderDetails = conn.Query<OrderDetailViewModel>(
                "SELECT od.*, p.ProductName FROM OrderDetails od JOIN Products p ON od.ProductId = p.ProductId WHERE OrderId = @orderid",
                new { orderid = orderId }
                );
        }

        log.Info($"Order Loaded - OrderId: {Order.OrderId}");

        // create PDF
        Order = ProcessOrder(Order, log);

        // Update database with ReceiptUrl
        using (var conn = CreateDbConnection())
        {
            conn.Execute(
                "UPDATE Orders SET ReceiptUrl = @receipturl WHERE OrderId = @orderid",
                new { receipturl = Order.ReceiptUrl, orderid = Order.OrderId }
                );
        }

    }
    catch (Exception ex)
    {
        log.Error("Error Processing Application", ex);

        throw;
    }

}

static IDbConnection CreateDbConnection()
{
    return new SqlConnection(
        System.Configuration.ConfigurationManager.ConnectionStrings["ContosoFinance"].ConnectionString
        );
}

static OrderViewModel ProcessOrder(OrderViewModel Order, TraceWriter log)
{
    string fileName = string.Format("ContosoFinance-Application-{0}.pdf", Order.OrderId);
    log.Info($"Using Filename {fileName}");

    var receipt = CreatePdfReport(Order, fileName, log);
    log.Info("PDF generated. Saving to blob storage...");

    Order.ReceiptUrl = UploadPdfToBlob(receipt, fileName, log);
    log.Info($"Using Order.ReceiptUrl {Order.ReceiptUrl}");

    return Order;
}