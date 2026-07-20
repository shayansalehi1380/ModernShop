using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernShop.Core.Enums
{
    public enum Gender
    {
        NotSpecified,
        Female,
        Male
    }

    public enum OrderStatus
    {
        PendingPayment,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Failed
    }

    public enum PaymentMethod
    {
        Online,
        CashOnDelivery
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed
    }

    public enum DiscountType
    {
        Percent,
        FixedAmount
    }

    public enum LoyaltyTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum
    }

}
