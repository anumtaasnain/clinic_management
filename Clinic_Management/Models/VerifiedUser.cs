using System.ComponentModel.DataAnnotations;

namespace Clinic_Management.Models
{
    public class VerifiedUser
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OTP { get; set; }
        public _PasswordReset PasswordReset { get; set; } = _PasswordReset.No;
        public string? MessageSid { get; set; } = null;
        public _Verified Verified { get; set; }
        public DateTime VerifiedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public enum _Verified
        {
            No,
            Yes
        }
        public enum _PasswordReset
        {
            No,
            Yes
        }
        public User? User { get; set; }
    }
}
