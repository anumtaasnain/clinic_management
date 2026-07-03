using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management.Models
{
    public class MachineStockReport
    {
        [Key]
        public int Id { get; set; }


        [Required]
        public int MachineId { get; set; }

        [Required]
        public TransactionTypeEnum TransactionType { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [ForeignKey("MachineId")]
        public MedicalInstrument Machine { get; set; }

        public enum TransactionTypeEnum
        {
            In,
            Out
        }
    }
}
