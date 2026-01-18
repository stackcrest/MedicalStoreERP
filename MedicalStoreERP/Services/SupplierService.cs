using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _context;

        public SupplierService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SupplierViewModel>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers
                .Where(s => s.IsActive)
                .Include(s => s.GRNs)
                .Select(s => new SupplierViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    ContactPerson = s.ContactPerson,
                    Phone = s.Phone,
                    Email = s.Email,
                    Address = s.Address,
                    City = s.City,
                    State = s.State,
                    PinCode = s.PinCode,
                    GSTIN = s.GSTIN,
                    PAN = s.PAN,
                    OpeningBalance = s.OpeningBalance,
                    CurrentBalance = s.CurrentBalance,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    TotalGRNs = s.GRNs.Count,
                    TotalPurchases = s.GRNs.Sum(g => g.TotalAmount)
                })
                .ToListAsync();
        }

        public async Task<SupplierViewModel?> GetByIdAsync(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.GRNs)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null) return null;

            return new SupplierViewModel
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                City = supplier.City,
                State = supplier.State,
                PinCode = supplier.PinCode,
                GSTIN = supplier.GSTIN,
                PAN = supplier.PAN,
                OpeningBalance = supplier.OpeningBalance,
                CurrentBalance = supplier.CurrentBalance,
                IsActive = supplier.IsActive,
                CreatedAt = supplier.CreatedAt,
                TotalGRNs = supplier.GRNs.Count,
                TotalPurchases = supplier.GRNs.Sum(g => g.TotalAmount)
            };
        }

        public async Task<ApiResponse<Supplier>> CreateAsync(SupplierCreateViewModel model)
        {
            try
            {
                var supplier = new Supplier
                {
                    Name = model.Name,
                    ContactPerson = model.ContactPerson,
                    Phone = model.Phone,
                    Email = model.Email,
                    Address = model.Address,
                    City = model.City,
                    State = model.State,
                    PinCode = model.PinCode,
                    GSTIN = model.GSTIN,
                    PAN = model.PAN,
                    OpeningBalance = model.OpeningBalance,
                    CurrentBalance = model.OpeningBalance,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Suppliers.AddAsync(supplier);
                await _context.SaveChangesAsync();

                // Add opening balance to ledger if any
                if (model.OpeningBalance > 0)
                {
                    var ledgerEntry = new SupplierLedger
                    {
                        SupplierId = supplier.Id,
                        TransactionDate = DateTime.UtcNow,
                        Description = "Opening Balance",
                        Credit = model.OpeningBalance,
                        Balance = model.OpeningBalance
                    };
                    await _context.SupplierLedgers.AddAsync(ledgerEntry);
                    await _context.SaveChangesAsync();
                }

                return new ApiResponse<Supplier>
                {
                    Success = true,
                    Message = "Supplier created successfully",
                    Data = supplier
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Supplier>
                {
                    Success = false,
                    Message = $"Error creating supplier: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Supplier>> UpdateAsync(int id, SupplierCreateViewModel model)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null)
                {
                    return new ApiResponse<Supplier> { Success = false, Message = "Supplier not found" };
                }

                supplier.Name = model.Name;
                supplier.ContactPerson = model.ContactPerson;
                supplier.Phone = model.Phone;
                supplier.Email = model.Email;
                supplier.Address = model.Address;
                supplier.City = model.City;
                supplier.State = model.State;
                supplier.PinCode = model.PinCode;
                supplier.GSTIN = model.GSTIN;
                supplier.PAN = model.PAN;
                supplier.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                return new ApiResponse<Supplier>
                {
                    Success = true,
                    Message = "Supplier updated successfully",
                    Data = supplier
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Supplier>
                {
                    Success = false,
                    Message = $"Error updating supplier: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.GRNs)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Supplier not found" };
                }

                if (supplier.GRNs.Any())
                {
                    supplier.IsActive = false;
                }
                else
                {
                    _context.Suppliers.Remove(supplier);
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Supplier deleted successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error deleting supplier: {ex.Message}"
                };
            }
        }

        public async Task<SupplierLedgerViewModel> GetLedgerAsync(int supplierId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var supplier = await GetByIdAsync(supplierId);
            if (supplier == null)
            {
                return new SupplierLedgerViewModel();
            }

            var query = _context.SupplierLedgers
                .Where(l => l.SupplierId == supplierId);

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.TransactionDate >= fromDate);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.TransactionDate <= toDate.Value.AddDays(1));
            }

            var ledgerItems = await query
                .OrderBy(l => l.TransactionDate)
                .Select(l => new SupplierLedgerItem
                {
                    Id = l.Id,
                    Date = l.TransactionDate,
                    Description = l.Description,
                    ReferenceNo = l.ReferenceNo,
                    Debit = l.Debit,
                    Credit = l.Credit,
                    Balance = l.Balance
                })
                .ToListAsync();

            return new SupplierLedgerViewModel
            {
                Supplier = supplier,
                Ledger = ledgerItems,
                OpeningBalance = supplier.OpeningBalance,
                TotalDebit = ledgerItems.Sum(l => l.Debit),
                TotalCredit = ledgerItems.Sum(l => l.Credit),
                ClosingBalance = supplier.CurrentBalance,
                FromDate = fromDate ?? DateTime.MinValue,
                ToDate = toDate ?? DateTime.MaxValue
            };
        }
    }
}
