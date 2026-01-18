using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public interface IOfflineSaleService
    {
        Task<List<MedicineSearchResult>> SearchMedicinesAsync(string query);
        Task<MedicineSearchResult?> GetMedicineByIdAsync(int id);
        Task<OfflineSaleViewModel?> CreateSaleAsync(CreateOfflineSaleRequest request, string userId);
        Task<OfflineSaleViewModel?> GetSaleByIdAsync(int id);
        Task<OfflineSaleListViewModel> GetSalesAsync(int page = 1, int pageSize = 20, DateTime? fromDate = null, DateTime? toDate = null);
        Task<string> GenerateInvoiceNumberAsync();
    }

    public class OfflineSaleService : IOfflineSaleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGSTService _gstService;

        public OfflineSaleService(ApplicationDbContext context, IGSTService gstService)
        {
            _context = context;
            _gstService = gstService;
        }

        public async Task<List<MedicineSearchResult>> SearchMedicinesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await _context.Medicines
                    .Where(m => m.IsActive && m.StockQty > 0 && m.ExpiryDate > DateTime.Today)
                    .OrderBy(m => m.Name)
                    .Take(20)
                    .Select(m => new MedicineSearchResult
                    {
                        Id = m.Id,
                        Name = m.Name,
                        GenericName = m.GenericName ?? "",
                        BatchNo = m.BatchNo,
                        HSNCode = m.HSNCode ?? "",
                        MRP = m.MRP,
                        SellingPrice = m.SalePrice,
                        GSTPercent = m.GSTPercent,
                        AvailableStock = m.StockQty,
                        ExpiryDate = m.ExpiryDate
                    })
                    .ToListAsync();
            }

            return await _context.Medicines
                .Where(m => m.IsActive && m.StockQty > 0 && m.ExpiryDate > DateTime.Today &&
                       (m.Name.Contains(query) || m.GenericName!.Contains(query) || m.BatchNo.Contains(query)))
                .OrderBy(m => m.Name)
                .Take(20)
                .Select(m => new MedicineSearchResult
                {
                    Id = m.Id,
                    Name = m.Name,
                    GenericName = m.GenericName ?? "",
                    BatchNo = m.BatchNo,
                    HSNCode = m.HSNCode ?? "",
                    MRP = m.MRP,
                    SellingPrice = m.SalePrice,
                    GSTPercent = m.GSTPercent,
                    AvailableStock = m.StockQty,
                    ExpiryDate = m.ExpiryDate
                })
                .ToListAsync();
        }

        public async Task<MedicineSearchResult?> GetMedicineByIdAsync(int id)
        {
            return await _context.Medicines
                .Where(m => m.Id == id && m.IsActive && m.StockQty > 0)
                .Select(m => new MedicineSearchResult
                {
                    Id = m.Id,
                    Name = m.Name,
                    GenericName = m.GenericName ?? "",
                    BatchNo = m.BatchNo,
                    HSNCode = m.HSNCode ?? "",
                    MRP = m.MRP,
                    SellingPrice = m.SalePrice,
                    GSTPercent = m.GSTPercent,
                    AvailableStock = m.StockQty,
                    ExpiryDate = m.ExpiryDate
                })
                .FirstOrDefaultAsync();
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var today = DateTime.Today;
            var prefix = $"INV{today:yyyyMMdd}";
            
            var lastSale = await _context.OfflineSales
                .Where(s => s.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(s => s.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastSale != null)
            {
                var lastNumber = lastSale.InvoiceNumber.Substring(prefix.Length);
                if (int.TryParse(lastNumber, out int parsed))
                {
                    nextNumber = parsed + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task<OfflineSaleViewModel?> CreateSaleAsync(CreateOfflineSaleRequest request, string userId)
        {
            if (request.Items == null || !request.Items.Any())
                return null;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sale = new OfflineSale
                {
                    InvoiceNumber = await GenerateInvoiceNumberAsync(),
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    CustomerAddress = request.CustomerAddress,
                    SaleDate = DateTime.Now,
                    PaymentMethod = request.PaymentMethod,
                    DiscountAmount = request.DiscountAmount,
                    AmountReceived = request.AmountReceived,
                    Notes = request.Notes,
                    CreatedByUserId = userId
                };

                decimal subTotal = 0;
                decimal totalGST = 0;

                foreach (var itemRequest in request.Items)
                {
                    var medicine = await _context.Medicines.FindAsync(itemRequest.MedicineId);
                    if (medicine == null || medicine.StockQty < itemRequest.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for {medicine?.Name ?? "Unknown medicine"}");
                    }

                    var basePrice = medicine.SalePrice * itemRequest.Quantity;
                    var gstAmount = _gstService.CalculateGST(basePrice, medicine.GSTPercent);
                    var itemTotal = basePrice + gstAmount;

                    var saleItem = new OfflineSaleItem
                    {
                        MedicineId = medicine.Id,
                        BatchNo = medicine.BatchNo,
                        HSNCode = medicine.HSNCode ?? "",
                        Quantity = itemRequest.Quantity,
                        UnitPrice = medicine.SalePrice,
                        MRP = medicine.MRP,
                        GSTPercent = medicine.GSTPercent,
                        GSTAmount = gstAmount,
                        TotalPrice = itemTotal
                    };

                    sale.Items.Add(saleItem);
                    subTotal += basePrice;
                    totalGST += gstAmount;

                    // Update stock
                    medicine.StockQty -= itemRequest.Quantity;
                }

                sale.SubTotal = subTotal;
                sale.TotalGSTAmount = totalGST;
                sale.CGSTAmount = totalGST / 2;
                sale.SGSTAmount = totalGST / 2;
                sale.TotalAmount = subTotal + totalGST - request.DiscountAmount;
                sale.ChangeAmount = request.AmountReceived - sale.TotalAmount;

                _context.OfflineSales.Add(sale);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetSaleByIdAsync(sale.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OfflineSaleViewModel?> GetSaleByIdAsync(int id)
        {
            return await _context.OfflineSales
                .Include(s => s.Items)
                    .ThenInclude(i => i.Medicine)
                .Include(s => s.CreatedByUser)
                .Where(s => s.Id == id)
                .Select(s => new OfflineSaleViewModel
                {
                    Id = s.Id,
                    InvoiceNumber = s.InvoiceNumber,
                    CustomerName = s.CustomerName,
                    CustomerPhone = s.CustomerPhone,
                    CustomerAddress = s.CustomerAddress,
                    SaleDate = s.SaleDate,
                    SubTotal = s.SubTotal,
                    CGSTAmount = s.CGSTAmount,
                    SGSTAmount = s.SGSTAmount,
                    TotalGSTAmount = s.TotalGSTAmount,
                    DiscountAmount = s.DiscountAmount,
                    TotalAmount = s.TotalAmount,
                    PaymentMethod = s.PaymentMethod,
                    AmountReceived = s.AmountReceived,
                    ChangeAmount = s.ChangeAmount,
                    Notes = s.Notes,
                    CreatedByName = s.CreatedByUser != null ? s.CreatedByUser.FullName : null,
                    Items = s.Items.Select(i => new OfflineSaleItemViewModel
                    {
                        Id = i.Id,
                        MedicineId = i.MedicineId,
                        MedicineName = i.Medicine.Name,
                        BatchNo = i.BatchNo,
                        HSNCode = i.HSNCode,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        MRP = i.MRP,
                        GSTPercent = i.GSTPercent,
                        GSTAmount = i.GSTAmount,
                        TotalPrice = i.TotalPrice
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<OfflineSaleListViewModel> GetSalesAsync(int page = 1, int pageSize = 20, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.OfflineSales
                .Include(s => s.CreatedByUser)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(s => s.SaleDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.SaleDate <= toDate.Value.AddDays(1));

            var totalSales = await query.CountAsync();
            var totalRevenue = await query.SumAsync(s => s.TotalAmount);

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new OfflineSaleViewModel
                {
                    Id = s.Id,
                    InvoiceNumber = s.InvoiceNumber,
                    CustomerName = s.CustomerName,
                    CustomerPhone = s.CustomerPhone,
                    SaleDate = s.SaleDate,
                    SubTotal = s.SubTotal,
                    TotalGSTAmount = s.TotalGSTAmount,
                    DiscountAmount = s.DiscountAmount,
                    TotalAmount = s.TotalAmount,
                    PaymentMethod = s.PaymentMethod,
                    CreatedByName = s.CreatedByUser != null ? s.CreatedByUser.FullName : null
                })
                .ToListAsync();

            return new OfflineSaleListViewModel
            {
                Sales = sales,
                TotalSales = totalSales,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalSales / (double)pageSize),
                FromDate = fromDate,
                ToDate = toDate,
                TotalRevenue = totalRevenue
            };
        }
    }
}
