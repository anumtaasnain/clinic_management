using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic_Management.Models
{
    public class Batch
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Medicine")]
        public int MedicineId { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters.")]
        public string BatchNumber { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be a positive value.")]
        public int StockQuantity { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ManufacturingDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }
        public int? Active { get; set; } = 1;

        public Medicine? Medicine { get; set; }
    }
}
