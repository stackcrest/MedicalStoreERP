using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public interface IAppSettingsService
    {
        Task<string?> GetSettingAsync(string key);
        Task<T?> GetSettingAsync<T>(string key);
        Task<bool> SetSettingAsync(string key, string? value, string? userId = null);
        Task<Dictionary<string, string?>> GetSettingsByGroupAsync(string group);
        Task<Dictionary<string, string?>> GetAllSettingsAsync();
        Task<List<AppSetting>> GetAllSettingEntitiesAsync();
        Task<List<AppSetting>> GetSettingEntitiesByGroupAsync(string group);
        Task<AppSetting?> GetSettingEntityAsync(string key);
        Task<bool> UpdateSettingAsync(AppSetting setting, string userId);
        Task InitializeDefaultSettingsAsync();
        void ClearCache();
        AppSettingsViewModel GetCachedSettings();
    }

    public class AppSettingsService : IAppSettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AppSettingsService> _logger;
        private const string CacheKey = "AppSettings";
        private const string ViewModelCacheKey = "AppSettingsViewModel";

        public AppSettingsService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<AppSettingsService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            var settings = await GetAllSettingsAsync();
            return settings.TryGetValue(key, out var value) ? value : null;
        }

        public async Task<T?> GetSettingAsync<T>(string key)
        {
            var value = await GetSettingAsync(key);
            if (string.IsNullOrEmpty(value))
                return default;

            try
            {
                if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(value);
                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(value);
                if (typeof(T) == typeof(decimal))
                    return (T)(object)decimal.Parse(value);
                if (typeof(T) == typeof(double))
                    return (T)(object)double.Parse(value);
                
                return (T)(object)value;
            }
            catch
            {
                return default;
            }
        }

        public async Task<bool> SetSettingAsync(string key, string? value, string? userId = null)
        {
            try
            {
                var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
                
                if (setting == null)
                {
                    setting = new AppSetting
                    {
                        SettingKey = key,
                        SettingValue = value,
                        UpdatedByUserId = userId,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.AppSettings.Add(setting);
                }
                else
                {
                    setting.SettingValue = value;
                    setting.UpdatedByUserId = userId;
                    setting.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                ClearCache();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value for key: {Key}", key);
                return false;
            }
        }

        public async Task<Dictionary<string, string?>> GetSettingsByGroupAsync(string group)
        {
            var settings = await _context.AppSettings
                .Where(s => s.SettingGroup == group)
                .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);
            return settings;
        }

        public async Task<Dictionary<string, string?>> GetAllSettingsAsync()
        {
            if (_cache.TryGetValue(CacheKey, out Dictionary<string, string?>? cachedSettings) && cachedSettings != null)
            {
                return cachedSettings;
            }

            var settings = await _context.AppSettings
                .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetAbsoluteExpiration(TimeSpan.FromHours(2));

            _cache.Set(CacheKey, settings, cacheOptions);

            return settings;
        }

        public async Task<List<AppSetting>> GetAllSettingEntitiesAsync()
        {
            return await _context.AppSettings
                .Include(s => s.UpdatedByUser)
                .OrderBy(s => s.SettingGroup)
                .ThenBy(s => s.SettingKey)
                .ToListAsync();
        }

        public async Task<List<AppSetting>> GetSettingEntitiesByGroupAsync(string group)
        {
            return await _context.AppSettings
                .Include(s => s.UpdatedByUser)
                .Where(s => s.SettingGroup == group)
                .OrderBy(s => s.SettingKey)
                .ToListAsync();
        }

        public async Task<AppSetting?> GetSettingEntityAsync(string key)
        {
            return await _context.AppSettings
                .Include(s => s.UpdatedByUser)
                .FirstOrDefaultAsync(s => s.SettingKey == key);
        }

        public async Task<bool> UpdateSettingAsync(AppSetting setting, string userId)
        {
            try
            {
                var existingSetting = await _context.AppSettings.FindAsync(setting.Id);
                if (existingSetting == null) return false;

                existingSetting.SettingValue = setting.SettingValue;
                existingSetting.UpdatedByUserId = userId;
                existingSetting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                ClearCache();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting: {Key}", setting.SettingKey);
                return false;
            }
        }

        public void ClearCache()
        {
            _cache.Remove(CacheKey);
            _cache.Remove(ViewModelCacheKey);
        }

        public AppSettingsViewModel GetCachedSettings()
        {
            if (_cache.TryGetValue(ViewModelCacheKey, out AppSettingsViewModel? cachedViewModel) && cachedViewModel != null)
            {
                return cachedViewModel;
            }

            var settings = _context.AppSettings.ToDictionary(s => s.SettingKey, s => s.SettingValue);
            var viewModel = MapToViewModel(settings);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetAbsoluteExpiration(TimeSpan.FromHours(2));

            _cache.Set(ViewModelCacheKey, viewModel, cacheOptions);

            return viewModel;
        }

        private AppSettingsViewModel MapToViewModel(Dictionary<string, string?> settings)
        {
            return new AppSettingsViewModel
            {
                // General
                AppName = settings.GetValueOrDefault(SettingKeys.AppName) ?? "MedPharma Store",
                AppTagline = settings.GetValueOrDefault(SettingKeys.AppTagline) ?? "Your Health, Our Priority",
                AppDescription = settings.GetValueOrDefault(SettingKeys.AppDescription),
                AppLogo = settings.GetValueOrDefault(SettingKeys.AppLogo) ?? "/images/logo.png",
                AppFavicon = settings.GetValueOrDefault(SettingKeys.AppFavicon) ?? "/favicon.ico",

                // Theme
                PrimaryColor = settings.GetValueOrDefault(SettingKeys.PrimaryColor) ?? "#0d6efd",
                SecondaryColor = settings.GetValueOrDefault(SettingKeys.SecondaryColor) ?? "#6c757d",
                AccentColor = settings.GetValueOrDefault(SettingKeys.AccentColor) ?? "#198754",
                HeaderBgColor = settings.GetValueOrDefault(SettingKeys.HeaderBgColor) ?? "#ffffff",
                FooterBgColor = settings.GetValueOrDefault(SettingKeys.FooterBgColor) ?? "#212529",
                FooterTextColor = settings.GetValueOrDefault(SettingKeys.FooterTextColor) ?? "#adb5bd",

                // Contact
                ContactEmail = settings.GetValueOrDefault(SettingKeys.ContactEmail) ?? "contact@medpharma.com",
                ContactPhone = settings.GetValueOrDefault(SettingKeys.ContactPhone) ?? "+91-9876543210",
                ContactPhone2 = settings.GetValueOrDefault(SettingKeys.ContactPhone2),
                ContactAddress = settings.GetValueOrDefault(SettingKeys.ContactAddress) ?? "123 Health Street",
                ContactCity = settings.GetValueOrDefault(SettingKeys.ContactCity) ?? "Mumbai",
                ContactState = settings.GetValueOrDefault(SettingKeys.ContactState) ?? "Maharashtra",
                ContactPinCode = settings.GetValueOrDefault(SettingKeys.ContactPinCode) ?? "400001",
                ContactCountry = settings.GetValueOrDefault(SettingKeys.ContactCountry) ?? "India",
                SupportHours = settings.GetValueOrDefault(SettingKeys.SupportHours) ?? "24/7 Support Available",

                // Business
                GSTIN = settings.GetValueOrDefault(SettingKeys.GSTIN) ?? "29AABCU9603R1ZM",
                DrugLicenseNo = settings.GetValueOrDefault(SettingKeys.DrugLicenseNo),
                FSSAINo = settings.GetValueOrDefault(SettingKeys.FSSAINo),
                BusinessHours = settings.GetValueOrDefault(SettingKeys.BusinessHours) ?? "Mon-Sat: 9AM-9PM",

                // Map
                StoreLatitude = double.TryParse(settings.GetValueOrDefault(SettingKeys.StoreLatitude), out var lat) ? lat : 19.0760,
                StoreLongitude = double.TryParse(settings.GetValueOrDefault(SettingKeys.StoreLongitude), out var lng) ? lng : 72.8777,
                MaxDeliveryRadiusKm = double.TryParse(settings.GetValueOrDefault(SettingKeys.MaxDeliveryRadiusKm), out var radius) ? radius : 10,
                MapZoomLevel = int.TryParse(settings.GetValueOrDefault(SettingKeys.MapZoomLevel), out var zoom) ? zoom : 13,
                EnableSameDayDelivery = bool.TryParse(settings.GetValueOrDefault(SettingKeys.EnableSameDayDelivery), out var sameDay) && sameDay,
                SameDayDeliveryCutoffHour = int.TryParse(settings.GetValueOrDefault(SettingKeys.SameDayDeliveryCutoffHour), out var cutoff) ? cutoff : 14,

                // Social Media
                FacebookUrl = settings.GetValueOrDefault(SettingKeys.FacebookUrl),
                TwitterUrl = settings.GetValueOrDefault(SettingKeys.TwitterUrl),
                InstagramUrl = settings.GetValueOrDefault(SettingKeys.InstagramUrl),
                LinkedInUrl = settings.GetValueOrDefault(SettingKeys.LinkedInUrl),
                YouTubeUrl = settings.GetValueOrDefault(SettingKeys.YouTubeUrl),

                // WhatsApp
                WhatsAppNumber = settings.GetValueOrDefault(SettingKeys.WhatsAppNumber) ?? "919876543210",
                WhatsAppMessage = settings.GetValueOrDefault(SettingKeys.WhatsAppMessage) ?? "Hello! I need help with my order.",
                WhatsAppEnabled = bool.TryParse(settings.GetValueOrDefault(SettingKeys.WhatsAppEnabled), out var waEnabled) && waEnabled,
                WhatsAppButtonText = settings.GetValueOrDefault(SettingKeys.WhatsAppButtonText) ?? "Need Help?",
                WhatsAppWelcomeMessage = settings.GetValueOrDefault(SettingKeys.WhatsAppWelcomeMessage) ?? "Chat with our support team on WhatsApp for quick assistance!",
                WhatsAppDefaultMessage = settings.GetValueOrDefault(SettingKeys.WhatsAppDefaultMessage) ?? "Hi! I'm {userName} and I need assistance.\n\nPage: {page}\n\nMy Query: ",

                // About
                AboutTitle = settings.GetValueOrDefault(SettingKeys.AboutTitle) ?? "About Us",
                AboutContent = settings.GetValueOrDefault(SettingKeys.AboutContent),
                AboutImage = settings.GetValueOrDefault(SettingKeys.AboutImage),
                MissionStatement = settings.GetValueOrDefault(SettingKeys.MissionStatement),
                VisionStatement = settings.GetValueOrDefault(SettingKeys.VisionStatement),

                // Footer
                FooterAbout = settings.GetValueOrDefault(SettingKeys.FooterAbout),
                CopyrightText = settings.GetValueOrDefault(SettingKeys.CopyrightText) ?? "© 2026 MedPharma Store. All rights reserved.",

                // SEO
                MetaTitle = settings.GetValueOrDefault(SettingKeys.MetaTitle),
                MetaDescription = settings.GetValueOrDefault(SettingKeys.MetaDescription),
                MetaKeywords = settings.GetValueOrDefault(SettingKeys.MetaKeywords),

                // E-commerce
                DiscountThreshold = decimal.TryParse(settings.GetValueOrDefault(SettingKeys.DiscountThreshold), out var threshold) ? threshold : 500,
                DiscountPercent = decimal.TryParse(settings.GetValueOrDefault(SettingKeys.DiscountPercent), out var percent) ? percent : 5,
                MinOrderAmount = decimal.TryParse(settings.GetValueOrDefault(SettingKeys.MinOrderAmount), out var minOrder) ? minOrder : 0,
                FreeShippingThreshold = decimal.TryParse(settings.GetValueOrDefault(SettingKeys.FreeShippingThreshold), out var freeShip) ? freeShip : 0,

                // Homepage
                HeroBannerTitle = settings.GetValueOrDefault(SettingKeys.HeroBannerTitle) ?? "Your Trusted Online Pharmacy",
                HeroBannerSubtitle = settings.GetValueOrDefault(SettingKeys.HeroBannerSubtitle) ?? "Quality medicines delivered to your doorstep",
                HeroBannerImage = settings.GetValueOrDefault(SettingKeys.HeroBannerImage),
                ShowFeaturedProducts = bool.TryParse(settings.GetValueOrDefault(SettingKeys.ShowFeaturedProducts), out var showFeatured) && showFeatured,
                ShowLatestArticles = bool.TryParse(settings.GetValueOrDefault(SettingKeys.ShowLatestArticles), out var showArticles) && showArticles
            };
        }

        public async Task InitializeDefaultSettingsAsync()
        {
            var existingCount = await _context.AppSettings.CountAsync();
            if (existingCount > 0) return;

            var defaultSettings = new List<AppSetting>
            {
                // General Settings
                new() { SettingKey = SettingKeys.AppName, SettingValue = "MedPharma Store", SettingGroup = SettingGroups.General, Description = "Application name displayed in header and title", SettingType = "Text" },
                new() { SettingKey = SettingKeys.AppTagline, SettingValue = "Your Health, Our Priority", SettingGroup = SettingGroups.General, Description = "Tagline shown below logo", SettingType = "Text" },
                new() { SettingKey = SettingKeys.AppDescription, SettingValue = "Your trusted online pharmacy for quality medicines and healthcare products.", SettingGroup = SettingGroups.General, Description = "Brief description of the application", SettingType = "Text" },
                new() { SettingKey = SettingKeys.AppLogo, SettingValue = "/images/logo.png", SettingGroup = SettingGroups.General, Description = "Main logo image URL", SettingType = "Image" },
                new() { SettingKey = SettingKeys.AppFavicon, SettingValue = "/favicon.ico", SettingGroup = SettingGroups.General, Description = "Favicon URL", SettingType = "Image" },

                // Theme Settings
                new() { SettingKey = SettingKeys.PrimaryColor, SettingValue = "#0d6efd", SettingGroup = SettingGroups.Theme, Description = "Primary brand color", SettingType = "Color" },
                new() { SettingKey = SettingKeys.SecondaryColor, SettingValue = "#6c757d", SettingGroup = SettingGroups.Theme, Description = "Secondary color", SettingType = "Color" },
                new() { SettingKey = SettingKeys.AccentColor, SettingValue = "#198754", SettingGroup = SettingGroups.Theme, Description = "Accent color for highlights", SettingType = "Color" },
                new() { SettingKey = SettingKeys.HeaderBgColor, SettingValue = "#ffffff", SettingGroup = SettingGroups.Theme, Description = "Header background color", SettingType = "Color" },
                new() { SettingKey = SettingKeys.FooterBgColor, SettingValue = "#212529", SettingGroup = SettingGroups.Theme, Description = "Footer background color", SettingType = "Color" },
                new() { SettingKey = SettingKeys.FooterTextColor, SettingValue = "#adb5bd", SettingGroup = SettingGroups.Theme, Description = "Footer text color", SettingType = "Color" },

                // Contact Settings
                new() { SettingKey = SettingKeys.ContactEmail, SettingValue = "contact@medpharma.com", SettingGroup = SettingGroups.Contact, Description = "Primary contact email", SettingType = "Text" },
                new() { SettingKey = SettingKeys.ContactPhone, SettingValue = "+91-9876543210", SettingGroup = SettingGroups.Contact, Description = "Primary contact phone", SettingType = "Text" },
                new() { SettingKey = SettingKeys.ContactPhone2, SettingValue = "", SettingGroup = SettingGroups.Contact, Description = "Secondary contact phone", SettingType = "Text" },
                new() { SettingKey = SettingKeys.ContactAddress, SettingValue = "123 Health Street, Medical City", SettingGroup = SettingGroups.Contact, Description = "Store address", SettingType = "Text" },
                new() { SettingKey = SettingKeys.ContactCity, SettingValue = "Mumbai", SettingGroup = SettingGroups.Contact, Description = "City", SettingType = "Text" },
                new() { SettingKey = SettingKeys.ContactState, SettingValue = "Maharashtra", SettingGroup = SettingGroups.Contact, Description = "State", SettingType = "Text" },
                new() { SettingKey = SettingKeys.ContactPinCode, SettingValue = "400001", SettingGroup = SettingGroups.Contact, Description = "PIN Code", SettingType = "Text" },
                new() { SettingKey = SettingKeys.ContactCountry, SettingValue = "India", SettingGroup = SettingGroups.Contact, Description = "Country", SettingType = "Text" },
                new() { SettingKey = SettingKeys.SupportHours, SettingValue = "24/7 Support Available", SettingGroup = SettingGroups.Contact, Description = "Support hours text", SettingType = "Text" },

                // Business Settings
                new() { SettingKey = SettingKeys.GSTIN, SettingValue = "29AABCU9603R1ZM", SettingGroup = SettingGroups.Business, Description = "GST Identification Number", SettingType = "Text" },
                new() { SettingKey = SettingKeys.DrugLicenseNo, SettingValue = "DL-2024-12345", SettingGroup = SettingGroups.Business, Description = "Drug License Number", SettingType = "Text" },
                new() { SettingKey = SettingKeys.FSSAINo, SettingValue = "", SettingGroup = SettingGroups.Business, Description = "FSSAI License Number", SettingType = "Text" },
                new() { SettingKey = SettingKeys.BusinessHours, SettingValue = "Mon-Sat: 9:00 AM - 9:00 PM, Sun: 10:00 AM - 6:00 PM", SettingGroup = SettingGroups.Business, Description = "Business operating hours", SettingType = "Text" },

                // Map Settings
                new() { SettingKey = SettingKeys.StoreLatitude, SettingValue = "19.0760", SettingGroup = SettingGroups.Map, Description = "Store latitude coordinate", SettingType = "Text" },
                new() { SettingKey = SettingKeys.StoreLongitude, SettingValue = "72.8777", SettingGroup = SettingGroups.Map, Description = "Store longitude coordinate", SettingType = "Text" },
                new() { SettingKey = SettingKeys.MaxDeliveryRadiusKm, SettingValue = "10", SettingGroup = SettingGroups.Map, Description = "Maximum delivery radius in kilometers", SettingType = "Text" },
                new() { SettingKey = SettingKeys.MapZoomLevel, SettingValue = "13", SettingGroup = SettingGroups.Map, Description = "Default map zoom level", SettingType = "Text" },
                new() { SettingKey = SettingKeys.EnableSameDayDelivery, SettingValue = "true", SettingGroup = SettingGroups.Map, Description = "Enable same-day delivery feature", SettingType = "Boolean" },
                new() { SettingKey = SettingKeys.SameDayDeliveryCutoffHour, SettingValue = "14", SettingGroup = SettingGroups.Map, Description = "Cutoff hour for same-day delivery (24h format)", SettingType = "Text" },

                // Social Media Settings
                new() { SettingKey = SettingKeys.FacebookUrl, SettingValue = "https://facebook.com/medpharma", SettingGroup = SettingGroups.SocialMedia, Description = "Facebook page URL", SettingType = "Text" },
                new() { SettingKey = SettingKeys.TwitterUrl, SettingValue = "https://twitter.com/medpharma", SettingGroup = SettingGroups.SocialMedia, Description = "Twitter/X profile URL", SettingType = "Text" },
                new() { SettingKey = SettingKeys.InstagramUrl, SettingValue = "https://instagram.com/medpharma", SettingGroup = SettingGroups.SocialMedia, Description = "Instagram profile URL", SettingType = "Text" },
                new() { SettingKey = SettingKeys.LinkedInUrl, SettingValue = "", SettingGroup = SettingGroups.SocialMedia, Description = "LinkedIn page URL", SettingType = "Text" },
                new() { SettingKey = SettingKeys.YouTubeUrl, SettingValue = "", SettingGroup = SettingGroups.SocialMedia, Description = "YouTube channel URL", SettingType = "Text" },

                // WhatsApp Settings
                new() { SettingKey = SettingKeys.WhatsAppNumber, SettingValue = "919876543210", SettingGroup = SettingGroups.WhatsApp, Description = "WhatsApp number (with country code, no +)", SettingType = "Text" },
                new() { SettingKey = SettingKeys.WhatsAppMessage, SettingValue = "Hello! I need help with my order.", SettingGroup = SettingGroups.WhatsApp, Description = "Default WhatsApp message", SettingType = "Text" },
                new() { SettingKey = SettingKeys.WhatsAppEnabled, SettingValue = "true", SettingGroup = SettingGroups.WhatsApp, Description = "Enable WhatsApp chat button", SettingType = "Boolean" },
                new() { SettingKey = SettingKeys.WhatsAppAdminNumber, SettingValue = "919876543210", SettingGroup = SettingGroups.WhatsApp, Description = "Admin WhatsApp for order notifications", SettingType = "Text" },
                new() { SettingKey = SettingKeys.WhatsAppButtonText, SettingValue = "Need Help?", SettingGroup = SettingGroups.WhatsApp, Description = "WhatsApp button tooltip text", SettingType = "Text" },
                new() { SettingKey = SettingKeys.WhatsAppWelcomeMessage, SettingValue = "Chat with our support team on WhatsApp for quick assistance!", SettingGroup = SettingGroups.WhatsApp, Description = "WhatsApp tooltip welcome message", SettingType = "Text" },
                new() { SettingKey = SettingKeys.WhatsAppDefaultMessage, SettingValue = "Hi! I'm {userName} and I need assistance.\n\nPage: {page}\n\nMy Query: ", SettingGroup = SettingGroups.WhatsApp, Description = "Default message template (use {userName} and {page} as placeholders)", SettingType = "Text" },

                // About Settings
                new() { SettingKey = SettingKeys.AboutTitle, SettingValue = "About MedPharma Store", SettingGroup = SettingGroups.About, Description = "About page title", SettingType = "Text" },
                new() { SettingKey = SettingKeys.AboutContent, SettingValue = "<p>MedPharma Store is your trusted online pharmacy, committed to providing quality medicines and healthcare products at affordable prices. With a team of licensed pharmacists and healthcare professionals, we ensure that you receive authentic products with proper guidance.</p><p>We believe in making healthcare accessible to everyone, which is why we offer doorstep delivery within a 10km radius with same-day delivery options.</p>", SettingGroup = SettingGroups.About, Description = "About page content (HTML)", SettingType = "Html" },
                new() { SettingKey = SettingKeys.AboutImage, SettingValue = "/images/about-us.jpg", SettingGroup = SettingGroups.About, Description = "About page image", SettingType = "Image" },
                new() { SettingKey = SettingKeys.MissionStatement, SettingValue = "To make quality healthcare accessible to everyone by providing genuine medicines at affordable prices with excellent customer service.", SettingGroup = SettingGroups.About, Description = "Company mission statement", SettingType = "Text" },
                new() { SettingKey = SettingKeys.VisionStatement, SettingValue = "To become the most trusted and preferred online pharmacy, known for quality, affordability, and customer care.", SettingGroup = SettingGroups.About, Description = "Company vision statement", SettingType = "Text" },

                // Footer Settings
                new() { SettingKey = SettingKeys.FooterAbout, SettingValue = "Your trusted online pharmacy for quality medicines and healthcare products. We deliver genuine medicines right to your doorstep.", SettingGroup = SettingGroups.Footer, Description = "Footer about text", SettingType = "Text" },
                new() { SettingKey = SettingKeys.CopyrightText, SettingValue = "© 2026 MedPharma Store. All rights reserved.", SettingGroup = SettingGroups.Footer, Description = "Copyright text", SettingType = "Text" },

                // SEO Settings
                new() { SettingKey = SettingKeys.MetaTitle, SettingValue = "MedPharma Store - Online Pharmacy | Buy Medicines Online", SettingGroup = SettingGroups.SEO, Description = "Default meta title for SEO", SettingType = "Text" },
                new() { SettingKey = SettingKeys.MetaDescription, SettingValue = "Buy genuine medicines online from MedPharma Store. We offer a wide range of prescription and OTC medicines with doorstep delivery.", SettingGroup = SettingGroups.SEO, Description = "Default meta description", SettingType = "Text" },
                new() { SettingKey = SettingKeys.MetaKeywords, SettingValue = "online pharmacy, buy medicines online, medical store, healthcare products, prescription medicines", SettingGroup = SettingGroups.SEO, Description = "Default meta keywords", SettingType = "Text" },

                // E-commerce Settings
                new() { SettingKey = SettingKeys.DiscountThreshold, SettingValue = "500", SettingGroup = SettingGroups.Ecommerce, Description = "Minimum order amount for discount", SettingType = "Text" },
                new() { SettingKey = SettingKeys.DiscountPercent, SettingValue = "5", SettingGroup = SettingGroups.Ecommerce, Description = "Discount percentage for orders above threshold", SettingType = "Text" },
                new() { SettingKey = SettingKeys.MinOrderAmount, SettingValue = "100", SettingGroup = SettingGroups.Ecommerce, Description = "Minimum order amount required", SettingType = "Text" },
                new() { SettingKey = SettingKeys.FreeShippingThreshold, SettingValue = "500", SettingGroup = SettingGroups.Ecommerce, Description = "Order amount for free shipping", SettingType = "Text" },
                new() { SettingKey = SettingKeys.ShippingCharges, SettingValue = "40", SettingGroup = SettingGroups.Ecommerce, Description = "Shipping charges below free shipping threshold", SettingType = "Text" },

                // Homepage Settings
                new() { SettingKey = SettingKeys.HeroBannerTitle, SettingValue = "Your Trusted Online Pharmacy", SettingGroup = SettingGroups.Homepage, Description = "Hero banner main title", SettingType = "Text" },
                new() { SettingKey = SettingKeys.HeroBannerSubtitle, SettingValue = "Quality medicines delivered to your doorstep", SettingGroup = SettingGroups.Homepage, Description = "Hero banner subtitle", SettingType = "Text" },
                new() { SettingKey = SettingKeys.HeroBannerImage, SettingValue = "/images/hero-banner.jpg", SettingGroup = SettingGroups.Homepage, Description = "Hero banner background image", SettingType = "Image" },
                new() { SettingKey = SettingKeys.ShowFeaturedProducts, SettingValue = "true", SettingGroup = SettingGroups.Homepage, Description = "Show featured products section", SettingType = "Boolean" },
                new() { SettingKey = SettingKeys.ShowLatestArticles, SettingValue = "true", SettingGroup = SettingGroups.Homepage, Description = "Show latest articles section", SettingType = "Boolean" },

                // Notification Settings
                new() { SettingKey = SettingKeys.EnableEmailNotifications, SettingValue = "true", SettingGroup = SettingGroups.Notifications, Description = "Enable email notifications", SettingType = "Boolean" },
                new() { SettingKey = SettingKeys.EnableSMSNotifications, SettingValue = "false", SettingGroup = SettingGroups.Notifications, Description = "Enable SMS notifications", SettingType = "Boolean" },
                new() { SettingKey = SettingKeys.AdminNotificationEmail, SettingValue = "admin@medpharma.com", SettingGroup = SettingGroups.Notifications, Description = "Admin email for notifications", SettingType = "Text" },
            };

            await _context.AppSettings.AddRangeAsync(defaultSettings);
            await _context.SaveChangesAsync();
            ClearCache();

            _logger.LogInformation("Initialized {Count} default settings", defaultSettings.Count);
        }
    }
}
