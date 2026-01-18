using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class PdfService : IPdfService
    {
        private readonly IOrderService _orderService;
        private readonly IGRNService _grnService;
        private readonly IAppSettingsService _appSettingsService;

        public PdfService(IOrderService orderService, IGRNService grnService, IAppSettingsService appSettingsService)
        {
            _orderService = orderService;
            _grnService = grnService;
            _appSettingsService = appSettingsService;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                throw new Exception("Order not found");
            }

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Get settings from cache
            var settings = _appSettingsService.GetCachedSettings();
            
            // Header
            var storeName = settings.AppName;
            var storeAddress = settings.FullAddress;
            var storePhone = settings.ContactPhone;
            var storeEmail = settings.ContactEmail;
            var gstin = settings.GSTIN ?? "";

            // Store Info
            document.Add(new Paragraph(storeName)
                .SetFont(boldFont)
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph(storeAddress)
                .SetFont(normalFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"Phone: {storePhone} | Email: {storeEmail}")
                .SetFont(normalFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"GSTIN: {gstin}")
                .SetFont(boldFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("\n"));

            // Invoice Title
            document.Add(new Paragraph("TAX INVOICE")
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetPadding(5));

            document.Add(new Paragraph("\n"));

            // Invoice Details Table
            var infoTable = new Table(2).UseAllAvailableWidth();
            
            // Left column - Bill To
            var billToCell = new Cell()
                .Add(new Paragraph("Bill To:").SetFont(boldFont))
                .Add(new Paragraph(order.CustomerName).SetFont(normalFont))
                .Add(new Paragraph(order.DeliveryAddress).SetFont(normalFont))
                .Add(new Paragraph($"{order.City}, {order.State} - {order.PinCode}").SetFont(normalFont))
                .Add(new Paragraph($"Phone: {order.CustomerPhone}").SetFont(normalFont))
                .Add(new Paragraph($"Email: {order.CustomerEmail}").SetFont(normalFont))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);

            // Right column - Invoice Info
            var invoiceInfoCell = new Cell()
                .Add(new Paragraph($"Invoice No: {order.OrderNumber}").SetFont(boldFont))
                .Add(new Paragraph($"Date: {order.OrderDate:dd/MM/yyyy}").SetFont(normalFont))
                .Add(new Paragraph($"Payment: {order.PaymentMethod}").SetFont(normalFont))
                .Add(new Paragraph($"Status: {order.Status}").SetFont(normalFont))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT);

            infoTable.AddCell(billToCell);
            infoTable.AddCell(invoiceInfoCell);
            document.Add(infoTable);

            document.Add(new Paragraph("\n"));

            // Items Table
            var itemsTable = new Table(new float[] { 1, 4, 2, 2, 1, 2, 2, 2 }).UseAllAvailableWidth();
            
            // Header
            itemsTable.AddHeaderCell(CreateHeaderCell("S.No", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Description", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("HSN", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Batch", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Qty", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Rate", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("GST%", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Amount", boldFont));

            int sno = 1;
            foreach (var item in order.Items)
            {
                itemsTable.AddCell(CreateCell(sno.ToString(), normalFont));
                itemsTable.AddCell(CreateCell(item.MedicineName, normalFont));
                itemsTable.AddCell(CreateCell(item.HSNCode, normalFont));
                itemsTable.AddCell(CreateCell(item.BatchNo, normalFont));
                itemsTable.AddCell(CreateCell(item.Quantity.ToString(), normalFont));
                itemsTable.AddCell(CreateCell($"₹{item.UnitPrice:N2}", normalFont));
                itemsTable.AddCell(CreateCell($"{item.GSTPercent}%", normalFont));
                itemsTable.AddCell(CreateCell($"₹{item.TotalPrice:N2}", normalFont));
                sno++;
            }

            document.Add(itemsTable);

            document.Add(new Paragraph("\n"));

            // Summary Table
            var summaryTable = new Table(2).SetWidth(250).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            
            summaryTable.AddCell(CreateCell("Subtotal:", normalFont));
            summaryTable.AddCell(CreateCell($"₹{order.SubTotal:N2}", normalFont, TextAlignment.RIGHT));
            
            summaryTable.AddCell(CreateCell($"CGST:", normalFont));
            summaryTable.AddCell(CreateCell($"₹{order.CGSTAmount:N2}", normalFont, TextAlignment.RIGHT));
            
            summaryTable.AddCell(CreateCell($"SGST:", normalFont));
            summaryTable.AddCell(CreateCell($"₹{order.SGSTAmount:N2}", normalFont, TextAlignment.RIGHT));

            if (order.DiscountAmount > 0)
            {
                summaryTable.AddCell(CreateCell("Discount:", normalFont));
                summaryTable.AddCell(CreateCell($"-₹{order.DiscountAmount:N2}", normalFont, TextAlignment.RIGHT));
            }

            if (order.ShippingCharges > 0)
            {
                summaryTable.AddCell(CreateCell("Shipping:", normalFont));
                summaryTable.AddCell(CreateCell($"₹{order.ShippingCharges:N2}", normalFont, TextAlignment.RIGHT));
            }

            summaryTable.AddCell(CreateCell("Total:", boldFont).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            summaryTable.AddCell(CreateCell($"₹{order.TotalAmount:N2}", boldFont, TextAlignment.RIGHT).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

            document.Add(summaryTable);

            document.Add(new Paragraph("\n\n"));

            // Footer
            document.Add(new Paragraph("Terms & Conditions:")
                .SetFont(boldFont)
                .SetFontSize(10));
            document.Add(new Paragraph("1. Goods once sold will not be taken back or exchanged.")
                .SetFont(normalFont)
                .SetFontSize(8));
            document.Add(new Paragraph("2. All disputes are subject to local jurisdiction.")
                .SetFont(normalFont)
                .SetFontSize(8));

            document.Add(new Paragraph("\n\nThis is a computer generated invoice and does not require signature.")
                .SetFont(normalFont)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Close();
            return ms.ToArray();
        }

        public async Task<byte[]> GenerateGRNPdfAsync(int grnId)
        {
            var grn = await _grnService.GetByIdAsync(grnId);
            if (grn == null)
            {
                throw new Exception("GRN not found");
            }

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Header
            document.Add(new Paragraph("GOODS RECEIVED NOTE")
                .SetFont(boldFont)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"GRN No: {grn.GRNNumber}")
                .SetFont(boldFont)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("\n"));

            // GRN Info
            var infoTable = new Table(2).UseAllAvailableWidth();
            
            infoTable.AddCell(new Cell()
                .Add(new Paragraph("Supplier Details:").SetFont(boldFont))
                .Add(new Paragraph(grn.SupplierName).SetFont(normalFont))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            infoTable.AddCell(new Cell()
                .Add(new Paragraph($"GRN Date: {grn.GRNDate:dd/MM/yyyy}").SetFont(normalFont))
                .Add(new Paragraph($"Invoice No: {grn.SupplierInvoiceNo ?? "-"}").SetFont(normalFont))
                .Add(new Paragraph($"Invoice Date: {grn.SupplierInvoiceDate?.ToString("dd/MM/yyyy") ?? "-"}").SetFont(normalFont))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(infoTable);
            document.Add(new Paragraph("\n"));

            // Items Table
            var itemsTable = new Table(new float[] { 1, 4, 2, 2, 1, 1, 2, 2, 2 }).UseAllAvailableWidth();
            
            itemsTable.AddHeaderCell(CreateHeaderCell("S.No", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Medicine", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Batch", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Expiry", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Qty", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Free", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Rate", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("GST%", boldFont));
            itemsTable.AddHeaderCell(CreateHeaderCell("Amount", boldFont));

            int sno = 1;
            foreach (var item in grn.Items)
            {
                itemsTable.AddCell(CreateCell(sno.ToString(), normalFont));
                itemsTable.AddCell(CreateCell(item.MedicineName, normalFont));
                itemsTable.AddCell(CreateCell(item.BatchNo, normalFont));
                itemsTable.AddCell(CreateCell(item.ExpiryDate.ToString("MM/yyyy"), normalFont));
                itemsTable.AddCell(CreateCell(item.Quantity.ToString(), normalFont));
                itemsTable.AddCell(CreateCell(item.FreeQuantity.ToString(), normalFont));
                itemsTable.AddCell(CreateCell($"₹{item.PurchasePrice:N2}", normalFont));
                itemsTable.AddCell(CreateCell($"{item.GSTPercent}%", normalFont));
                itemsTable.AddCell(CreateCell($"₹{item.TotalAmount:N2}", normalFont));
                sno++;
            }

            document.Add(itemsTable);
            document.Add(new Paragraph("\n"));

            // Summary
            var summaryTable = new Table(2).SetWidth(200).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            summaryTable.AddCell(CreateCell("Subtotal:", normalFont));
            summaryTable.AddCell(CreateCell($"₹{grn.SubTotal:N2}", normalFont, TextAlignment.RIGHT));
            summaryTable.AddCell(CreateCell("GST:", normalFont));
            summaryTable.AddCell(CreateCell($"₹{grn.GSTAmount:N2}", normalFont, TextAlignment.RIGHT));
            summaryTable.AddCell(CreateCell("Total:", boldFont).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            summaryTable.AddCell(CreateCell($"₹{grn.TotalAmount:N2}", boldFont, TextAlignment.RIGHT).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

            document.Add(summaryTable);

            document.Close();
            return ms.ToArray();
        }

        public Task<byte[]> GenerateReportPdfAsync(string reportType, object data)
        {
            // Implement report PDF generation based on report type
            throw new NotImplementedException("Report PDF generation to be implemented");
        }

        private static Cell CreateHeaderCell(string text, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(9))
                .SetBackgroundColor(new DeviceRgb(66, 139, 202))
                .SetFontColor(ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5);
        }

        private static Cell CreateCell(string text, PdfFont font, TextAlignment alignment = TextAlignment.LEFT)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(9))
                .SetTextAlignment(alignment)
                .SetPadding(3);
        }
    }
}
