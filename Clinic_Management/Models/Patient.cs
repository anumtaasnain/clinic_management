using System.ComponentModel.DataAnnotations;

namespace Clinic_Management.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
        public string Name { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; }

        [Required]
        public string Contact { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string Address { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Dob { get; set; }

        [Required]
        public _Gender Gender { get; set; }

        [Required]
        public string Medical_History { get; set; }


        [DataType(DataType.DateTime)]
        public DateTime Created_at { get; set; } = DateTime.Now;
        public List<Appointments>? Appointments { get; set; }

        public enum _Gender
        {
            Male,
            Female,
            Other
        }
    }
}
