using Azure.Core;
using Clinic_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Stripe;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.HttpResults;
using Clinic_Management.Migrations;
using System.Net.Mail;
using System.Net;
using static Clinic_Management.Models.User;
using System.Reflection.PortableExecutable;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Identity;
using Stripe.Climate;
using System.Text;
using Clinic_Management.Filters;

namespace Clinic_Management.Controllers
{
    [CookiesFilter]
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;
        private readonly myDbContext myDbContext;
        private readonly StripeSettings _stripeSettings;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly EmailSettings emailSettings;
        private readonly GoogleReCAPTCHA googleReCAPTCHA;

        public HomeController(
            ILogger<HomeController> logger,
            myDbContext myDbContext,
            IOptions<StripeSettings> stripeSettings,
            IWebHostEnvironment webHostEnvironment,
            IOptions<EmailSettings> emailSettings,
            IOptions<GoogleReCAPTCHA> googleReCAPTCHA
            )
        {
            _logger = logger;
            this.myDbContext = myDbContext;
            _stripeSettings = stripeSettings.Value;
            this.webHostEnvironment = webHostEnvironment;
            this.emailSettings = emailSettings.Value;
            this.googleReCAPTCHA = googleReCAPTCHA.Value;
        }
        //[Authorize]

        public async Task<IActionResult> Index()
        {
            //var cookies = HttpContext.Request.Cookies;
            //if (HttpContext.Request.Cookies.Keys.Contains("id"))
            //{
            //    var user = await myDbContext.Users.FindAsync(Convert.ToInt32(cookies["id"]));
            //    HttpContext.Session.SetString("id", cookies["id"]);
            //    HttpContext.Session.SetString("name", cookies["name"]);
            //    HttpContext.Session.SetString("email", cookies["email"]);
            //    HttpContext.Session.SetString("role", cookies["role"]);
            //    HttpContext.Session.SetString("image", cookies["image"]);
            //    HttpContext.Session.SetString("password", user.Password);
            //    if (cookies["role"] == "3")
            //    {
            //        HttpContext.Session.SetString("gender", cookies["gender"]);
            //        HttpContext.Session.SetString("medicalhistory", cookies["medicalhistory"]);
            //    }
            //    if (cookies["role"] == "0" || cookies["role"] == "1" || cookies["role"] == "3")
            //    {
            //        HttpContext.Session.SetString("phone", cookies["phone"]);
            //        HttpContext.Session.SetString("address", cookies["address"]);
            //    }
            //    else
            //    {
            //        HttpContext.Session.SetString("staff_role", cookies["staff_role"]);
            //    }
            //}
            var verifyUser = await myDbContext.VerifiedUsers.Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id"))).FirstOrDefaultAsync();

            if (verifyUser != null)
            {
                HttpContext.Session.SetString("confirmotp", "done");
            }
            ViewData["CurrentAction"] = "Index";
            ViewData["CurrentController"] = "Home";
            ViewBag.doctors = await myDbContext.Users.Where(x => x.Staff_Role == StaffRole.Doctor).ToListAsync();

            var machines = await myDbContext.MedicalInstruments
                .Include(x => x.Category)
                .OrderByDescending(x => x.Id)
                .Take(2)
                .ToListAsync();

            var machines2 = await myDbContext.MedicalInstruments
                .Include(x => x.Category)
                .OrderByDescending(x => x.Id)
                .Skip(2)
                .Take(2)
                .ToListAsync();

            var doctors = await myDbContext.Users
                .Where(x => x.Staff_Role == StaffRole.Doctor)
                .Take(5)
                .ToListAsync();


            //return Json(doctors);

            ViewBag.machines = machines;

            ViewBag.machines2 = machines2;

            ViewBag.doctordetails = doctors;
            //if (HttpContext.Session.GetString("user_id") != null)
            //{

            //var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            //ViewBag.ip = ip;
            return View();

            //}

            //return RedirectToAction("Login", "User");
        }

        public async Task<IActionResult> Medicines()
        {
            ViewData["CurrentAction"] = "Medicines";
            ViewData["CurrentController"] = "Home";
            var medicines = await myDbContext.Medicines
                .Include(x => x.Category)
                .Include(x => x.Batches)
                .Select(m => new MedicineViewModel
                {
                    MedicineId = m.Id,
                    MedicineName = m.MedicineName,
                    CategoryName = m.Category.Name,
                    UnitPrice = m.UnitPrice,
                    Image = m.Image,
                    BatchId = m.Batches.OrderBy(b => b.ManufacturingDate)
                                           .Select(b => b.Id)
                                           .FirstOrDefault(),
                    BatchNumber = m.Batches.OrderBy(b => b.ManufacturingDate)
                                           .Select(b => b.BatchNumber)
                                           .FirstOrDefault(),
                    StockQuantity = m.Batches.OrderBy(b => b.ManufacturingDate)
                                             .Select(b => b.StockQuantity)
                                             .FirstOrDefault(),
                    ManufacturingDate = m.Batches.OrderBy(b => b.ManufacturingDate)
                                                 .Select(b => b.ManufacturingDate)
                                                 .FirstOrDefault(),
                    ExpiryDate = m.Batches.OrderBy(b => b.ManufacturingDate)
                                          .Select(b => b.ExpiryDate)
                                          .FirstOrDefault()
                })
                .Where(m => m.BatchNumber != null)
                .GroupBy(m => m.MedicineName)
                .Select(g => g.First())
                .ToListAsync();

            //return Ok(medicines);

            return View(medicines);
        }

        public async Task<IActionResult> MedicineDetail(int? id)
        {
            if (id == null)
            {
                return NotFound("Id Not Found");
            }
            var reviews = await myDbContext.Reviews
                .Include(x => x.User)
                .Where(x => x.ProductId == id && x.ProductType == "Medicine")
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            var medicine = await myDbContext.Medicines
                .Include(x => x.Category)
                .Include(x => x.Batches)
                .Where(m => m.Id == id)
                .Select(m => new MedicineViewModel
                {
                    MedicineId = m.Id,
                    MedicineName = m.MedicineName,
                    CategoryName = m.Category.Name,
                    UnitPrice = m.UnitPrice,
                    Image = m.Image,
                    BatchId = m.Batches.OrderBy(b => b.ManufacturingDate)
                                           .Select(b => b.Id)
                                           .FirstOrDefault(),
                    BatchNumber = m.Batches.OrderBy(b => b.ManufacturingDate)
                                           .Select(b => b.BatchNumber)
                                           .FirstOrDefault(),
                    StockQuantity = m.Batches.OrderBy(b => b.ManufacturingDate)
                                             .Select(b => b.StockQuantity)
                                             .FirstOrDefault(),
                    ManufacturingDate = m.Batches.OrderBy(b => b.ManufacturingDate)
                                                 .Select(b => b.ManufacturingDate)
                                                 .FirstOrDefault(),
                    ExpiryDate = m.Batches.OrderBy(b => b.ManufacturingDate)
                                          .Select(b => b.ExpiryDate)
                                          .FirstOrDefault(),
                    Reviews = reviews

                })
                .FirstOrDefaultAsync();


            var completedOrder = myDbContext.OrderItems
                .Include(x => x.Order)
                .Where(x => x.Order.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.Order.PaymentStatus == "Paid" && x.ProductId == id && x.ProductType == "Medicine")
                .Count();

            var submitReview = myDbContext.Reviews
                .Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.ProductId == id)
                .Count();

            var pendingOrder = myDbContext.OrderItems
                .Include(x => x.Order)
                .Where(x => x.Order.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.Order.PaymentStatus == "Pending" && x.ProductId == id && x.ProductType == "Medicine")
                .Count();

            var averageRating = myDbContext.Reviews
                .Where(r => r.ProductId == id && r.ProductType == "Medicine")
                .Select(r => (double?)r.Rating)
                .Average() ?? 0;

            ViewBag.AverageRating = Math.Round(averageRating, 1);

            ViewBag.completedOrder = completedOrder;

            ViewBag.submitReview = submitReview;

            ViewBag.pendingOrder = pendingOrder;

            //return Json(submitReview);


            if (medicine == null)
            {
                return NotFound();
            }

            //return Ok(medicine);

            return View(medicine);
        }

        [AuthenticationFilter]
        public async Task<IActionResult> CartItems()
        {
            var carts = await myDbContext.Carts
                        .Include(x => x.User)
                        .Include(x => x.Medicine)
                        .ThenInclude(x => x.Batches)
                        .Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.ProductType == "Medicine")
                        .ToListAsync();

            var cart2 = await myDbContext.Carts.Include(x => x.User)
                        .Include(x => x.MedicalInstrument)
                        .Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.ProductType == "MedicalInstrument")
                        .ToListAsync();


            ViewBag.cart2 = cart2;
            return View(carts);
        }

        [ValidateAntiForgeryToken]
        [AuthenticationFilter]
        [HttpPost]
        public async Task<IActionResult> AddCart(int? MedicineId, string ProductType)
        {
            if (MedicineId == null)
            {
                return NotFound("Id Not Found");
            }

            if (ProductType == "Medicine")
            {
                var medicine = await myDbContext.Medicines.FindAsync(MedicineId);
                if (medicine == null)
                {
                    return NotFound("Medicine Not Found");
                }
            }
            else if (ProductType == "MedicalInstrument")
            {
                var medicalInstrument = await myDbContext.MedicalInstruments.FindAsync(MedicineId);
                if (medicalInstrument == null)
                {
                    return NotFound("Medical Instrument Not Found");
                }
            }
            else
            {
                return BadRequest("Invalid Product Type");
            }

            var cart = await myDbContext.Carts
                .Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.ProductId == MedicineId && x.ProductType == ProductType)
                .FirstOrDefaultAsync();

            if (cart != null)
            {
                return RedirectToAction("CartItems");
            }

            await myDbContext.Carts.AddAsync(new Cart
            {
                UserId = Convert.ToInt32(HttpContext.Session.GetString("id")),
                ProductId = Convert.ToInt32(MedicineId),
                Quantity = 1,
                ProductType = ProductType
            });

            await myDbContext.SaveChangesAsync();
            return RedirectToAction("CartItems");
        }

        [AuthenticationFilter]
        public async Task<JsonResult> QuantityUpdate(int? cartId, int? quantity, string productType)
        {
            //return Json(productType);
            if (cartId == null)
            {
                return Json(new { success = false, message = "Id Not Found" });
            }
            var cart = await myDbContext.Carts.FindAsync(cartId);
            if (cart == null)
            {
                return Json(new { success = false, message = "Cart Not Found" });
            }
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Quantity must be greater than zero" });
            }
            if (quantity == null)
            {
                return Json(new { success = false, message = "Quantity can not be null" });
            }
            if (productType == "Medicine")
            {
                var batch = await myDbContext.Batches.Where(x => x.MedicineId == cart.ProductId).FirstOrDefaultAsync();
                if (batch == null)
                {
                    return Json(new { success = false, message = "Batch Not Found" });
                }
                if (quantity > batch.StockQuantity)
                {
                    return Json(new { success = false, message = "Requested quantity exceeds available stock" });
                }
                cart.Quantity = (int)quantity;
                await myDbContext.SaveChangesAsync();
            }
            else
            {
                var machine = await myDbContext.MedicalInstruments.FindAsync(cart.ProductId);
                if (machine == null)
                {
                    return Json(new { success = false, message = "Machine Not Found" });
                }
                if (quantity > machine.StockQuantity)
                {
                    return Json(new { success = false, message = "Requested quantity exceeds available stock" });
                }
                cart.Quantity = (int)quantity;
                await myDbContext.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Quantity Update Successfully" });
        }

        [ValidateAntiForgeryToken]
        [AuthenticationFilter]
        [HttpPost]
        public async Task<JsonResult> RemoveCart(int? cartId)
        {
            if (cartId == null)
            {
                return Json(new { success = false, message = "Id Not Found" });
            }

            var cart = await myDbContext.Carts.FindAsync(cartId);
            if (cart == null)
            {
                return Json(new { success = false, message = "Cart Not Found" });
            }

            myDbContext.Carts.Remove(cart);
            await myDbContext.SaveChangesAsync();
            return Json(new { success = true, message = "Item Remove Successfully" });

        }

        [AuthenticationFilter]
        public async Task<IActionResult> Checkout()
        {
            var cart = await myDbContext.Carts
                .Include(x => x.Medicine)
                .Include(x => x.User)
                .Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.ProductType == "Medicine")
                .ToListAsync();

            var cart2 = await myDbContext.Carts
                .Include(x => x.MedicalInstrument)
                .Include(x => x.User)
                .Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.ProductType == "MedicalInstrument")
                .ToListAsync();
            ViewBag.cart2 = cart2;
            return View(cart);
        }
        public string check()
        {
            var userId = Convert.ToInt32(HttpContext.Session.GetString("id"));
            var cartItems = myDbContext.Carts
                .Include(c => c.Medicine)
                .Include(c => c.MedicalInstrument)
                .Where(c => c.UserId == userId)
                .ToQueryString();
            return cartItems;
            //return 1;
        }

        //[ValidateAntiForgeryToken]
        [AuthenticationFilter]
        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            try
            {
                var userId = Convert.ToInt32(HttpContext.Session.GetString("id"));

                var cartItems = myDbContext.Carts
                    .FromSqlRaw("SELECT * FROM Carts WHERE UserId = {0}", userId)
                    .ToList();

                if (!cartItems.Any())
                {
                    return Json(new { success = false, message = "Cart is empty" });
                }

                decimal grandTotal = 0;
                List<OrderItem> orderItems = new List<OrderItem>();

                foreach (var item in cartItems)
                {
                    decimal itemPrice = 0;
                    string productType = "";

                    if (item.ProductType.ToLower() == "medicine")
                    {
                        var medicine = myDbContext.Medicines
                            .FromSqlRaw("SELECT * FROM Medicines WHERE Id = {0}", item.ProductId)
                            .FirstOrDefault();

                        if (medicine != null)
                        {
                            itemPrice = (decimal)medicine.UnitPrice;
                            productType = "Medicine";
                        }
                    }
                    else if (item.ProductType.ToLower() == "medicalinstrument")
                    {
                        var instrument = myDbContext.MedicalInstruments
                            .FromSqlRaw("SELECT * FROM MedicalInstruments WHERE Id = {0}", item.ProductId)
                            .FirstOrDefault();

                        if (instrument != null)
                        {
                            itemPrice = (decimal)instrument.UnitPrice;
                            productType = "MedicalInstrument";
                        }
                    }

                    grandTotal += itemPrice * item.Quantity;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductType = productType,
                        Quantity = item.Quantity,
                        Price = itemPrice
                    });
                }

                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = grandTotal,
                    OrderDate = DateTime.Now,
                    PaymentStatus = "Paid",
                    OrderItems = orderItems
                };

                myDbContext.Orders.Add(order);
                await myDbContext.SaveChangesAsync();

                List<MedicineStockReport> medicineStockReports = new List<MedicineStockReport>();
                List<MachineStockReport> machineStockReports = new List<MachineStockReport>();

                foreach (var item in cartItems)
                {
                    if (item.ProductType.ToLower() == "medicine")
                    {
                        var batch = myDbContext.Batches
                            .FromSqlRaw("SELECT * FROM Batches WHERE MedicineId = {0}", item.ProductId)
                            .FirstOrDefault();

                        if (batch != null)
                        {
                            batch.StockQuantity -= item.Quantity;

                            medicineStockReports.Add(new MedicineStockReport
                            {
                                BatchId = batch.Id,
                                MedicineId = item.ProductId,
                                TransactionType = MedicineStockReport.TransactionTypeEnum.Out,
                                Quantity = item.Quantity,
                                Date = DateTime.Now
                            });
                        }
                    }
                    else if (item.ProductType.ToLower() == "medicalinstrument")
                    {
                        var machine = myDbContext.MedicalInstruments
                            .FromSqlRaw("SELECT * FROM MedicalInstruments WHERE Id = {0}", item.ProductId)
                            .FirstOrDefault();

                        if (machine != null)
                        {
                            machine.StockQuantity -= item.Quantity;

                            machineStockReports.Add(new MachineStockReport
                            {
                                MachineId = machine.Id,
                                TransactionType = MachineStockReport.TransactionTypeEnum.Out,
                                Quantity = item.Quantity,
                                Date = DateTime.Now
                            });
                        }
                    }
                }

                if (medicineStockReports.Any())
                {
                    myDbContext.MedicineStockReports.AddRange(medicineStockReports);
                }

                if (machineStockReports.Any())
                {
                    myDbContext.MachineStockReports.AddRange(machineStockReports);
                }

                myDbContext.Carts.RemoveRange(cartItems);
                await myDbContext.SaveChangesAsync();


                return Json(new { success = true, message = "Order placed successfully!" });

            }
            catch (StripeException ex)
            {
                return Json(new { success = false, message = ex.StripeError.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Machines()
        {
            ViewData["CurrentAction"] = "Machines";
            ViewData["CurrentController"] = "Home";
            var machines = await myDbContext.MedicalInstruments
                .Include(x => x.Category)
                .ToListAsync();

            return View(machines);
        }

        public async Task<IActionResult> MachinesDetail(int? id)
        {
            if (id == null)
            {
                return NotFound("Id Not Found");
            }

            var machines = await myDbContext.MedicalInstruments.Include(x => x.Category).Where(x => x.Id == id).FirstOrDefaultAsync();


            if (machines == null)
            {
                return NotFound();
            }
            var reviews = await myDbContext.Reviews
                .Include(x => x.User)
                .Where(x => x.ProductId == id && x.ProductType == "MedicalInstrument")
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var completedOrder = myDbContext.OrderItems
                .Include(x => x.Order)
                .Where(x => x.Order.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.Order.PaymentStatus == "Paid" && x.ProductId == id && x.ProductType == "MedicalInstrument")
                .Count();

            var submitReview = myDbContext.Reviews
                .Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.ProductId == id)
                .Count();

            var pendingOrder = myDbContext.OrderItems
                .Include(x => x.Order)
                .Where(x => x.Order.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")) && x.Order.PaymentStatus == "Pending" && x.ProductId == id && x.ProductType == "MedicalInstrument")
                .Count();

            var averageRating = myDbContext.Reviews
                .Where(r => r.ProductId == id && r.ProductType == "MedicalInstrument")
                .Select(r => (double?)r.Rating)
                .Average() ?? 0;

            ViewBag.AverageRating = Math.Round(averageRating, 1);

            ViewBag.reviews = reviews;

            ViewBag.completedOrder = completedOrder;

            ViewBag.submitReview = submitReview;

            ViewBag.pendingOrder = pendingOrder;

            //return Ok(medicine);

            return View(machines);
        }

        public async Task<IActionResult> Seminars()
        {
            ViewData["CurrentAction"] = "Seminars";
            ViewData["CurrentController"] = "Home";
            List<Seminar> seminar = await myDbContext.Seminars
                .OrderByDescending(x => x.Id)
                .Where(x => x.Approve == Seminar._Approve.Yes)
                .ToListAsync();
            return View(seminar);
        }

        public async Task<IActionResult> SeminarDetail(int? id)
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

        [AuthenticationFilter]
        public async Task<IActionResult> Booking(int? id)
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

            SeminarBooking seminarBooking = new SeminarBooking()
            {
                Seminar = seminar,
                Booking = new Booking()
            };
            ViewBag.PublishableKey = _stripeSettings.PublishableKey;
            return View(seminarBooking);
        }
        public IActionResult checkPayment()
        {
            ViewBag.PublishableKey = _stripeSettings.PublishableKey;
            return View();
        }

        public async Task<IActionResult> ShowBookings()
        {
            List<Booking> bookings = await myDbContext.Bookings
                .Include(x => x.Seminar)
                .Include(x => x.User)
                .Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")))
                .ToListAsync();
            return View(bookings);
        }

        [AuthenticationFilter]
        public async Task<IActionResult> AddBookings(int? id)
        {
            if (id == null)
            {
                return NotFound("Id Not Found");
            }
            Seminar seminar = await myDbContext.Seminars
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
            if (seminar == null)
            {
                return NotFound("Seminar Not Found");
            }
            Booking booking = await myDbContext.Bookings
                .Where(x => x.SeminarId == id && x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")))
                .FirstOrDefaultAsync();
            if (booking != null)
            {
                return NotFound("You've already registered for this seminar");
            }
            var smtpClient = new SmtpClient(emailSettings.Host)
            {
                Port = emailSettings.Port,
                Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings.UserName),
                Subject = "Seminar Registration",
                Body = $"Congratulations! Hi, {HttpContext.Session.GetString("name")}, You have successfully registered for the seminar. We look forward to your participation and hope you have an insightful and enriching experience. See you there!\" ??",
                IsBodyHtml = false,
            };

            mailMessage.To.Add(HttpContext.Session.GetString("email"));

            mailMessage.CC.Add(emailSettings.CCEmail);

            await smtpClient.SendMailAsync(mailMessage);
            await myDbContext.Bookings.AddAsync(new Booking { UserId = Convert.ToInt32(HttpContext.Session.GetString("id")), SeminarId = (int)id });
            await myDbContext.SaveChangesAsync();
            TempData["success"] = "Booking Added Successfully";

            return RedirectToAction("ShowBookings");

        }

        //[ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Booking(int? seminarId, int? price)
        {

            if (seminarId == null)
            {
                return Json(new { success = false, error = "Id Not Found" });
            }
            if (price == null)
            {
                return Json(new { success = false, error = "Price can not be null" });
            }
            Seminar seminar = await myDbContext.Seminars.Where(x => x.Id == seminarId).FirstOrDefaultAsync();
            if (seminar == null)
            {
                return Json(new { success = false, error = "Seminar Not Found" });
            }
            Booking booking = await myDbContext.Bookings
                .Where(x => x.SeminarId == seminarId && x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")))
                .FirstOrDefaultAsync();
            if (booking != null)
            {
                return Json(new { success = false, error = "You've already registered for this seminar" });
            }
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Test Product",
                        },
                        UnitAmount = price * 100,
                    },
                    Quantity = 1,
                },
            },
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/Home/AddBookings?id={seminarId}",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/Home/Cancel",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Json(new { id = session.Id });
        }

        public async Task<IActionResult> Doctors()
        {
            var doctors = await myDbContext.Users.Where(x => x.Staff_Role == StaffRole.Doctor).ToListAsync();
            return View(doctors);
        }

        [PatientFilter]
        public async Task<IActionResult> DoctorSlots(int? id)
        {
            if (id != null)
            {
                var slots = await myDbContext.DoctorTimeSlots
                    .Include(x => x.Doctor)
                    .Where(x => x.DoctorId == id && x.Status == 0)
                    .ToListAsync();
                var patients = await myDbContext.Patients
                    .OrderBy(x => x.Id)
                    .ToListAsync();
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
        [PatientFilter]
        [HttpPost]
        public async Task<IActionResult> AddAppointment(Appointments appointments)
        {
            //return Json(appointments);
            //return Json(appointments.DateId);
            if (appointments != null)
            {
                //return Json(slot_status);
                appointments.PatientId = Convert.ToInt32(HttpContext.Session.GetString("id"));
                await myDbContext.Appointments.AddAsync(appointments);
                var slot_status = await myDbContext.DoctorTimeSlots
                    .Where(x => x.Id == appointments.DateId)
                    .FirstOrDefaultAsync();
                slot_status.Status = 1;
                await myDbContext.SaveChangesAsync();
                //TempData["success"] = "Appointment Booked Succesfully";
                return RedirectToAction("ShowAppointments");
            }
            return NotFound();
        }

        public async Task<IActionResult> ShowAppointments()
        {
            var appointments = await myDbContext.Appointments
                .Include(x => x.DoctorUser)
                .Include(x => x.TimeSlot)
                .Include(x => x.PatientUser)
                .Where(x => x.PatientUser.Id == Convert.ToInt32(HttpContext.Session.GetString("id")))
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            var users = await myDbContext.Users
                .Where(x => x.Staff_Role == StaffRole.Doctor)
                .ToListAsync();
            var timeslots = await myDbContext.DoctorTimeSlots
                .ToListAsync();
            Manage_Appointment manage_Appointment = new Manage_Appointment()
            {
                All_Appointment = appointments,
                Users = users,
                TimeSlot = timeslots
            };
            return View(manage_Appointment);
        }

        public async Task<IActionResult> ShowOrders()
        {
            var orders = await myDbContext.Orders
                .Include(x => x.User)
                .Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")))
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> ViewOrder(int? id)
        {
            if (id == null)
            {
                return NotFound("Id Not Found");
            }

            var order = await myDbContext.Orders
                .Include(x => x.User)
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
            {
                return NotFound("Order Not Found");
            }

            List<OrdersManage> ordersManages = new List<OrdersManage>();

            foreach (var item in order.OrderItems)
            {
                string productName = "Unkown";
                string productImage = "default";
                string productType = "unkon";

                if (item.ProductType == "Medicine")
                {
                    var medicine = await myDbContext.Medicines
                        .Where(m => m.Id == item.ProductId)
                        .Select(m => new { m.MedicineName, m.Image })
                        .FirstOrDefaultAsync();

                    if (medicine != null)
                    {
                        productName = medicine.MedicineName;
                        productImage = medicine.Image;
                        productType = "Medicine";
                    }
                }
                else if (item.ProductType == "MedicalInstrument")
                {
                    var instrument = await myDbContext.MedicalInstruments
                        .Where(m => m.Id == item.ProductId)
                        .Select(m => new { m.Name, m.Image })
                        .FirstOrDefaultAsync();

                    if (instrument != null)
                    {
                        productName = instrument.Name;
                        productImage = instrument.Image;
                        productType = "MedicalInstrument";
                    }
                }

                ordersManages.Add(new OrdersManage
                {
                    Name = order.User.Name,
                    Email = order.User.Email,
                    Phone = order.User.Phone,
                    Address = order.User.Address,
                    ProductId = item.ProductId,
                    ProductName = productName,
                    ProductType = productType,
                    Quantity = item.Quantity,
                    Price = (int)item.Price,
                    Image = productImage
                });
            }

            var total = await myDbContext.Orders.Where(o => o.Id == id).Select(o => o.TotalAmount).FirstOrDefaultAsync();
            ViewBag.total = total;
            //return Ok(ordersManages);
            return View(ordersManages);
        }

        public IActionResult UpdateProfile()
        {
            if (User.Identity.IsAuthenticated)
            {
                string referer = Request.Headers["Referer"];
                string url = string.IsNullOrEmpty(referer) ? "/" : referer;
                TempData["autherror"] = "You are logged in by Google Account";
                return Redirect(url);
            }

            return View();
        }

        [ValidateAntiForgeryToken]
        [AuthenticationFilter]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User user, IFormFile? Image)
        {
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("RecaptchaToken");
            var emailCheck = await myDbContext.Users
                .Where(x => x.Email == user.Email && x.Id != user.Id)
                .FirstOrDefaultAsync();
            var userData = await myDbContext.Users.FindAsync(user.Id);
            if (emailCheck != null)
            {
                TempData["error"] = "Email Already Exists";
                return View(user);
            }
            if (!HttpContext.Session.Keys.Contains("staff_role"))
            {
                if (string.IsNullOrEmpty(user.Phone))
                {
                    ModelState.AddModelError("Phone", "Phone is required");
                }
                if (string.IsNullOrEmpty(user.Address))
                {
                    ModelState.AddModelError("Address", "Address is required");
                }
            }
            if (HttpContext.Session.GetString("role") == "3")
            {
                if (string.IsNullOrEmpty(user.MedicalHistory))
                {
                    ModelState.AddModelError("MedicalHistory", "Medical History is required");
                }
            }
            //return Json(user);
            if (ModelState.IsValid)
            {
                if (Image != null)
                {
                    var NotExtension = Path.GetFileNameWithoutExtension(Image.FileName);
                    var Extension = Path.GetExtension(Image.FileName);

                    var ImageName = NotExtension + DateTime.Now.ToString("yyyyMMddHHmmss") + Extension;

                    var path = Path.Combine(webHostEnvironment.WebRootPath, "UserImages");

                    var ImagePath = Path.Combine(path, ImageName);

                    using (var stream = new FileStream(ImagePath, FileMode.Create))
                    {
                        await Image.CopyToAsync(stream);
                    }

                    userData.Image = ImageName;
                }
                else
                {
                    userData.Image = HttpContext.Session.GetString("image");
                }

                if (user.Password != null)
                {
                    if (user.Password.Length < 6)
                    {
                        TempData["error"] = "Password must be at least 6 characters";
                        return View(user);
                    }
                    //var Password = user.Password;

                    var hash = new PasswordHasher<User>();

                    userData.Password = hash.HashPassword(user, user.Password);
                }
                else
                {

                    userData.Password = userData.Password;
                }

                if (Enum.TryParse(HttpContext.Session.GetString("staff_role"), out StaffRole staffRole))
                {
                    userData.Staff_Role = staffRole;
                }
                else
                {
                    userData.Staff_Role = null;
                }

                userData.Role = Convert.ToInt32(HttpContext.Session.GetString("role"));
                //return Json(user);
                userData.Name = user.Name;
                userData.Email = user.Email;
                userData.Phone = user.Phone;
                userData.Address = user.Address;
                userData.Gender = user.Gender;
                userData.MedicalHistory = user.MedicalHistory;
                //myDbContext.Users.Update(user);
                await myDbContext.SaveChangesAsync();
                if (HttpContext.Request.Cookies.Keys.Contains("id"))
                {
                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(7),
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    };
                    Response.Cookies.Append("id", userData.Id.ToString(), cookieOptions);
                    Response.Cookies.Append("name", userData.Name, cookieOptions);
                    Response.Cookies.Append("email", userData.Email, cookieOptions);
                    Response.Cookies.Append("role", userData.Role.ToString(), cookieOptions);
                    Response.Cookies.Append("image", userData.Image, cookieOptions);
                    if (user.Role == 3)
                    {
                        Response.Cookies.Append("gender", userData.Gender.ToString(), cookieOptions);
                        Response.Cookies.Append("medicalhistory", userData.MedicalHistory, cookieOptions);
                    }
                    if (userData.Role == 0 || userData.Role == 1 || userData.Role == 3)
                    {
                        Response.Cookies.Append("phone", userData.Phone, cookieOptions);
                        Response.Cookies.Append("address", userData.Address, cookieOptions);
                    }
                    else
                    {
                        Response.Cookies.Append("staff_role", userData.Staff_Role.ToString(), cookieOptions);
                    }
                }
                HttpContext.Session.SetString("role", userData.Role.ToString());
                HttpContext.Session.SetString("name", userData.Name);
                HttpContext.Session.SetString("email", userData.Email);
                HttpContext.Session.SetString("image", userData.Image);
                HttpContext.Session.SetString("password", userData.Password);
                if (HttpContext.Session.GetString("role") == "0" || HttpContext.Session.GetString("role") == "3")
                {
                    HttpContext.Session.SetString("phone", userData.Phone);
                    HttpContext.Session.SetString("address", userData.Address);
                }
                else
                {
                    HttpContext.Session.SetString("staff_role", userData.Staff_Role.ToString());
                }

                if (HttpContext.Session.GetString("role") == "3")
                {
                    HttpContext.Session.SetString("gender", userData.Gender.ToString());
                    HttpContext.Session.SetString("medicalhistory", userData.MedicalHistory);
                }
                TempData["success"] = "Your Profile Update Successfully";
                if (HttpContext.Session.GetString("role") == "2" || HttpContext.Session.GetString("role") == "1")
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index");
            }
            //return Json(ModelState);
            return View(user);
        }

        public async Task<JsonResult> SearchMedicine(string search)
        {
            var medicines = await myDbContext.Medicines
                .Include(x => x.Category)
                .Where(x => x.MedicineName.ToLower().Contains(search.ToLower()))
                .Select(x => new MedicineDTO
                {
                    MedicineId = x.Id,
                    MedicineName = x.MedicineName,
                    Image = x.Image,
                    CategoryName = x.Category.Name
                })
                .ToListAsync();

            return Json(medicines);
        }

        [ValidateAntiForgeryToken]
        [AuthenticationFilter]
        [HttpPost]
        public async Task<IActionResult> Feedback(int productId, string productType, int? rating, string comment)
        {
            if (rating == null || comment == null)
            {
                TempData["error"] = "Please fill all fields";
                if (productType == "Medicine")
                {
                    return RedirectToAction("MedicineDetail", new { id = productId });
                }
                else
                {
                    return RedirectToAction("MachinesDetail", new { id = productId });
                }
            }

            await myDbContext.Reviews.AddAsync(new Models.Review
            {
                UserId = Convert.ToInt32(HttpContext.Session.GetString("id")),
                ProductId = productId,
                ProductType = productType,
                Rating = (int)rating,
                Comment = comment
            });
            await myDbContext.SaveChangesAsync();
            TempData["success"] = "Your Review Added Successfully";
            if (productType == "Medicine")
            {
                return RedirectToAction("MedicineDetail", new { id = productId });
            }
            else
            {
                return RedirectToAction("MachinesDetail", new { id = productId });
            }
        }
        [ValidateAntiForgeryToken]
        [AuthenticationFilter]
        [HttpPost]
        public async Task<IActionResult> UpdateReview(int? reviewId, int productId, string productType, string comment)
        {
            var review = await myDbContext.Reviews.FindAsync(reviewId);

            review.Comment = comment;

            await myDbContext.SaveChangesAsync();
            TempData["success"] = "Review Updated Successfully";
            if (productType == "Medicine")
            {
                return RedirectToAction("MedicineDetail", new { id = productId });
            }
            else
            {
                return RedirectToAction("MachinesDetail", new { id = productId });
            }
        }
        [ValidateAntiForgeryToken]
        [AuthenticationFilter]
        [HttpPost]
        public async Task<IActionResult> DeleteReview(int? reviewId, int productId, string productType)
        {
            var review = await myDbContext.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return NotFound("Review Not Found");
            }
            myDbContext.Reviews.Remove(review);
            await myDbContext.SaveChangesAsync();
            TempData["success"] = "Review Deleted Successfully";
            if (productType == "Medicine")
            {
                return RedirectToAction("MedicineDetail", new { id = productId });
            }
            else
            {
                return RedirectToAction("MachinesDetail", new { id = productId });
            }
        }

        public IActionResult About()
        {
            ViewData["name"] = "Rafay";
            ViewData["age"] = 18;
            ViewData["CurrentAction"] = "About";
            ViewData["CurrentController"] = "Home";
            return View();
        }
        public IActionResult Contact()
        {
            ViewData["CurrentAction"] = "Contact";
            ViewData["CurrentController"] = "Home";
            return View();
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Contact(Contact contact, string returnUrlContact)
        {
            if (!string.IsNullOrEmpty(contact.Subject))
            {
                Response.Cookies.Append("subject", contact.Subject);
            }
            if (!string.IsNullOrEmpty(contact.Message))
            {
                Response.Cookies.Append("message", contact.Message);
            }
            if (HttpContext.Session.GetString("id") == null && HttpContext.Request.Cookies["id"] == null)
            {
                TempData["alert"] = "Please Login First";

                if (!string.IsNullOrEmpty(returnUrlContact))
                {
                    return RedirectToAction("Login", "User", new { returnUrl = returnUrlContact });
                }
                else
                {
                    return RedirectToAction("Login", "User", null);
                }
            }
            if (!HttpContext.Session.Keys.Contains("confirmotp"))
            {
                TempData["alert"] = "Please verify your email by OTP, we sent it to your email";
                return RedirectToAction("VerifyOtp", "User");
            }

            if (ModelState.IsValid)
            {
                contact.UserId = Convert.ToInt32(HttpContext.Session.GetString("id"));

                await myDbContext.Contact.AddAsync(contact);
                await myDbContext.SaveChangesAsync();

                Response.Cookies.Delete("subject");
                Response.Cookies.Delete("message");

                TempData["success"] = "Form submitted successfully, We will contact you ASAP";
                
                return RedirectToAction("Index");
            }
            return View();
        }

        //public IActionResult DeleteAccount()
        //{
        //    return View();
        //}

        //[HttpPost]
        //// Try with or without this:
        ////[ValidateAntiForgeryToken] // <- Isko lagao aur hatao dekhne ke liye
        //public async Task<IActionResult> DeleteAccount(int userId)
        //{
        //    // Just for testing
        //    var user = await myDbContext.Users.FindAsync(userId);
        //    myDbContext.Remove(user);
        //    await myDbContext.SaveChangesAsync();
        //    return Content("Account Deleted Successfully");
        //}

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
