using Clinic_Management.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Clinic_Management.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]

    public class AdminController : Controller
    {
        private readonly myDbContext myDbContext;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly EmailSettings emailSettings;
        public AdminController(myDbContext myDbContext, IWebHostEnvironment webHostEnvironment, IOptions<EmailSettings> emailSettings)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.myDbContext = myDbContext;
            this.emailSettings = emailSettings.Value;
        }
        [AdminIndexFilter]
        public async Task<IActionResult> Index()
        {
            // 🏭 **Machine Profit Calculation**
            var lastRecord = myDbContext.MachineStockReports
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

            if (lastRecord == null)
            {
                ViewBag.machineProfit = "No Data Available";
            }
            else
            {
                var lastAvailableMonthStart = new DateTime(lastRecord.Date.Year, lastRecord.Date.Month, 1);
                var lastAvailableMonthEnd = lastAvailableMonthStart.AddMonths(1).AddTicks(-1);

                var lastMonthProfit = myDbContext.MachineStockReports
                    .Where(x => x.Date >= lastAvailableMonthStart && x.Date <= lastAvailableMonthEnd)
                    .Join(
                        myDbContext.MedicalInstruments,
                        stock => stock.MachineId,
                        machine => machine.Id,
                        (stock, machine) => new
                        {
                            stock.MachineId,
                            stock.TransactionType,
                            stock.Quantity,
                            machine.UnitPrice
                        }
                    )
                    .GroupBy(x => x.MachineId)
                    .Select(g => new
                    {
                        MachineId = g.Key,
                        PurchaseAmount = g.Where(x => x.TransactionType == MachineStockReport.TransactionTypeEnum.In)
                                          .Sum(x => x.Quantity * x.UnitPrice),

                        SaleAmount = g.Where(x => x.TransactionType == MachineStockReport.TransactionTypeEnum.Out)
                                      .Sum(x => x.Quantity * x.UnitPrice)
                    })
                    .ToList();

                ViewBag.machineProfit = lastMonthProfit.Sum(x => x.SaleAmount - x.PurchaseAmount);
            }

            // 💊 **Medicine Profit Calculation**
            var lastRecord2 = myDbContext.MedicineStockReports
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

            if (lastRecord2 == null)
            {
                ViewBag.medicineProfit = "No Data Available";
            }
            else
            {
                var lastAvailableMonthStart2 = new DateTime(lastRecord2.Date.Year, lastRecord2.Date.Month, 1);
                var lastAvailableMonthEnd2 = lastAvailableMonthStart2.AddMonths(1).AddTicks(-1);

                var lastMonthProfit2 = myDbContext.MedicineStockReports
                    .Where(x => x.Date >= lastAvailableMonthStart2 && x.Date <= lastAvailableMonthEnd2)
                    .Join(
                        myDbContext.Batches,
                        stock => stock.BatchId,
                        batch => batch.Id,
                        (stock, batch) => new
                        {
                            stock.MedicineId,
                            stock.TransactionType,
                            stock.Quantity,
                            batch.Medicine.UnitPrice  // ✅ Batch se correct price le rahe hain
                        }
                    )
                    .GroupBy(x => x.MedicineId)
                    .Select(g => new
                    {
                        MedicineId = g.Key,
                        PurchaseAmount = g.Where(x => x.TransactionType == MedicineStockReport.TransactionTypeEnum.In)
                                          .Sum(x => x.Quantity * x.UnitPrice),

                        SaleAmount = g.Where(x => x.TransactionType == MedicineStockReport.TransactionTypeEnum.Out)
                                      .Sum(x => x.Quantity * x.UnitPrice)
                    })
                    .ToList();

                ViewBag.medicineProfit = lastMonthProfit2.Sum(x => x.SaleAmount - x.PurchaseAmount);
            }



            var seminarProfit = myDbContext.Bookings
                .GroupBy(b => b.SeminarId)
                .Select(g => new
                {
                    SeminarId = g.Key,
                    TotalBookings = g.Count(),
                    SeminarPrice = myDbContext.Seminars.Where(s => s.Id == g.Key).Select(s => s.Price).FirstOrDefault()
                })
                .Sum(x => x.TotalBookings * x.SeminarPrice);

            ViewBag.seminarProfit = seminarProfit;

            var lastAppointment = await myDbContext.Appointments
                .Where(x => x.Status == Appointments._Status.Scheduled)
                .Include(x => x.PatientUser)
                .OrderByDescending(x => x.Id)
                .Select(x => x.PatientUser.Name)
                .Take(1)
                .FirstOrDefaultAsync();

            ViewBag.lastAppointment = lastAppointment;

            return View();
        }


        [AdminFilter]
        public async Task<IActionResult> Staff()
        {
            var staff = await myDbContext.Users.Where(x => x.Role == 2).ToListAsync();
            return View(staff);
        }
        [AdminFilter]
        public IActionResult AddStaff()
        {
            return View();

        }

        [ValidateAntiForgeryToken]
        [AdminFilter]
        [HttpPost]
        public async Task<IActionResult> AddStaff(User user, IFormFile? Image)
        {
            //return Json(ModelState);
            if (!user.Staff_Role.HasValue || user.Staff_Role < 0)
            {
                ModelState.AddModelError("StaffRole", "Please select a valid Staff Role.");
                TempData["error"] = "Please select a valid Staff Role.";
                //return View(user);
            }
            if (ModelState.IsValid)
            {


                if (Image != null)
                {

                    //return Json(DateTime.Now.ToString("yyyyMMddHHmmss"));

                    var namenotExt = Path.GetFileNameWithoutExtension(Image.FileName);
                    var extension = Path.GetExtension(Image.FileName);
                    //return Json(ImageNamess);

                    var ImageName = namenotExt + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                    var path = Path.Combine(webHostEnvironment.WebRootPath, "UserImages");

                    var ImagePath = Path.Combine(path, ImageName);

                    using (var stream = new FileStream(ImagePath, FileMode.Create))
                    {
                        await Image.CopyToAsync(stream);
                    }

                    user.Image = ImageName;

                }
                else
                {
                    user.Image = "user.png";
                }



                var user_email = await myDbContext.Users.Where(x => x.Email == user.Email).FirstOrDefaultAsync();
                //return Json(user_email);
                if (user_email == null)
                {
                    var NoHash = user.Password;
                    var hash = new PasswordHasher<User>();
                    user.Password = hash.HashPassword(user, user.Password);
                    user.Role = 2;
                    //Image != null ? user.Image = Image.FileName : user.Image = "~/UserImage/User.png";
                    //return Json(user);


                    var smtpClient = new SmtpClient(emailSettings.Host)
                    {
                        Port = emailSettings.Port,
                        Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                        EnableSsl = true,
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(emailSettings.UserName),
                        Subject = "Staff Registration",
                        Body = $"Congratulations! Hi, {HttpContext.Session.GetString("name")}, You have successfully registered, Your Email is {user.Email} and Your Password is {NoHash}",
                        IsBodyHtml = false,
                    };

                    mailMessage.To.Add(user.Email);

                    mailMessage.CC.Add(emailSettings.CCEmail);

                    await smtpClient.SendMailAsync(mailMessage);

                    await myDbContext.Users.AddAsync(user);
                    await myDbContext.SaveChangesAsync();

                    TempData["success"] = "Staff Add Successfully";
                    return RedirectToAction("Staff");
                }
                else
                {
                    TempData["email_err"] = "Email Already Exist";
                    return View(user);
                }
            }
            return View(user);
        }

        [AdminFilter]
        public async Task<JsonResult> ApproveSeminar(int? seminarId, int price)
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

            seminar.Price = price;
            seminar.Approve = Seminar._Approve.Yes;
            await myDbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Seminar Approved Successfully" });

        }

        public async Task<IActionResult> Contact()
        {
            var contact = await myDbContext.Contact
                .OrderByDescending(x => x.Id)
                .Include(x => x.User)
                .ToListAsync();

            return View(contact);
        }
    }
}
