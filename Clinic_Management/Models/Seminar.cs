using Clinic_Management.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

public class Seminar
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    [Required] 
    public string Description { get; set; }

    public int DoctorId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime DateTime { get; set; }
  
    public int Price { get; set; } = 0;
    public _Approve Approve { get; set; }
    
    public _Registration Registration { get; set; }
    public string? Image { get; set; }

    public User? Doctor { get; set; }
    public List<Booking>? Bookings { get; set; }

    public enum _Approve
    {
        No,
        Yes
    }
    public enum _Registration
    {
        Close,
        Open
    }
}
