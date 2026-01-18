using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalStoreERP.Models
{
    public class AppSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? SettingValue { get; set; }

        [StringLength(50)]
        public string? SettingGroup { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string SettingType { get; set; } = "Text"; // Text, Html, Image, Color, Json, Boolean

        public bool IsEditable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedByUserId { get; set; }

        [ForeignKey("UpdatedByUserId")]
        public virtual ApplicationUser? UpdatedByUser { get; set; }
    }

    // Static class to hold setting keys as constants
    public static class SettingKeys
    {
        // General Settings
        public const string AppName = "AppName";
        public const string AppTagline = "AppTagline";
        public const string AppDescription = "AppDescription";
        public const string AppLogo = "AppLogo";
        public const string AppFavicon = "AppFavicon";
        public const string AppLogoAlt = "AppLogoAlt";

        // Theme/Colors
        public const string PrimaryColor = "PrimaryColor";
        public const string SecondaryColor = "SecondaryColor";
        public const string AccentColor = "AccentColor";
        public const string HeaderBgColor = "HeaderBgColor";
        public const string FooterBgColor = "FooterBgColor";
        public const string FooterTextColor = "FooterTextColor";

        // Contact Information
        public const string ContactEmail = "ContactEmail";
        public const string ContactPhone = "ContactPhone";
        public const string ContactPhone2 = "ContactPhone2";
        public const string ContactAddress = "ContactAddress";
        public const string ContactCity = "ContactCity";
        public const string ContactState = "ContactState";
        public const string ContactPinCode = "ContactPinCode";
        public const string ContactCountry = "ContactCountry";
        public const string SupportHours = "SupportHours";

        // Business Information
        public const string GSTIN = "GSTIN";
        public const string DrugLicenseNo = "DrugLicenseNo";
        public const string FSSAINo = "FSSAINo";
        public const string BusinessHours = "BusinessHours";

        // Map Configuration
        public const string StoreLatitude = "StoreLatitude";
        public const string StoreLongitude = "StoreLongitude";
        public const string MaxDeliveryRadiusKm = "MaxDeliveryRadiusKm";
        public const string MapZoomLevel = "MapZoomLevel";
        public const string EnableSameDayDelivery = "EnableSameDayDelivery";
        public const string SameDayDeliveryCutoffHour = "SameDayDeliveryCutoffHour";

        // Social Media Links
        public const string FacebookUrl = "FacebookUrl";
        public const string TwitterUrl = "TwitterUrl";
        public const string InstagramUrl = "InstagramUrl";
        public const string LinkedInUrl = "LinkedInUrl";
        public const string YouTubeUrl = "YouTubeUrl";
        public const string PinterestUrl = "PinterestUrl";

        // WhatsApp Configuration
        public const string WhatsAppNumber = "WhatsAppNumber";
        public const string WhatsAppMessage = "WhatsAppMessage";
        public const string WhatsAppEnabled = "WhatsAppEnabled";
        public const string WhatsAppAdminNumber = "WhatsAppAdminNumber";
        public const string WhatsAppButtonText = "WhatsAppButtonText";
        public const string WhatsAppWelcomeMessage = "WhatsAppWelcomeMessage";
        public const string WhatsAppDefaultMessage = "WhatsAppDefaultMessage";

        // About Section
        public const string AboutTitle = "AboutTitle";
        public const string AboutContent = "AboutContent";
        public const string AboutImage = "AboutImage";
        public const string MissionStatement = "MissionStatement";
        public const string VisionStatement = "VisionStatement";

        // Footer Settings
        public const string FooterAbout = "FooterAbout";
        public const string CopyrightText = "CopyrightText";

        // SEO Settings
        public const string MetaTitle = "MetaTitle";
        public const string MetaDescription = "MetaDescription";
        public const string MetaKeywords = "MetaKeywords";

        // E-commerce Settings
        public const string DiscountThreshold = "DiscountThreshold";
        public const string DiscountPercent = "DiscountPercent";
        public const string MinOrderAmount = "MinOrderAmount";
        public const string FreeShippingThreshold = "FreeShippingThreshold";
        public const string ShippingCharges = "ShippingCharges";

        // Notification Settings
        public const string EnableEmailNotifications = "EnableEmailNotifications";
        public const string EnableSMSNotifications = "EnableSMSNotifications";
        public const string AdminNotificationEmail = "AdminNotificationEmail";

        // Homepage Settings
        public const string HeroBannerTitle = "HeroBannerTitle";
        public const string HeroBannerSubtitle = "HeroBannerSubtitle";
        public const string HeroBannerImage = "HeroBannerImage";
        public const string ShowFeaturedProducts = "ShowFeaturedProducts";
        public const string ShowLatestArticles = "ShowLatestArticles";
    }

    // Grouping for settings
    public static class SettingGroups
    {
        public const string General = "General";
        public const string Theme = "Theme";
        public const string Contact = "Contact";
        public const string Business = "Business";
        public const string Map = "Map";
        public const string SocialMedia = "SocialMedia";
        public const string WhatsApp = "WhatsApp";
        public const string About = "About";
        public const string Footer = "Footer";
        public const string SEO = "SEO";
        public const string Ecommerce = "Ecommerce";
        public const string Notifications = "Notifications";
        public const string Homepage = "Homepage";
    }
}
