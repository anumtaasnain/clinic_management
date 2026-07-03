using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic_Management.Models
{
    public class Contact
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [NotMapped]
        public string RecaptchaToken { get; set; }
        public User? User { get; set; }
    }
}
