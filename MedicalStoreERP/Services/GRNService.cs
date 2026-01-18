using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class GRNService : IGRNService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMedicineService _medicineService;

        public GRNService(ApplicationDbContext context, IMedicineService medicineService)
        {
            _context = context;
            _medicineService = medicineService;
        }

        public async Task<List<GRNViewModel>> GetAllGRNsAsync(DateTime? fromDate = null, DateTime? toDate = null, int? supplierId = null)
        {
            var query = _context.GRNs
                .Include(g => g.Supplier)
                .Include(g => g.Items)
                .ThenInclude(i => i.Medicine)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(g => g.GRNDate >= fromDate);
            }

            if (toDate.HasValue)
            {
                query = query.Where(g => g.GRNDate <= toDate.Value.AddDays(1));
            }

            if (supplierId.HasValue)
            {
                query = query.Where(g => g.SupplierId == supplierId);
            }

            return await query
                .OrderByDescending(g => g.GRNDate)
                .Select(g => new GRNViewModel
                {
                    Id = g.Id,
                    GRNNumber = g.GRNNumber,
                    SupplierId = g.SupplierId,
                    SupplierName = g.Supplier != null ? g.Supplier.Name : "",
                    SupplierInvoiceNo = g.SupplierInvoiceNo,
                    SupplierInvoiceDate = g.SupplierInvoiceDate,
                    GRNDate = g.GRNDate,
                    SubTotal = g.SubTotal,
                    GSTAmount = g.GSTAmount,
                    TotalAmount = g.TotalAmount,
                    Notes = g.Notes,
                    IsPosted = g.IsPosted,
                    CreatedAt = g.CreatedAt,
                    CreatedBy = g.CreatedBy,
                    Items = g.Items.Select(i => new GRNItemViewModel
                    {
                        Id = i.Id,
                        MedicineId = i.MedicineId,
                        MedicineName = i.Medicine != null ? i.Medicine.Name : "",
                        BatchNo = i.BatchNo,
                        ExpiryDate = i.ExpiryDate,
                        Quantity = i.Quantity,
                        FreeQuantity = i.FreeQuantity,
                        PurchasePrice = i.PurchasePrice,
                        MRP = i.MRP,
                        GSTPercent = i.GSTPercent,
                        GSTAmount = i.GSTAmount,
                        TotalAmount = i.TotalAmount
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<GRNViewModel?> GetByIdAsync(int id)
        {
            var grn = await _context.GRNs
                .Include(g => g.Supplier)
                .Include(g => g.Items)
                .ThenInclude(i => i.Medicine)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grn == null) return null;

            return new GRNViewModel
            {
                Id = grn.Id,
                GRNNumber = grn.GRNNumber,
                SupplierId = grn.SupplierId,
                SupplierName = grn.Supplier?.Name ?? "",
                SupplierInvoiceNo = grn.SupplierInvoiceNo,
                SupplierInvoiceDate = grn.SupplierInvoiceDate,
                GRNDate = grn.GRNDate,
                SubTotal = grn.SubTotal,
                GSTAmount = grn.GSTAmount,
                TotalAmount = grn.TotalAmount,
                Notes = grn.Notes,
                IsPosted = grn.IsPosted,
                CreatedAt = grn.CreatedAt,
                CreatedBy = grn.CreatedBy,
                Items = grn.Items.Select(i => new GRNItemViewModel
                {
                    Id = i.Id,
                    MedicineId = i.MedicineId,
                    MedicineName = i.Medicine?.Name ?? "",
                    BatchNo = i.BatchNo,
                    ExpiryDate = i.ExpiryDate,
                    Quantity = i.Quantity,
                    FreeQuantity = i.FreeQuantity,
                    PurchasePrice = i.PurchasePrice,
                    MRP = i.MRP,
                    GSTPercent = i.GSTPercent,
                    GSTAmount = i.GSTAmount,
                    TotalAmount = i.TotalAmount
                }).ToList()
            };
        }

        public async Task<ApiResponse<GRN>> CreateAsync(GRNCreateViewModel model, string createdBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!model.Items.Any())
                {
                    return new ApiResponse<GRN> { Success = false, Message = "GRN must have at least one item" };
                }

                var grnNumber = await GenerateGRNNumberAsync();

                decimal subTotal = 0;
                decimal totalGST = 0;

                foreach (var item in model.Items)
                {
                    var itemTotal = item.Quantity * item.PurchasePrice;
                    var itemGST = itemTotal * item.GSTPercent / 100;
                    subTotal += itemTotal;
                    totalGST += itemGST;
                }

                var grn = new GRN
                {
                    GRNNumber = grnNumber,
                    SupplierId = model.SupplierId,
                    SupplierInvoiceNo = model.SupplierInvoiceNo,
                    SupplierInvoiceDate = model.SupplierInvoiceDate,
                    GRNDate = model.GRNDate,
                    SubTotal = subTotal,
                    GSTAmount = totalGST,
                    TotalAmount = subTotal + totalGST,
                    Notes = model.Notes,
                    IsPosted = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await _context.GRNs.AddAsync(grn);
                await _context.SaveChangesAsync();

                // Add GRN items and update stock
                foreach (var item in model.Items)
                {
                    var itemTotal = item.Quantity * item.PurchasePrice;
                    var itemGST = itemTotal * item.GSTPercent / 100;

                    var grnItem = new GRNItem
                    {
                        GRNId = grn.Id,
                        MedicineId = item.MedicineId,
                        BatchNo = item.BatchNo,
                        ExpiryDate = item.ExpiryDate,
                        Quantity = item.Quantity,
                        FreeQuantity = item.FreeQuantity,
                        PurchasePrice = item.PurchasePrice,
                        MRP = item.MRP,
                        GSTPercent = item.GSTPercent,
                        GSTAmount = itemGST,
                        TotalAmount = itemTotal + itemGST
                    };

                    await _context.GRNItems.AddAsync(grnItem);

                    // Update medicine stock
                    var totalQty = item.Quantity + item.FreeQuantity;
                    await _medicineService.UpdateStockAsync(item.MedicineId, totalQty, true);

                    // Update medicine batch and expiry if newer
                    var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                    if (medicine != null)
                    {
                        medicine.BatchNo = item.BatchNo;
                        medicine.ExpiryDate = item.ExpiryDate;
                        medicine.PurchasePrice = item.PurchasePrice;
                        medicine.MRP = item.MRP;
                        medicine.SalePrice = item.MRP * 0.9m; // Default 10% margin
                        medicine.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                // Update supplier ledger
                var supplier = await _context.Suppliers.FindAsync(model.SupplierId);
                if (supplier != null)
                {
                    supplier.CurrentBalance += grn.TotalAmount;

                    var ledgerEntry = new SupplierLedger
                    {
                        SupplierId = model.SupplierId,
                        TransactionDate = DateTime.UtcNow,
                        Description = $"GRN #{grnNumber}",
                        ReferenceNo = grnNumber,
                        Credit = grn.TotalAmount,
                        Balance = supplier.CurrentBalance
                    };
                    await _context.SupplierLedgers.AddAsync(ledgerEntry);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return new ApiResponse<GRN>
                {
                    Success = true,
                    Message = "GRN created successfully",
                    Data = grn
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse<GRN>
                {
                    Success = false,
                    Message = $"Error creating GRN: {ex.Message}"
                };
            }
        }

        public async Task<string> GenerateGRNNumberAsync()
        {
            var today = DateTime.Today;
            var grnsToday = await _context.GRNs
                .CountAsync(g => g.GRNDate.Date == today);

            return $"GRN{today:yyyyMMdd}{(grnsToday + 1):D4}";
        }
    }
}
