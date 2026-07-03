using Microsoft.EntityFrameworkCore;

namespace Clinic_Management.Models
{
    public class myDbContext : DbContext
    {
        public myDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<DoctorTimeSlot> DoctorTimeSlots { get; set; }
        public DbSet<Appointments> Appointments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<MedicalInstrument> MedicalInstruments { get; set; }
        public DbSet<MedicineStockReport> MedicineStockReports { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<MachineStockReport> MachineStockReports { get; set; }
        public DbSet<Seminar> Seminars { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Contact> Contact { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<VerifiedUser> VerifiedUsers { get; set; }
        public DbSet<OtpLog> OtpLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DoctorTimeSlot>()
                .HasOne(d => d.Doctor)
                .WithMany(u => u.doctorTimeSlots)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointments>()
                .HasOne(a => a.DoctorUser)
                .WithMany(u => u.DoctorAppointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointments>()
                .HasOne(a => a.PatientUser)
                .WithMany(u => u.PatientAppointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Appointments>()
                .HasOne(a => a.TimeSlot)
                .WithMany(t => t.Appointments)
                .HasForeignKey(a => a.DateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Batch>()
                .HasOne(b => b.Medicine)
                .WithMany(m => m.Batches)
                .HasForeignKey(b => b.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.Category)
                .WithMany(c => c.Medicine)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.Pharmacist)
                .WithMany(u => u.Medicines)
                .HasForeignKey(m => m.AddedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cart>()
                .HasOne(m => m.Medicine)
                .WithMany(u => u.Cart)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Carts_Medicines_ProductId");

            modelBuilder.Entity<Cart>()
                .HasOne(m => m.MedicalInstrument)
                .WithMany(u => u.Cart)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Carts_MedicalInstruments_ProductId");

            modelBuilder.Entity<Cart>()
                .HasOne(m => m.User)
                .WithMany(u => u.Cart)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicalInstrument>()
                .HasOne(m => m.Technician)
                .WithMany(u => u.MedicalInstruments)
                .HasForeignKey(m => m.AddedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicalInstrument>()
                 .HasOne(m => m.Category)
                 .WithMany(c => c.MedicalInstruments)
                 .HasForeignKey(m => m.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicineStockReport>()
                .HasOne(m => m.Medicine)
                .WithMany()
                .HasForeignKey(m => m.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicineStockReport>()
                .HasOne(m => m.Batch)
                .WithMany()
                .HasForeignKey(m => m.BatchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MachineStockReport>()
                .HasOne(m => m.Machine)
                .WithMany(r => r.MachineReport)
                .HasForeignKey(m => m.MachineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(o => o.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(o => o.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Seminar>()
                .HasOne(s => s.Doctor)
                .WithMany(d => d.Seminars)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
                .HasOne(s => s.User)
                .WithMany(d => d.Bookings)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(s => s.Seminar)
                .WithMany(s => s.Bookings)
                .HasForeignKey(s => s.SeminarId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Contact>()
                .HasOne(c => c.User)
                .WithMany(u => u.Contact)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(c => c.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VerifiedUser>()
                .HasOne(c => c.User)
                .WithMany(u => u.VerifiedUser)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OtpLog>()
                .HasOne(c => c.User)
                .WithMany(u => u.OtpLogs)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
