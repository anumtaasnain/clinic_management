using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic_Management.Models
{
    public class MedicineStockReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BatchId { get; set; }

        [Required]
        public int MedicineId { get; set; }

        [Required]
        public TransactionTypeEnum TransactionType { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [ForeignKey("MedicineId")]
        public Medicine Medicine { get; set; }

        [ForeignKey("BatchId")]
        public Batch Batch { get; set; }

        public enum TransactionTypeEnum
        {
            In,
            Out
        }
    }
}
