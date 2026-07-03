using Clinic_Management.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IO;

namespace Clinic_Management.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [DoctorFilter]
    public class DoctorController : Controller
    {
        private readonly myDbContext myDbContext;
        private readonly IWebHostEnvironment webHostEnvironment;
        public DoctorController(myDbContext myDbContext, IWebHostEnvironment webHostEnvironment)
        {
            this.myDbContext = myDbContext;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> ShowSlots()
        {
            List<DateTime> slots;
            if (HttpContext.Session.GetString("role") == "1")
            {
                slots = await myDbContext.DoctorTimeSlots
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Date)
                    .Distinct()
                    .ToListAsync();

            }
            else
            {

                slots = await myDbContext.DoctorTimeSlots
                    .Where(x => x.DoctorId == Convert.ToInt32(HttpContext.Session.GetString("id")))
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Date)
                    .Distinct()
                    .ToListAsync();
            }
            return View(slots);
        }
        public IActionResult AddSlots()
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult AddSlots(DateTime startDate, DateTime endDate, string startTime, string endTime)
        {
            TimeSpan salonOpenTime = TimeSpan.Parse("09:00:00");
            TimeSpan salonCloseTime = TimeSpan.Parse("23:00:00");

            TimeSpan start = TimeSpan.Parse(startTime);
            TimeSpan end = TimeSpan.Parse(endTime);

            if (start < salonOpenTime || end > salonCloseTime)
            {
                TempData["error"] = "Error: Time must be between 9:00 AM and 11:00 PM.";
                return View();
            }
            else if (start >= end)
            {
                TempData["error"] = "Error: Start time must be earlier than end time.";
                return View();
            }
            var doctor_id = HttpContext.Session.GetString("id");

            var overlappingSlots = myDbContext.DoctorTimeSlots
                .Where(slot => slot.DoctorId == Convert.ToInt32(HttpContext.Session.GetString("id")) &&
                               slot.Date >= startDate &&
                               slot.Date <= endDate)
                .Any();

            if (overlappingSlots)
            {
                TempData["error"] = "Error: Selected dates overlap with existing availability. Please choose a different range.";
                return View();
            }

            DateTime currentDate = startDate;

            while (currentDate <= endDate)
            {
                int dayOfWeek = (int)currentDate.DayOfWeek;

                if (dayOfWeek == 6 || dayOfWeek == 0)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                TimeSpan currentStart = start;

                while (currentStart < end)
                {
                    TimeSpan slotEnd = currentStart.Add(TimeSpan.FromHours(1));

                    var newSlot = new DoctorTimeSlot
                    {
                        DoctorId = Convert.ToInt32(HttpContext.Session.GetString("id")),
                        Date = currentDate,
                        StartTime = currentStart,
                        EndTime = slotEnd,
                        Status = 0
                    };

                    myDbContext.DoctorTimeSlots.Add(newSlot);

                    currentStart = slotEnd;
                }

                currentDate = currentDate.AddDays(1);
            }

            myDbContext.SaveChanges();

            TempData["success"] = "Availability slots have been set successfully, excluding weekends!";
            return RedirectToAction("ShowSlots");
        }

        public async Task<IActionResult> EditSlot(int id, DateTime date)
        {
            //return Json(new { id = id, date = date });
            var times = await myDbContext.DoctorTimeSlots.Where(x => x.DoctorId == id && x.Date == date).OrderBy(x => x.Id).ToListAsync();
            //return Json(id);
            //return Json(times);
            return View(times);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> RemoveSlot(int slot_id)
        {
            var slot = await myDbContext.DoctorTimeSlots.FindAsync(slot_id);
            myDbContext.Remove(slot);
            await myDbContext.SaveChangesAsync();
            return Json(new { success = "Slot Removed Successfully" });
        }

        //[HttpPost]
        //public async Task<IActionResult> EditSlots(List<int> SlotIds, List<string> StartTime, List<string> EndTime, List<string> NewStartTime, List<string> NewEndTime)
        //{
        //    //if (string.IsNullOrEmpty(List<NewStartTime>) || string.IsNullOrEmpty(NewEndTime))
        //    //{
        //    var slotsData = SlotIds.Select((slotId, index) => new
        //    {
        //        slotid = slotId,
        //        starttime = TimeSpan.Parse(StartTime[index]),
        //        endtime = TimeSpan.Parse(EndTime[index]),
        //        newstarttime = TimeSpan.Parse(NewStartTime[index]),
        //        newendtime = TimeSpan.Parse(NewEndTime[index])
        //    });

        //    //return Json(slotsData);
        //    //string arr = "";
        //    foreach (var slotid in SlotIds)
        //    {
        //        int index = SlotIds.IndexOf(slotid);
        //        var starttime = TimeSpan.Parse(StartTime[index]);
        //        var endtime = TimeSpan.Parse(EndTime[index]);

        //        await myDbContext.DoctorTimeSlots.Update();
        //    }
        //    //}


        //    //var newstarttime = TimeSpan.Parse(NewStartTime[index]);
        //    //var newendtime = TimeSpan.Parse(NewEndTime[index]);
        //    //return BadRequest("New start time aur end time required hain.");
        //}
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditSlots(List<int> SlotIds, List<string> StartTime, List<string> EndTime, List<string> NewStartTime, List<string> NewEndTime, DateTime SlotDate)
        {
            //return Json(SlotDate);
            for (int i = 0; i < SlotIds.Count; i++)
            {
                //return Json(SlotIds.Count);
                var slotid = SlotIds[i];
                var starttime = TimeSpan.Parse(StartTime[i]);
                var endtime = TimeSpan.Parse(EndTime[i]);

                var slot = await myDbContext.DoctorTimeSlots
                    .FirstOrDefaultAsync(s => s.Id == slotid);

                if (slot != null)
                {
                    slot.StartTime = starttime;
                    slot.EndTime = endtime;
                    TempData["success"] = "Slots Edit Successfully";
                }
            }

            await myDbContext.SaveChangesAsync();

            if (NewStartTime != null && NewEndTime != null)
            {
                foreach (var NewSlots in NewStartTime)
                {
                    int index = NewStartTime.IndexOf(NewSlots);
                    var NewStartTimes = TimeSpan.Parse(NewStartTime[index]);
                    var NewEndTimes = TimeSpan.Parse(NewEndTime[index]);

                    await myDbContext.DoctorTimeSlots.AddAsync(new DoctorTimeSlot { DoctorId = Convert.ToInt32(HttpContext.Session.GetString("id")), Date = SlotDate, StartTime = NewStartTimes, EndTime = NewEndTimes, Status = 0 });
                    await myDbContext.SaveChangesAsync();
                    TempData["success"] = "Slots Added/Updated Successfully";
                }
            }

            return RedirectToAction("ShowSlots");
        }

        public async Task<IActionResult> ShowSeminars()
        {
            List<Seminar> Seminars;
            if (Convert.ToInt32(HttpContext.Session.GetString("role")) == 1)
            {

                Seminars = await myDbContext.Seminars
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();
            }
            else
            {
                Seminars = await myDbContext.Seminars
                    .OrderByDescending(x => x.Id)
                    .Where(x => x.DoctorId == Convert.ToInt32(HttpContext.Session.GetString("id")))
                    .ToListAsync();
            }


            //return Json(Seminars);
            return View(Seminars);
        }

        public IActionResult AddSeminar()
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddSeminar([Bind("Id,Title,Description,DateTime")] Seminar seminar, IFormFile Image)
        {
            //return Json(Image);
            if (!ModelState.IsValid)
            {
                return View(seminar);
            }

            var NotExtension = Path.GetFileNameWithoutExtension(Image.FileName);
            var Extension = Path.GetExtension(Image.FileName);

            var ImageName = NotExtension + DateTime.Now.ToString("yyyyMMddHHmmss") + Extension;

            var path = Path.Combine(webHostEnvironment.WebRootPath, "SeminarImages");

            var ImagePath = Path.Combine(path, ImageName);

            using (var stream = new FileStream(ImagePath, FileMode.Create))
            {
                await Image.CopyToAsync(stream);
            }

            seminar.Image = ImageName;
            seminar.DoctorId = Convert.ToInt32(HttpContext.Session.GetString("id"));
            seminar.Price = 0;
            seminar.Approve = Seminar._Approve.No;
            seminar.Registration = Seminar._Registration.Open;

            await myDbContext.Seminars.AddAsync(seminar);
            await myDbContext.SaveChangesAsync();

            TempData["success"] = "Seminar Add Successfully! Wait for Admin approve";
            return RedirectToAction("ShowSeminars");

        }

        public async Task<JsonResult> CloseRegistration(int? seminarId)
        {
            if (seminarId == null)
            {
                return Json(new { success = false, message = "Id not found" });
            }

            Seminar seminar = await myDbContext.Seminars.FindAsync(seminarId);

            if (seminar == null)
            {
                return Json(new { success = false, message = "Seminar not found" });
            }

            seminar.Registration = Seminar._Registration.Close;
            await myDbContext.SaveChangesAsync();
            return Json(new { success = true, message = "Registraion Closed Successfully" });
        }
        public async Task<JsonResult> OpenRegistration(int? seminarId)
        {
            if (seminarId == null)
            {
                return Json(new { success = false, message = "Id not found" });
            }

            Seminar seminar = await myDbContext.Seminars.FindAsync(seminarId);

            if (seminar == null)
            {
                return Json(new { success = false, message = "Seminar not found" });
            }

            seminar.Registration = Seminar._Registration.Open;
            await myDbContext.SaveChangesAsync();
            return Json(new { success = true, message = "Registraion Open Successfully" });
        }

        public async Task<IActionResult> EditSeminar(int? id)
        {
            if (id == null)
            {
                return NotFound("Id Not Found");
            }

            Seminar seminar = await myDbContext.Seminars.FindAsync(id);

            if (seminar == null)
            {
                return NotFound("Seminar Not Found");
            }
            return View(seminar);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditSeminar([Bind("Id,Title,Description,DateTime")] Seminar seminar, IFormFile Image)
        {
            //return Json(Image);
            ModelState.Remove("Image");

            if (!ModelState.IsValid)
            {
                return View(seminar);
            }

            var seminarData = await myDbContext.Seminars.FindAsync(seminar.Id);
            if (seminarData != null)
            {
                seminarData.Title = seminar.Title;
                seminarData.Description = seminar.Description;
                seminarData.DateTime = seminar.DateTime;

            }
            if (Image != null)
            {
                var NotExtension = Path.GetFileNameWithoutExtension(Image.FileName);
                var Extension = Path.GetExtension(Image.FileName);
                var ImageName = NotExtension + DateTime.Now.ToString("yyyyMMddHHmmss") + Extension;
                var path = Path.Combine(webHostEnvironment.WebRootPath, "SeminarImages");
                var ImagePath = Path.Combine(path, ImageName);

                using (var stream = new FileStream(ImagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(stream);
                }

                seminarData.Image = ImageName;
            }
            else
            {
                seminarData.Image = seminarData.Image;
            }
            await myDbContext.SaveChangesAsync();

            TempData["success"] = "Seminar Update Successfully";
            return RedirectToAction("ShowSeminars");
        }

        public async Task<IActionResult> DeleteSeminar(int? id)
        {
            if (id == null)
            {
                return NotFound("Id Not Found");
            }

            var hasBookings = myDbContext.Bookings.Any(b => b.SeminarId == id);

            if (hasBookings)
            {
                TempData["error"] = "You cannot delete this seminar because it has bookings.";
                return RedirectToAction("ShowSeminars");
            }

            var seminar = await myDbContext.Seminars.FindAsync(id);

            if (seminar == null)
            {
                return NotFound("Seminar Not Found");
            }

            myDbContext.Seminars.Remove(seminar);
            await myDbContext.SaveChangesAsync();

            return RedirectToAction("ShowSeminars");
        }

        public async Task<JsonResult> ViewParticipants(int? seminarId)
        {
            if (seminarId == null)
            {
                return Json(new { success = false, message = "Id Not Found" });
            }

            Seminar seminar = await myDbContext.Seminars.FindAsync(seminarId);

            if (seminar == null)
            {
                return Json(new { success = false, message = "Seminar Not Found" });
            }

            var bookings = await myDbContext.Bookings
                .Where(x => x.SeminarId == seminarId)
                .Include(x => x.User)
                .Select(x => new
                {
                    x.Id,
                    x.SeminarId,
                    x.UserId,
                    x.CreatedAt,
                    UserName = x.User.Name,
                    UserEmail = x.User.Email,
                    UserPhone = x.User.Phone
                })
                .ToListAsync();

            if (!bookings.Any())
            {
                return Json(new { success = true, message = "No Booking Found" });
            }

            return Json(new { success = true, message = "Bookings", data = bookings });
        }

        public async Task<IActionResult> SeminarReport()
        {
            var lastMonth = DateTime.Now.AddMonths(-1);

            var profitReport = await myDbContext.Bookings
                .Where(b => b.CreatedAt >= lastMonth)
                .GroupBy(b => new { b.Seminar.Id, b.Seminar.Title, b.Seminar.Price })
                .Select(g => new
                {
                    SeminarName = g.Key.Title,
                    Price = g.Key.Price,
                    TotalBookings = g.Count(),
                    TotalEarnings = g.Count() * g.Key.Price
                })
                .OrderByDescending(x => x.TotalEarnings)
                .ToListAsync();

            ViewBag.report = profitReport;

            return View();
        }
    }
}
