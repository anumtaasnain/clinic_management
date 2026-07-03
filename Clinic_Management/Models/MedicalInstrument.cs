using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Clinic_Management.Models
{
    public class MedicalInstrument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } 
        public int CategoryId { get; set; }

        [Required]
        public double UnitPrice { get; set; } 
        [Required]
        public string Manufacturer { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string ManufacturerEmail { get; set; }
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be a positive value.")]
        public int StockQuantity { get; set; }

        [Required]
        public int ReorderLevel { get; set; }

        [ForeignKey("Technician")]
        public int AddedBy { get; set; }

        public string Description { get; set; }
        [AllowNull]
        public string? Image { get; set; }
        public User? Technician { get; set; }
        public Category? Category { get; set; }
        public List<Cart>? Cart { get; set; }
        public List<MachineStockReport>? MachineReport { get; set; }
    }
}
