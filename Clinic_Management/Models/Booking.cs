using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Clinic_Management.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }
        public int SeminarId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        [JsonIgnore]
        public Seminar Seminar { get; set; }
        public User User { get; set; }
    }
}
