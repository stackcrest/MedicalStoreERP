namespace MedicalStoreERP.Models.ViewModels
{
    public class AppSettingsViewModel
    {
        // General
        public string AppName { get; set; } = "MedPharma Store";
        public string AppTagline { get; set; } = "Your Health, Our Priority";
        public string? AppDescription { get; set; }
        public string AppLogo { get; set; } = "/images/logo.png";
        public string AppFavicon { get; set; } = "/favicon.ico";

        // Theme
        public string PrimaryColor { get; set; } = "#0d6efd";
        public string SecondaryColor { get; set; } = "#6c757d";
        public string AccentColor { get; set; } = "#198754";
        public string HeaderBgColor { get; set; } = "#ffffff";
        public string FooterBgColor { get; set; } = "#212529";
        public string FooterTextColor { get; set; } = "#adb5bd";

        // Contact
        public string ContactEmail { get; set; } = "contact@medpharma.com";
        public string ContactPhone { get; set; } = "+91-9876543210";
        public string? ContactPhone2 { get; set; }
        public string ContactAddress { get; set; } = "123 Health Street";
        public string ContactCity { get; set; } = "Mumbai";
        public string ContactState { get; set; } = "Maharashtra";
        public string ContactPinCode { get; set; } = "400001";
        public string ContactCountry { get; set; } = "India";
        public string SupportHours { get; set; } = "24/7 Support Available";

        public string FullAddress => $"{ContactAddress}, {ContactCity}, {ContactState} - {ContactPinCode}, {ContactCountry}";
        public string Address => FullAddress;
        public string Phone => ContactPhone;

        // Business
        public string? GSTIN { get; set; }
        public string? DrugLicenseNo { get; set; }
        public string? FSSAINo { get; set; }
        public string? BusinessHours { get; set; }

        // Map
        public double StoreLatitude { get; set; } = 19.0760;
        public double StoreLongitude { get; set; } = 72.8777;
        public double MaxDeliveryRadiusKm { get; set; } = 10;
        public int MapZoomLevel { get; set; } = 13;
        public bool EnableSameDayDelivery { get; set; } = true;
        public int SameDayDeliveryCutoffHour { get; set; } = 14;

        // Social Media
        public string? FacebookUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? YouTubeUrl { get; set; }
        public string? YoutubeUrl => YouTubeUrl; // Alias for case variation

        public bool HasSocialMedia => !string.IsNullOrEmpty(FacebookUrl) || 
                                       !string.IsNullOrEmpty(TwitterUrl) || 
                                       !string.IsNullOrEmpty(InstagramUrl) ||
                                       !string.IsNullOrEmpty(LinkedInUrl) ||
                                       !string.IsNullOrEmpty(YouTubeUrl);

        // WhatsApp
        public string WhatsAppNumber { get; set; } = "919876543210";
        public string WhatsAppMessage { get; set; } = "Hello! I need help with my order.";
        public bool WhatsAppEnabled { get; set; } = true;
        public string WhatsAppButtonText { get; set; } = "Need Help?";
        public string WhatsAppWelcomeMessage { get; set; } = "Chat with our support team on WhatsApp for quick assistance!";
        public string WhatsAppDefaultMessage { get; set; } = "Hi! I'm {userName} and I need assistance.\n\nPage: {page}\n\nMy Query: ";

        public string WhatsAppUrl => $"https://wa.me/{WhatsAppNumber}?text={Uri.EscapeDataString(WhatsAppMessage)}";

        // About
        public string AboutTitle { get; set; } = "About Us";
        public string? AboutContent { get; set; }
        public string? AboutImage { get; set; }
        public string? MissionStatement { get; set; }
        public string? VisionStatement { get; set; }

        // Footer
        public string? FooterAbout { get; set; }
        public string CopyrightText { get; set; } = "© 2026 MedPharma Store. All rights reserved.";

        // SEO
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }

        // E-commerce
        public decimal DiscountThreshold { get; set; } = 500;
        public decimal DiscountPercent { get; set; } = 5;
        public decimal MinOrderAmount { get; set; } = 0;
        public decimal FreeShippingThreshold { get; set; } = 0;

        // Homepage
        public string HeroBannerTitle { get; set; } = "Your Trusted Online Pharmacy";
        public string HeroBannerSubtitle { get; set; } = "Quality medicines delivered to your doorstep";
        public string? HeroBannerImage { get; set; }
        public bool ShowFeaturedProducts { get; set; } = true;
        public bool ShowLatestArticles { get; set; } = true;
    }

    public class SettingsEditViewModel
    {
        public string Group { get; set; } = string.Empty;
        public string GroupDisplayName { get; set; } = string.Empty;
        public List<SettingItemViewModel> Settings { get; set; } = new();
    }

    public class SettingItemViewModel
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Description { get; set; }
        public string SettingType { get; set; } = "Text";
        public string Group { get; set; } = string.Empty;
        public bool IsEditable { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }
    }

    public class BulkSettingsUpdateModel
    {
        public List<SettingUpdateItem> Settings { get; set; } = new();
    }

    public class SettingUpdateItem
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
