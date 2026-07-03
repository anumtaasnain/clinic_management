using System.ComponentModel.DataAnnotations;

namespace Clinic_Management.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        public List<Medicine>? Medicine { get; set; }
        public List<MedicalInstrument>? MedicalInstruments { get; set; }
    }
}
