using ZBilet.Domain.Entities;

namespace ZBilet.UI.Models
{
    public class PaymentViewModel
    {
        //Layout
        public List<Category> Categories { get; set; }
        public List<Subcategory> Subcategories { get; set; }
        public List<Corporate> Corporates { get; set; }
        public Setting Setting { get; set; }

        public Ticket? Ticket { get; set; }
        public Price? Price { get; set; }
        public Event? Event { get; set; }

        public int? UserId { get; set; }
        public string? CardNumber { get; set; }
        public string? ExpirationMonth { get; set; }
        public string? ExpirationYear { get; set; }
        public string? Cvv { get; set; }
        public string? Amount { get; set; }

        public string Status { get; set; }
        public string MerchantId { get; set; }
        public string VerifyEnrollmentRequestId { get; set; }
        public string Xid { get; set; }
        public string PurchAmount { get; set; }
        public string SessionInfo { get; set; }
        public string PurchCurrency { get; set; }
        public string Pan { get; set; }
        public string ExpiryDate { get; set; }
        public string Eci { get; set; }
        public string Cavv { get; set; }
        public string InstallmentCount { get; set; }
    }
}
