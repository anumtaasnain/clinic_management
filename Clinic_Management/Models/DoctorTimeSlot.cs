using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management.Models
{
    public class DoctorTimeSlot
    {
        [Key]
        public int Id { get; set; }

        public int DoctorId { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }
        public int Status { get; set; } = 0;

        public User Doctor { get; set; }
        public List<Appointments> Appointments { get; set; }
    }
}
