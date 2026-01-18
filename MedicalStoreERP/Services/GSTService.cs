using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class GSTService : IGSTService
    {
        public decimal CalculateGST(decimal amount, decimal gstPercent)
        {
            // GST is inclusive, so extract GST from the amount
            // For inclusive: GST Amount = (Amount * GST Rate) / (100 + GST Rate)
            return Math.Round(amount * gstPercent / (100 + gstPercent), 2);
        }

        public decimal CalculateCGST(decimal amount, decimal gstPercent)
        {
            var totalGST = CalculateGST(amount, gstPercent);
            return Math.Round(totalGST / 2, 2);
        }

        public decimal CalculateSGST(decimal amount, decimal gstPercent)
        {
            var totalGST = CalculateGST(amount, gstPercent);
            return Math.Round(totalGST / 2, 2);
        }

        public decimal CalculateDiscount(decimal amount, decimal discountPercent)
        {
            return Math.Round(amount * discountPercent / 100, 2);
        }

        public CartViewModel ApplyDiscountAndGST(CartViewModel cart, decimal discountThreshold, decimal discountPercent)
        {
            // Calculate subtotal
            cart.SubTotal = cart.Items.Sum(i => i.TotalPrice);

            // Calculate GST for each item
            decimal totalGST = 0;
            foreach (var item in cart.Items)
            {
                item.GSTAmount = CalculateGST(item.TotalPrice, item.GSTPercent);
                totalGST += item.GSTAmount;
            }

            cart.TotalGSTAmount = totalGST;
            cart.CGSTAmount = Math.Round(totalGST / 2, 2);
            cart.SGSTAmount = Math.Round(totalGST / 2, 2);

            // Apply discount if eligible
            if (cart.SubTotal >= discountThreshold)
            {
                cart.DiscountAmount = CalculateDiscount(cart.SubTotal, discountPercent);
                cart.DiscountPercent = discountPercent;
                cart.IsDiscountApplied = true;
                cart.ShippingCharges = 0; // Free shipping on orders above threshold
            }
            else
            {
                cart.DiscountAmount = 0;
                cart.IsDiscountApplied = false;
                cart.ShippingCharges = 40; // Standard shipping charge
            }

            cart.TotalAmount = cart.SubTotal + cart.TotalGSTAmount - cart.DiscountAmount + cart.ShippingCharges;

            return cart;
        }
    }
}
