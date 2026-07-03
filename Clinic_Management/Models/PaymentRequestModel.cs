namespace Clinic_Management.Models
{
    public class PaymentRequestModel
    {
        public string StripeToken { get; set; }
        public decimal Amount { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Method { get; set; }
    }

}
