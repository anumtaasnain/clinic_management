using System.ComponentModel.DataAnnotations;

namespace Clinic_Management.Models
{
    public class OtpLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string PhoneNumber { get; set; }

        public int Otp { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        public string? MessageSid { get; set; } = null;

        public User? User { get; set; }
    }

}
