using Clinic_Management.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Appointments
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("PatientUser")]
    public int? PatientId { get; set; }

    [ForeignKey("DoctorUser")]
    public int? DoctorId { get; set; }

    public int? DateId { get; set; }
    public _Status Status { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime Created_at { get; set; } = DateTime.Now;

    [DataType(DataType.DateTime)]
    public DateTime Updated_at { get; set; } = DateTime.Now;

    [Required]
    [NotMapped]
    public string? DoctorName { get; set; }

    [Required]
    [NotMapped]
    public string? Date { get; set; }

    [Required]
    [NotMapped]
    public string? StartTime { get; set; }

    [Required]
    [NotMapped]
    public string? EndTime { get; set; }

    public User? DoctorUser { get; set; }
    public User? PatientUser { get; set; }
    public DoctorTimeSlot? TimeSlot { get; set; }

    public enum _Status
    {
        NotApprove,
        Scheduled,
        Cancelled,
        Completed
    }
}
