using Clinic_Management.Migrations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Clinic_Management.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match")]
        [NotMapped]
        public string ConfirmPassword { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        public StaffRole? Staff_Role { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime Created_at { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime Updated_at { get; set; } = DateTime.Now;

        public int Role { get; set; } = 0;
        public string? Image { get; set; } = "user.png";
        public _Gender? Gender { get; set; }
        [MinLength(10, ErrorMessage = "Medical History must be greater than 10 characters")]
        public string? MedicalHistory { get; set; }
        [NotMapped]
        public string RecaptchaToken { get; set; }

        public _Verified Verified { get; set; } = _Verified.No;
        public _Provider Provider { get; set; } = _Provider.Normal;
        public enum _Verified
        {
            No,
            Yes
        }

        public enum StaffRole
        {
            Receptionist,
            Pharmacist,
            Technician,
            Doctor
        }
        public enum _Gender
        {
            Male,
            Female
        }

        public enum _Provider
        {
            Normal,
            Google
        }
        public List<DoctorTimeSlot>? doctorTimeSlots { get; set; }
        public ICollection<Appointments>? DoctorAppointments { get; set; }

        public ICollection<Appointments>? PatientAppointments { get; set; }
        public List<Medicine>? Medicines { get; set; }
        public List<MedicalInstrument>? MedicalInstruments { get; set; }
        public List<Cart>? Cart { get; set; }
        public List<Seminar>? Seminars { get; set; }
        public List<Booking>? Bookings { get; set; }
        public List<Contact>? Contact { get; set; }
        public List<Review>? Reviews { get; set; }
        public List<VerifiedUser>? VerifiedUser { get; set; }
        public List<OtpLog>? OtpLogs { get; set; }
    }
}
