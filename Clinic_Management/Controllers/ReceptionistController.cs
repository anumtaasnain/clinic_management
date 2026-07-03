using Clinic_Management.Migrations;
using Clinic_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using static Clinic_Management.Models.User;
using Microsoft.Extensions.Options;

namespace Clinic_Management.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ReceptionistFilter]
    public class ReceptionistController : Controller
    {
        private readonly myDbContext myDbContext;
        private readonly EmailSettings emailSettings;

        public ReceptionistController(myDbContext myDbContext, IOptions<EmailSettings> emailSettings)
        {
            this.myDbContext = myDbContext;
            this.emailSettings = emailSettings.Value;
        }

        public async Task<ViewResult> Patient()
        {
            var patient = await myDbContext.Users.Where(x => x.Role == 3).ToListAsync();
            return View(patient);
        }
        public async Task<IActionResult> AddPatient()
        {
            var doctors = await myDbContext.Users.Where(x => x.Staff_Role == StaffRole.Doctor).ToListAsync();
            Manage_Appointment manage_Appointment = new Manage_Appointment()
            {
                Users = doctors,
                SinglePatient = new Patient()
            };
            //return Json(doctors);
            return View(manage_Appointment);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddPatient(Manage_Appointment manage_appointment)
        {
            //return Json(new
            //{
            //    success = false,
            //    data = manage_appointment.SinglePatient
            //});
            //if (ModelState.IsValid)
            //{
            await myDbContext.Patients.AddAsync(manage_appointment.SinglePatient);
            await myDbContext.SaveChangesAsync();

            return Json(new
            {
                success = true,
                patientId = manage_appointment.SinglePatient.Id,
                patientName = manage_appointment.SinglePatient.Name
            });
            //}
            //else
            //{
            //    return Json(new
            //    {
            //        success = false,
            //        error = "Invalid data. Please check the form fields.",
            //        data = manage_appointment.SinglePatient
            //    });
            //}
        }

        public async Task<IActionResult> EditPatient(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var patient = await myDbContext.Patients.FindAsync(id);

            return View(patient);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditPatient(Patient patient)
        {
            //return Json(patient);
            if (patient != null)
            {
                if (ModelState.IsValid)
                {
                    myDbContext.Patients.Update(patient);
                    await myDbContext.SaveChangesAsync();
                    return RedirectToAction("Patient");

                }
                else
                {
                    return View(patient);
                }
            }
            return NotFound();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> DeletePatient(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await myDbContext.Patients.FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            myDbContext.Patients.Remove(student);
            await myDbContext.SaveChangesAsync();

            return RedirectToAction("Patient");
        }

        public async Task<JsonResult> getTime2(DateTime date, int doctor_id)
        {
            var times = await myDbContext.DoctorTimeSlots
                                          .Where(x => x.Date == date && x.DoctorId == doctor_id && x.Status == 0)
                                          .Select(x => new
                                          {
                                              id = x.Id,
                                              startTime = x.StartTime,
                                              endTime = x.EndTime
                                          })
                                          .ToListAsync();

            return Json(times);
        }

        public async Task<JsonResult> getDate(int doctorId)
        {
            var date = await myDbContext.DoctorTimeSlots.Where(x => x.DoctorId == doctorId && x.Status == 0).ToListAsync();

            return Json(date);
        }
        public async Task<JsonResult> getTime(int dateId)
        {
            var times = await myDbContext.DoctorTimeSlots
                                          .Where(x => x.Id == dateId)
                                          .Select(x => new
                                          {
                                              id = x.Id,
                                              startTime = x.StartTime,
                                              endTime = x.EndTime
                                          })
                                          .ToListAsync();

            return Json(times);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> NewAppointment(Appointments appointment)
        {
            //return Json(appointment);
            try
            {
                await myDbContext.Appointments.AddAsync(new Appointments { PatientId = appointment.PatientId, DoctorId = Convert.ToInt32(appointment.DoctorName), DateId = appointment.DateId });
                var slot_status = await myDbContext.DoctorTimeSlots.Where(x => x.Id == appointment.DateId).FirstOrDefaultAsync();
                slot_status.Status = 1;
                await myDbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }



        public async Task<IActionResult> ShowDoctors()
        {
            var doctors = await myDbContext.Users.Where(x => x.Staff_Role == StaffRole.Doctor).ToListAsync();
            return View(doctors);
        }

        public async Task<IActionResult> ShowSlots(int? id)
        {

            if (id != null)
            {

                var slots = await myDbContext.DoctorTimeSlots.Include(x => x.Doctor).Where(x => x.DoctorId == id).ToListAsync();
                var patients = await myDbContext.Patients.OrderBy(x => x.Id).ToListAsync();
                Manage_Appointment manage_Appointment = new Manage_Appointment()
                {
                    TimeSlot = slots,
                    Patient = patients,
                    Appointments = new Appointments()

                };
                //manage_Appointment.TimeSlot = slots;
                //manage_Appointment.Patient = patients;
                return View(manage_Appointment);
            }
            return NotFound();
            //return id;

            //return Json(slots);

        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddAppointment(Appointments appointments)
        {
            //return Json(appointments.DateId);
            if (appointments != null)
            {
                //return Json(slot_status);
                await myDbContext.Appointments.AddAsync(appointments);
                var slot_status = await myDbContext.DoctorTimeSlots.Where(x => x.Id == appointments.DateId).FirstOrDefaultAsync();
                slot_status.Status = 1;
                await myDbContext.SaveChangesAsync();
                //TempData["success"] = "Appointment Booked Succesfully";
                return RedirectToAction("ShowAppointments");
            }
            return NotFound();
        }

        public async Task<IActionResult> AddLinkAppointment()
        {
            var patients = await myDbContext.Patients.OrderByDescending(x => x.Id).ToListAsync();
            var doctor = await myDbContext.Users.Where(x => x.Staff_Role == StaffRole.Doctor).ToListAsync();
            Manage_Appointment manage_Appointment = new Manage_Appointment()
            {
                Patient = patients,
                Users = doctor
            };
            return View(manage_Appointment);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddLinkAppointment(Appointments appointments)
        {
            await myDbContext.Appointments.AddAsync(appointments);
            var slot_status = await myDbContext.DoctorTimeSlots.Where(x => x.Id == appointments.DateId).FirstOrDefaultAsync();
            slot_status.Status = 1;
            await myDbContext.SaveChangesAsync();
            return RedirectToAction("ShowAppointments");
        }

        public async Task<JsonResult> getDate2(int doctorId)
        {
            var date = await myDbContext.DoctorTimeSlots.Where(x => x.DoctorId == doctorId && x.Status == 0).Select(x => x.Date).Distinct().ToListAsync();

            return Json(date);
        }

        public async Task<IActionResult> ShowAppointments()
        {
            var appointments = await myDbContext.Appointments.Include(x => x.DoctorUser).Include(x => x.TimeSlot).Include(x => x.PatientUser).OrderByDescending(x => x.Id).ToListAsync();
            var users = await myDbContext.Users.Where(x => x.Staff_Role == StaffRole.Doctor).ToListAsync();
            var timeslots = await myDbContext.DoctorTimeSlots.ToListAsync();
            List<Appointments> all_appointments;
            if (appointments == null)
            {
                all_appointments = new List<Appointments>();
            }
            else
            {
                all_appointments = appointments;
            }
            Manage_Appointment manage_Appointment = new Manage_Appointment()
            {

                All_Appointment = all_appointments,
                Users = users,
                TimeSlot = timeslots
            };
            return View(manage_Appointment);
        }

        [HttpGet]
        public JsonResult GetDoctorDates(int doctorId)
        {
            var availableDates = myDbContext.Appointments
                                .Where(a => a.DoctorId == doctorId)
                                .Select(a => a.TimeSlot.Date)
                                .ToList();

            return Json(availableDates);
        }

        //[HttpGet]
        //public JsonResult GetAvailableTimes(int doctorId, string date)
        //{
        //    // Convert date string to DateTime object
        //    DateTime selectedDate = DateTime.Parse(date);

        //    // Get all booked time slots for this doctor on the selected date
        //    var bookedSlots = myDbContext.Appointments
        //        .Where(a => a.DoctorId == doctorId && a.TimeSlot.Date == selectedDate)
        //        .Select(a => a.TimeSlot.StartTime)
        //        .ToList();

        //    // Get all available time slots for the doctor from predefined schedule
        //    var allSlots = myDbContext.DoctorTimeSlots
        //        .Where(t => t.DoctorId == doctorId && t.Date == selectedDate)
        //        .Select(t => new
        //        {
        //            StartTime = t.StartTime,
        //            EndTime = t.EndTime
        //        })
        //        .ToList();

        //    // Filter out booked slots
        //    var availableSlots = allSlots
        //        .Where(slot => !bookedSlots.Contains(slot.StartTime))
        //        .Select(slot => $"{slot.StartTime} - {slot.EndTime}")
        //        .ToList();

        //    return Json(availableSlots);
        //}

        public async Task<JsonResult> GetAppointmentTimes(DateTime date, int patientId)
        {
            var appointment = await myDbContext.Appointments
                .Include(a => a.TimeSlot)
                .FirstOrDefaultAsync(a => a.Id == patientId);

            var availableTimes = await myDbContext.DoctorTimeSlots
                .Where(t => t.Date == date && (t.Status == 0 ||
                       (appointment != null && t.StartTime == appointment.TimeSlot.StartTime && t.EndTime == appointment.TimeSlot.EndTime)))
                .Select(t => new
                {
                    Value = t.Id,
                    Text = $"{t.StartTime} - {t.EndTime}",
                    Selected = appointment != null && t.StartTime == appointment.TimeSlot.StartTime && t.EndTime == appointment.TimeSlot.EndTime
                })
                .ToListAsync();

            return Json(availableTimes);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> UpdateAppointment(int patientId, int dateId, int time)
        {

            var appointment = myDbContext.Appointments.Find(patientId);
            //return Json(appointment.PatientUser);
            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found!" });
            }

            if (appointment.DateId == null)
            {
                return Json(new { success = false, message = "TimeSlot is null for the appointment!" });
            }
            var oldTimeSlot = myDbContext.DoctorTimeSlots
                .Find(appointment.DateId);


            if (oldTimeSlot != null)
            {
                oldTimeSlot.Status = 0;
                myDbContext.SaveChanges();
            }

            var newTimeSlot = myDbContext.DoctorTimeSlots
                .FirstOrDefault(t => t.Id == time);

            if (newTimeSlot != null)
            {
                newTimeSlot.Status = 1;
                appointment.DateId = newTimeSlot.Id;

                myDbContext.SaveChanges();
                var smtpClient = new SmtpClient(emailSettings.Host)
                {
                    Port = emailSettings.Port,
                    Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailSettings.UserName),
                    Subject = "Reschedule Appointment",
                    Body = $"Dear Patient, <br/><br/>Your appointment has been rescheduled from <b>{oldTimeSlot.Date + oldTimeSlot.StartTime + oldTimeSlot.EndTime}</b> to <b>{newTimeSlot.Date + newTimeSlot.StartTime + newTimeSlot.EndTime}</b>. <br/><br/>Thank you.",
                    IsBodyHtml = false,
                };
                var userEmail = await myDbContext.Users.FindAsync(patientId);

                mailMessage.To.Add(userEmail.Email);

                mailMessage.CC.Add(emailSettings.CCEmail);

                await smtpClient.SendMailAsync(mailMessage);
                return Json(new { success = true, message = "Appointment updated successfully!" });
            }
            else
            {
                return Json(new { success = false, message = "Time slot not found!" });
            }

        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> CancelAppointment(int appointmentId)
        {
            Appointments appointment = await myDbContext.Appointments.FindAsync(appointmentId);
            //return Json(appointment);
            if (appointment != null)
            {
                appointment.Status = Appointments._Status.Cancelled;
                await myDbContext.SaveChangesAsync();
                return Json(new { success = true, message = "Appointment Cancelled Successfully" });
            }
            return Json(new { success = false, data = appointment });
        }

        public async Task<JsonResult> Approve(int? appointmendId)
        {
            if (appointmendId == null)
            {
                return Json(new { success = false, message = "Id Not Found" });
            }

            var appointment = await myDbContext.Appointments.FindAsync(appointmendId);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment Not Found" });
            }

            appointment.Status = Appointments._Status.Scheduled;
            await myDbContext.SaveChangesAsync();

            return Json(new { success = false, message = "Appointment Scheduled Successfully" });
        }

        public async Task<JsonResult> CompleteAppointment(int? appointmentId)
        {
            if (appointmentId == null)
            {
                return Json(new { success = false, message = "Id Not Found" });
            }

            var appointment = await myDbContext.Appointments.FindAsync(appointmentId);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment Not Found" });
            }

            appointment.Status = Appointments._Status.Completed;

            await myDbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Appointment Compeleted Successfully" });
        }
    }
}
