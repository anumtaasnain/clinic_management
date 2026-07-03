namespace Clinic_Management.Models
{
    public class Manage_Appointment
    {
        public Patient? SinglePatient { get; set; }
        public List<User>? Users { get; set; }
        public List<Patient>? Patient { get; set; }
        public List<DoctorTimeSlot>? TimeSlot { get; set; }
        public List<Appointments>? All_Appointment { get; set; }
        public Appointments? Appointments { get; set; }
    }
}
