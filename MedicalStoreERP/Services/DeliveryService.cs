using Microsoft.Extensions.Options;

namespace MedicalStoreERP.Services
{
    public class StoreLocationSettings
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double MaxDeliveryRadiusKm { get; set; } = 10;
        public string StoreName { get; set; } = string.Empty;
        public string StoreCity { get; set; } = string.Empty;
        public bool EnableSameDayDelivery { get; set; } = true;
        public int SameDayDeliveryCutoffHour { get; set; } = 14; // 2 PM
    }

    public class DeliveryCheckResult
    {
        public bool IsDeliverable { get; set; }
        public double DistanceKm { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool SameDayDeliveryAvailable { get; set; }
        public string EstimatedDeliveryTime { get; set; } = string.Empty;
        public DateTime? EstimatedDeliveryDate { get; set; }
    }

    public interface IDeliveryService
    {
        DeliveryCheckResult CheckDeliveryAvailability(double customerLatitude, double customerLongitude);
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
        StoreLocationSettings GetStoreLocation();
    }

    public class DeliveryService : IDeliveryService
    {
        private readonly StoreLocationSettings _storeLocation;
        private readonly ILogger<DeliveryService> _logger;

        public DeliveryService(IOptions<StoreLocationSettings> storeLocation, ILogger<DeliveryService> logger)
        {
            _storeLocation = storeLocation.Value;
            _logger = logger;
        }

        public StoreLocationSettings GetStoreLocation()
        {
            return _storeLocation;
        }

        public DeliveryCheckResult CheckDeliveryAvailability(double customerLatitude, double customerLongitude)
        {
            var result = new DeliveryCheckResult();

            // Calculate distance from store to customer
            var distance = CalculateDistance(
                _storeLocation.Latitude,
                _storeLocation.Longitude,
                customerLatitude,
                customerLongitude
            );

            result.DistanceKm = Math.Round(distance, 2);

            _logger.LogInformation(
                "Delivery check: Customer location ({CustLat}, {CustLon}) is {Distance} km from store",
                customerLatitude, customerLongitude, result.DistanceKm);

            if (distance <= _storeLocation.MaxDeliveryRadiusKm)
            {
                result.IsDeliverable = true;
                
                // Check for same day delivery
                var now = DateTime.Now;
                if (_storeLocation.EnableSameDayDelivery && now.Hour < _storeLocation.SameDayDeliveryCutoffHour)
                {
                    result.SameDayDeliveryAvailable = true;
                    result.EstimatedDeliveryDate = now.Date;
                    result.EstimatedDeliveryTime = "Today, within 4-6 hours";
                    result.Message = $"Great news! You're within our {_storeLocation.MaxDeliveryRadiusKm}km delivery zone. Same-day delivery is available!";
                }
                else
                {
                    result.SameDayDeliveryAvailable = false;
                    result.EstimatedDeliveryDate = now.Date.AddDays(1);
                    result.EstimatedDeliveryTime = "Tomorrow, within 4-6 hours";
                    result.Message = $"You're within our {_storeLocation.MaxDeliveryRadiusKm}km delivery zone. Your order will be delivered tomorrow.";
                }

                // Adjust delivery time based on distance
                if (distance <= 3)
                {
                    result.EstimatedDeliveryTime = result.SameDayDeliveryAvailable 
                        ? "Today, within 2-3 hours" 
                        : "Tomorrow, within 2-3 hours";
                }
                else if (distance <= 5)
                {
                    result.EstimatedDeliveryTime = result.SameDayDeliveryAvailable 
                        ? "Today, within 3-4 hours" 
                        : "Tomorrow, within 3-4 hours";
                }
            }
            else
            {
                result.IsDeliverable = false;
                result.SameDayDeliveryAvailable = false;
                result.Message = $"We're sorry! Your location is {result.DistanceKm} km away from our store. " +
                    $"Currently, we deliver only within {_storeLocation.MaxDeliveryRadiusKm} km radius. " +
                    "We're expanding soon and will notify you when delivery becomes available in your area!";
            }

            return result;
        }

        /// <summary>
        /// Calculate distance between two geographic coordinates using Haversine formula
        /// Returns distance in kilometers
        /// </summary>
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusKm = 6371.0;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var lat1Rad = DegreesToRadians(lat1);
            var lat2Rad = DegreesToRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1Rad) * Math.Cos(lat2Rad);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
