using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public interface IContactService
    {
        Task<ApiResponse<ContactMessage>> SubmitContactAsync(ContactMessage message);
        Task<PaginatedResult<ContactMessage>> GetAllMessagesAsync(
            ContactStatus? status = null,
            ContactCategory? category = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20);
        Task<ContactMessage?> GetMessageByIdAsync(int id);
        Task<ApiResponse<bool>> UpdateStatusAsync(int id, ContactStatus status, string adminUserId);
        Task<ApiResponse<bool>> RespondToMessageAsync(int id, string response, string adminUserId);
        Task<int> GetUnreadCountAsync();
        Task<List<ContactMessage>> GetRecentMessagesAsync(int count = 5);
    }

    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactService> _logger;

        public ContactService(ApplicationDbContext context, ILogger<ContactService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<ContactMessage>> SubmitContactAsync(ContactMessage message)
        {
            try
            {
                message.CreatedAt = DateTime.UtcNow;
                message.Status = ContactStatus.New;

                _context.ContactMessages.Add(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact message submitted: {Subject} from {Email}", message.Subject, message.Email);

                return new ApiResponse<ContactMessage> 
                { 
                    Success = true, 
                    Message = "Your message has been submitted successfully. We'll get back to you soon!",
                    Data = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting contact message");
                return new ApiResponse<ContactMessage>
                {
                    Success = false,
                    Message = "An error occurred while submitting your message. Please try again."
                };
            }
        }

        public async Task<PaginatedResult<ContactMessage>> GetAllMessagesAsync(
            ContactStatus? status = null,
            ContactCategory? category = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.ContactMessages
                .Include(c => c.User)
                .Include(c => c.Order)
                .Include(c => c.RespondedByUser)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status);
            }

            if (category.HasValue)
            {
                query = query.Where(c => c.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    c.Email.ToLower().Contains(searchTerm) ||
                    c.Subject.ToLower().Contains(searchTerm) ||
                    c.Message.ToLower().Contains(searchTerm) ||
                    (c.Phone != null && c.Phone.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var messages = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<ContactMessage>
            {
                Items = messages,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<ContactMessage?> GetMessageByIdAsync(int id)
        {
            return await _context.ContactMessages
                .Include(c => c.User)
                .Include(c => c.Order)
                .Include(c => c.RespondedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(int id, ContactStatus status, string adminUserId)
        {
            try
            {
                var message = await _context.ContactMessages.FindAsync(id);
                if (message == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Message not found." };
                }

                message.Status = status;
                message.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact message {Id} status updated to {Status} by {AdminId}", id, status, adminUserId);

                return new ApiResponse<bool> { Success = true, Message = $"Status updated to {status}.", Data = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact message status {Id}", id);
                return new ApiResponse<bool> { Success = false, Message = "An error occurred while updating the status." };
            }
        }

        public async Task<ApiResponse<bool>> RespondToMessageAsync(int id, string response, string adminUserId)
        {
            try
            {
                var message = await _context.ContactMessages.FindAsync(id);
                if (message == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Message not found." };
                }

                message.AdminResponse = response;
                message.RespondedByUserId = adminUserId;
                message.RespondedAt = DateTime.UtcNow;
                message.Status = ContactStatus.Resolved;
                message.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact message {Id} responded by {AdminId}", id, adminUserId);

                return new ApiResponse<bool> { Success = true, Message = "Response saved successfully.", Data = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to contact message {Id}", id);
                return new ApiResponse<bool> { Success = false, Message = "An error occurred while saving the response." };
            }
        }

        public async Task<int> GetUnreadCountAsync()
        {
            return await _context.ContactMessages
                .Where(c => c.Status == ContactStatus.New)
                .CountAsync();
        }

        public async Task<List<ContactMessage>> GetRecentMessagesAsync(int count = 5)
        {
            return await _context.ContactMessages
                .OrderByDescending(c => c.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
