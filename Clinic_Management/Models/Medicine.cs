using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Clinic_Management.Models
{
    public class Medicine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Medicine name cannot exceed 100 characters.")]
        public string MedicineName { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Medicine code cannot exceed 50 characters.")]
        public string MedicineCode { get; set; }

        [ForeignKey("Medicine")]
        public int CategoryId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be a positive value.")]
        public double UnitPrice { get; set; }

        [Required]
        public string Manufacturer { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string ManufacturerEmail { get; set; }

        [Required]
        public int ReorderLevel { get; set; }

        [ForeignKey("Pharmacist")]
        public int AddedBy { get; set; }
        [AllowNull]
        public string? Image { get; set; }

        public List<Batch>? Batches { get; set; }
        public User? Pharmacist { get; set; }
        public Category? Category { get; set; }
        public List<Cart>? Cart { get; set; }

    }
}
