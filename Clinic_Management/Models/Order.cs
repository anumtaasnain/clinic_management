using Clinic_Management.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Required]
    public decimal TotalAmount { get; set; }

    [Required]
    public string PaymentStatus { get; set; } = "Pending";

    [Required]
    public string OrderStatus { get; set; } = "Pending";

    [ForeignKey("UserId")]
    public User? User { get; set; }

    public List<OrderItem>? OrderItems { get; set; }
}
