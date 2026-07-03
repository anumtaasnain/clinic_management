namespace Clinic_Management.Models
{
    public class MedicineViewModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string CategoryName { get; set; }
        public double UnitPrice { get; set; }
        public string Image { get; set; }
        public int BatchId { get; set; }
        public string BatchNumber { get; set; }
        public int StockQuantity { get; set; }
        public DateTime ManufacturingDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        public List<Review>? Reviews { get; set; }
    }
    
}
