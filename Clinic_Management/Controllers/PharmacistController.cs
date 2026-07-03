using Clinic_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Clinic_Management.Migrations;
using Microsoft.Extensions.Options;

namespace Clinic_Management.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [PharmacistFilter]
    public class PharmacistController : Controller
    {
        private readonly myDbContext myDbContext;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly EmailSettings emailSettings;
        public PharmacistController(myDbContext myDbContext, IWebHostEnvironment webHostEnvironment, IOptions<EmailSettings> emailSettings)
        {
            this.myDbContext = myDbContext;
            this.webHostEnvironment = webHostEnvironment;
            this.emailSettings = emailSettings.Value;
        }
        public async Task<IActionResult> ShowCategory()
        {
            var categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
            return View(categories);
        }
        public IActionResult CreateCategory()
        {
            return View();
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CreateCategory([Bind("Id,Name")] Category category)
        {
            if (ModelState.IsValid)
            {
                var existCategory = await myDbContext.Categories
                    .Where(x => x.Name == category.Name)
                    .FirstOrDefaultAsync();

                if (existCategory != null)
                {
                    TempData["error"] = "Category Already Exists";
                    return View(category);
                }

                await myDbContext.Categories.AddAsync(category);
                await myDbContext.SaveChangesAsync();

                TempData["success"] = "Category Add Successfully";
                return RedirectToAction("ShowCategory");
            }
            return View(category);

        }

        public async Task<IActionResult> EditCategory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await myDbContext.Categories.FindAsync(id);
            return View(category);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditCategory(Category category)
        {
            if (category == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                myDbContext.Categories.Update(category);
                await myDbContext.SaveChangesAsync();

                TempData["success"] = "Category Update Successfully";
                return RedirectToAction("ShowCategory");
            }
            return View(category);
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await myDbContext.Categories.FindAsync(id);
            myDbContext.Categories.Remove(category);
            await myDbContext.SaveChangesAsync();

            TempData["success"] = "Category Delete Successfully";
            return RedirectToAction("ShowCategory");
        }

        public async Task<IActionResult> ShowMedicine()
        {
            var medicines = await myDbContext.Medicines
                .Include(x => x.Pharmacist)
                .Include(x => x.Category)
                .Include(x => x.Batches)
                .ToListAsync();

            var companies = await myDbContext.Medicines.Select(x => x.ManufacturerEmail).Distinct().ToListAsync();
            ViewBag.companies = companies;


            var stockAlert = await myDbContext.Medicines.Include(m => m.Batches).Where(m => m.Batches.Any(b => b.StockQuantity < m.ReorderLevel && b.Active == 1)).ToListAsync();


            //return Json(stockAlert);

            ViewBag.stockAlert = stockAlert;


            return View(medicines);
        }


        public async Task<IActionResult> CreateMedicine()
        {
            var categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
            ViewBag.categories = categories;
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CreateMedicine(MedicineBatchViewModel medicineBatchViewModel, IFormFile Image)
        {
            if (Image == null)
            {
                ViewBag.categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
                TempData["error"] = "Image is required";
                return View();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(Image.FileName).ToLower();

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            var mimeType = Image.ContentType.ToLower();

            if (!allowedExtensions.Contains(fileExtension) || !allowedMimeTypes.Contains(mimeType))
            {
                ViewBag.categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
                TempData["error"] = "Only JPG, PNG, and WEBP formats are allowed!";
                return View();
            }

            var ImageName = Path.GetFileNameWithoutExtension(Image.FileName)
                            + DateTime.Now.ToString("yyyyMMddHHmmss") + fileExtension;

            var path = Path.Combine(webHostEnvironment.WebRootPath, "MedicineImages");
            var ImagePath = Path.Combine(path, ImageName);

            using (var stream = new FileStream(ImagePath, FileMode.Create))
            {
                await Image.CopyToAsync(stream);
            }

            medicineBatchViewModel.Medicine.Image = ImageName;
            medicineBatchViewModel.Medicine.AddedBy = Convert.ToInt32(HttpContext.Session.GetString("id"));

            await myDbContext.Medicines.AddAsync(medicineBatchViewModel.Medicine);
            await myDbContext.SaveChangesAsync();

            medicineBatchViewModel.Batch.MedicineId = medicineBatchViewModel.Medicine.Id;
            await myDbContext.Batches.AddAsync(medicineBatchViewModel.Batch);
            await myDbContext.SaveChangesAsync();

            TempData["success"] = "Medicine Add Successfully";
            return RedirectToAction("ShowMedicine");
        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> SendMail(string medicineName, int quantity, string companyEmail)
        {
            if (companyEmail == null)
            {
                TempData["error"] = "Please Fill All Fields!";
                return RedirectToAction("ShowMachines");
            }
            if (quantity <= 0)
            {
                TempData["error"] = "Quantity must be greater than 0";
                return RedirectToAction("ShowMachines");
            }
            try
            {
                var smtpClient = new SmtpClient(emailSettings.Host)
                {
                    Port = emailSettings.Port,
                    Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailSettings.UserName),
                    Subject = "Medicine Stock Request",
                    Body = $"Dear Supplier,\n\nWe need {quantity} units of {medicineName}.\nPlease confirm the availability.\n\nBest Regards,\nPharmacist",
                    IsBodyHtml = false,
                };

                mailMessage.To.Add(companyEmail);

                mailMessage.CC.Add(emailSettings.CCEmail);

                await smtpClient.SendMailAsync(mailMessage);

                TempData["success"] = "Email Sent Successfully!";
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error sending email: " + ex.Message;
            }

            return RedirectToAction("ShowMedicine");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> SendMachineMail(string machineName, int quantity, string companyEmail)
        {
            if (companyEmail == null)
            {
                TempData["error"] = "Please Fill All Fields!";
                return RedirectToAction("ShowMachines");
            }
            if (quantity <= 0)
            {
                TempData["error"] = "Quantity must be greater than 0";
                return RedirectToAction("ShowMachines");
            }
            try
            {
                var smtpClient = new SmtpClient(emailSettings.Host)
                {
                    Port = emailSettings.Port,
                    Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailSettings.UserName),
                    Subject = "Machine Stock Request",
                    Body = $"Dear Supplier,\n\nWe need {quantity} units of {machineName}.\nPlease confirm the availability.\n\nBest Regards,\nPharmacist",
                    IsBodyHtml = false,
                };

                mailMessage.To.Add(companyEmail);

                mailMessage.CC.Add(emailSettings.CCEmail);

                await smtpClient.SendMailAsync(mailMessage);

                TempData["success"] = "Email Sent Successfully!";
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error sending email: " + ex.Message;
            }

            return RedirectToAction("ShowMachines");
        }

        public async Task<JsonResult> DisableBatch(int? batchId)
        {
            if (batchId == null)
            {
                return Json(new { success = false, message = "Batch Id Not Found" });
            }

            var batch = await myDbContext.Batches.FindAsync(batchId);
            if (batch == null)
            {
                return Json(new { success = false, message = "Batch Not Found" });
            }

            if (batch.Active == 1)
            {
                batch.Active = 0;
                await myDbContext.SaveChangesAsync();
                return Json(new { success = true, message = "Batch Disable Successfully" });
            }
            else
            {
                batch.Active = 1;
                await myDbContext.SaveChangesAsync();
                return Json(new { success = true, message = "Batch Enable Successfully" });
            }
        }

        public async Task<IActionResult> EditMedicine(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicine = await myDbContext.Medicines.Include(x => x.Batches).Where(x => x.Id == id).FirstOrDefaultAsync();

            if (medicine == null)
            {
                return NotFound();
            }
            MedicineBatchViewModel medicineBatchViewModel = new MedicineBatchViewModel()
            {
                Medicine = medicine,
                Batch = medicine.Batches.FirstOrDefault()
            };
            var categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
            ViewBag.categories = categories;
            return View(medicineBatchViewModel);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditMedicine(MedicineBatchViewModel medicineBatchViewModel, IFormFile? Image, string oldImage)
        {
            //return Json(ModelState);
            //return Json(oldImage);
            //return Json(medicineBatchViewModel);
            if (ModelState.IsValid)
            {
                if (Image != null)
                {
                    var NotExtension = Path.GetFileNameWithoutExtension(Image.FileName);
                    var Extension = Path.GetExtension(Image.FileName);

                    var ImageName = NotExtension + DateTime.Now.ToString("yyyyMMddHHmmss") + Extension;

                    var path = Path.Combine(webHostEnvironment.WebRootPath, "MedicineImages");

                    var ImagePath = Path.Combine(path, ImageName);

                    using (var stream = new FileStream(ImagePath, FileMode.Create))
                    {
                        await Image.CopyToAsync(stream);
                    }

                    medicineBatchViewModel.Medicine.Image = ImageName;
                }
                else
                {
                    medicineBatchViewModel.Medicine.Image = oldImage;
                }
                medicineBatchViewModel.Medicine.AddedBy = Convert.ToInt32(HttpContext.Session.GetString("id"));
                myDbContext.Medicines.Update(medicineBatchViewModel.Medicine);
                await myDbContext.SaveChangesAsync();

                medicineBatchViewModel.Batch.MedicineId = medicineBatchViewModel.Medicine.Id;
                myDbContext.Batches.Update(medicineBatchViewModel.Batch);
                await myDbContext.SaveChangesAsync();

                TempData["success"] = "Medicine Edit Successfully";
                return RedirectToAction("ShowMedicine");
            }
            var categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
            ViewBag.categories = categories;
            return View(medicineBatchViewModel);
        }

        public async Task<IActionResult> EditMachine(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicine = await myDbContext.MedicalInstruments.Include(x => x.Technician).Where(x => x.Id == id).FirstOrDefaultAsync();

            if (medicine == null)
            {
                return NotFound();
            }

            var categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
            ViewBag.categories = categories;
            return View(medicine);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditMachine(MedicalInstrument medicalInstrument, IFormFile? Image)
        {
            //return Json(Image);
            ModelState.Remove("Image");

            if (ModelState.IsValid)
            {
                var machine = await myDbContext.MedicalInstruments.FindAsync(medicalInstrument.Id);

                if (machine != null)
                {
                    machine.Name = medicalInstrument.Name;
                    machine.Description = medicalInstrument.Description;
                    machine.UnitPrice = medicalInstrument.UnitPrice;
                    machine.Manufacturer = medicalInstrument.Manufacturer;
                    machine.ManufacturerEmail = medicalInstrument.ManufacturerEmail;
                    machine.StockQuantity = medicalInstrument.StockQuantity;
                    machine.ReorderLevel = medicalInstrument.ReorderLevel;
                    machine.CategoryId = medicalInstrument.CategoryId;
                    machine.AddedBy = Convert.ToInt32(HttpContext.Session.GetString("id"));

                    if (Image != null)
                    {
                        var NotExtension = Path.GetFileNameWithoutExtension(Image.FileName);
                        var Extension = Path.GetExtension(Image.FileName);
                        var ImageName = NotExtension + DateTime.Now.ToString("yyyyMMddHHmmss") + Extension;
                        var path = Path.Combine(webHostEnvironment.WebRootPath, "MachinesImages");
                        var ImagePath = Path.Combine(path, ImageName);

                        using (var stream = new FileStream(ImagePath, FileMode.Create))
                        {
                            await Image.CopyToAsync(stream);
                        }

                        machine.Image = ImageName;
                    }

                    await myDbContext.SaveChangesAsync();

                    TempData["success"] = "Machine Update Successfully";
                    return RedirectToAction("ShowMachines");
                }
            }

            var categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
            ViewBag.categories = categories;
            return View(medicalInstrument);
        }


        public async Task<IActionResult> AddQuantity(int? batchId, int quantity)
        {
            if (batchId == null)
            {
                TempData["error"] = "Batch Id Not Found";
                return RedirectToAction("ShowMedicine");
            }

            if (quantity <= 0)
            {
                TempData["error"] = "Value must be greater than zero";
                return RedirectToAction("ShowMedicine");
            }

            var batch = await myDbContext.Batches.FindAsync(batchId);

            batch.StockQuantity = batch.StockQuantity + quantity;

            await myDbContext.MedicineStockReports.AddAsync(new MedicineStockReport { BatchId = Convert.ToInt32(batchId), MedicineId = batch.MedicineId, TransactionType = MedicineStockReport.TransactionTypeEnum.In, Quantity = quantity, Date = DateTime.Now });

            await myDbContext.SaveChangesAsync();
            TempData["success"] = "Quantity Added Successfully";
            return RedirectToAction("ShowMedicine");

        }
        public async Task<IActionResult> AddMachineQuantity(int? machineId, int quantity)
        {
            if (machineId == null)
            {
                TempData["error"] = "Batch Id Not Found";
                return RedirectToAction("ShowMachines");
            }

            if (quantity <= 0)
            {
                TempData["error"] = "Value must be greater than zero";
                return RedirectToAction("ShowMachines");
            }

            var batch = await myDbContext.MedicalInstruments.FindAsync(machineId);

            batch.StockQuantity = batch.StockQuantity + quantity;

            await myDbContext.MachineStockReports.AddAsync(new MachineStockReport { MachineId = Convert.ToInt32(machineId), TransactionType = MachineStockReport.TransactionTypeEnum.In, Quantity = quantity, Date = DateTime.Now });

            await myDbContext.SaveChangesAsync();
            TempData["success"] = "Quantity Added Successfully";
            return RedirectToAction("ShowMachines");

        }

        public async Task<IActionResult> ShowMachines()
        {
            var medicines = await myDbContext.MedicalInstruments
                .Include(x => x.Technician)
                .Include(x => x.Category)
                .ToListAsync();

            var companies = await myDbContext.MedicalInstruments
                .Select(x => x.ManufacturerEmail)
                .Distinct()
                .ToListAsync();
            ViewBag.companies = companies;

            var stockAlert = await myDbContext.MedicalInstruments.Where(m => m.StockQuantity < m.ReorderLevel).ToListAsync();
            //return Json(stockAlert);
            ViewBag.stockAlert = stockAlert;

            return View(medicines);
        }

        public async Task<IActionResult> CreateMachine()
        {
            var categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
            ViewBag.categories = categories;
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CreateMachine(MedicalInstrument medicalInstrument, IFormFile Image)
        {
            if (Image == null)
            {
                ViewBag.categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
                TempData["error"] = "Image is required";
                return View();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            var fileExtension = Path.GetExtension(Image.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ViewBag.categories = await myDbContext.Categories.OrderByDescending(x => x.Id).ToListAsync();
                TempData["error"] = "Only JPG, PNG, and WEBP formats are allowed!";
                return View();
            }

            var ImageName = Path.GetFileNameWithoutExtension(Image.FileName)
                            + DateTime.Now.ToString("yyyyMMddHHmmss") + fileExtension;

            var path = Path.Combine(webHostEnvironment.WebRootPath, "MachinesImages");
            var ImagePath = Path.Combine(path, ImageName);

            using (var stream = new FileStream(ImagePath, FileMode.Create))
            {
                await Image.CopyToAsync(stream);
            }

            medicalInstrument.Image = ImageName;
            medicalInstrument.AddedBy = Convert.ToInt32(HttpContext.Session.GetString("id"));

            await myDbContext.MedicalInstruments.AddAsync(medicalInstrument);
            await myDbContext.SaveChangesAsync();

            TempData["success"] = "Machine Add Successfully";
            return RedirectToAction("ShowMachines");
        }


        public IActionResult MachineReport()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var report = myDbContext.MachineStockReports
                .GroupBy(m => new { m.MachineId, m.Machine.Name })
                .Select(g => new
                {
                    MachineId = g.Key.MachineId,
                    MachineName = g.Key.Name,
                    TotalIn = g.Where(x => x.TransactionType == MachineStockReport.TransactionTypeEnum.In).Sum(x => x.Quantity),
                    TotalOut = g.Where(x => x.TransactionType == MachineStockReport.TransactionTypeEnum.Out).Sum(x => x.Quantity),
                    NetQuantity = g.Sum(x => x.TransactionType == MachineStockReport.TransactionTypeEnum.In ? x.Quantity : -x.Quantity),
                    LastTransactionDate = g.Max(x => x.Date),
                    MonthlyIn = g.Where(x => x.TransactionType == MachineStockReport.TransactionTypeEnum.In
                                        && x.Date.Month == currentMonth && x.Date.Year == currentYear)
                                 .Sum(x => x.Quantity),
                    MonthlyOut = g.Where(x => x.TransactionType == MachineStockReport.TransactionTypeEnum.Out
                                        && x.Date.Month == currentMonth && x.Date.Year == currentYear)
                                  .Sum(x => x.Quantity),
                    StockQuantity = myDbContext.MedicalInstruments
                                  .Where(m => m.Id == g.Key.MachineId)
                                  .Select(m => m.StockQuantity)
                                  .FirstOrDefault()
                })
                .OrderBy(r => r.MachineId)
                .ToList();

            ViewBag.report = report;

            return View(report);
        }


        public IActionResult MedicineReport()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var report = myDbContext.MedicineStockReports
                .GroupBy(m => new { m.MedicineId, m.Medicine.MedicineName })
                .Select(g => new
                {
                    MedicineId = g.Key.MedicineId,
                    MedicineName = g.Key.MedicineName,
                    TotalIn = g.Where(x => x.TransactionType == MedicineStockReport.TransactionTypeEnum.In).Sum(x => x.Quantity),
                    TotalOut = g.Where(x => x.TransactionType == MedicineStockReport.TransactionTypeEnum.Out).Sum(x => x.Quantity),
                    NetQuantity = g.Sum(x => x.TransactionType == MedicineStockReport.TransactionTypeEnum.In ? x.Quantity : -x.Quantity),
                    LastTransactionDate = g.Max(x => x.Date),
                    MonthlyIn = g.Where(x => x.TransactionType == MedicineStockReport.TransactionTypeEnum.In
                                        && x.Date.Month == currentMonth && x.Date.Year == currentYear)
                                 .Sum(x => x.Quantity),
                    MonthlyOut = g.Where(x => x.TransactionType == MedicineStockReport.TransactionTypeEnum.Out
                                        && x.Date.Month == currentMonth && x.Date.Year == currentYear)
                                  .Sum(x => x.Quantity),
                    BatchStock = myDbContext.Batches.Where(b => b.MedicineId == g.Key.MedicineId).Sum(b => b.StockQuantity)
                })
                .OrderBy(r => r.MedicineId)
                .ToList();

            ViewBag.report = report;

            return View();
        }

        public async Task<IActionResult> ShowOrders()
        {
            var orders = await myDbContext.Orders
                .Include(x => x.User)
                .Include(x => x.OrderItems)
                .Where(x => x.OrderStatus == "Pending" || x.PaymentStatus == "Pending")
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> CompletedOrders()
        {
            var orders = await myDbContext.Orders
                .Include(x => x.User)
                .Include(x => x.OrderItems)
                .Where(x => x.OrderStatus == "Completed" && x.PaymentStatus == "Paid")
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

            var orders = await myDbContext.Orders
                .Include(x => x.User)
                .Include(x => x.OrderItems)
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            List<OrdersManage> ordersManages = new List<OrdersManage>();

            foreach (var items in orders.OrderItems)
            {
                string productName = "unknown";
                string productImage = "defauly";
                string productType = "unknown";

                if (items.ProductType == "Medicine")
                {
                    var medicines = await myDbContext.Medicines.Where(x => x.Id == items.ProductId).Select(x => new { x.MedicineName, x.Image }).FirstOrDefaultAsync();

                    if (medicines != null)
                    {
                        productName = medicines.MedicineName;
                        productImage = medicines.Image;
                        productType = "Medicine";
                    }

                }
                else if (items.ProductType == "MedicalInstrument")
                {
                    var machines = await myDbContext.MedicalInstruments.Where(x => x.Id == items.ProductId).Select(x => new { x.Name, x.Image }).FirstOrDefaultAsync();

                    if (machines != null)
                    {
                        productName = machines.Name;
                        productImage = machines.Image;
                        productType = "MedicalInstrument";
                    }
                }

                ordersManages.Add(new OrdersManage
                {
                    ProductName = productName,
                    ProductType = productType,
                    Image = productImage,
                    Price = (int)items.Price,
                    Quantity = items.Quantity,
                });


            }

            ViewBag.orderId = orders.Id;
            ViewBag.userName = orders.User.Name;
            ViewBag.userEmail = orders.User.Email;
            ViewBag.userPhone = orders.User.Phone;
            ViewBag.userAddress = orders.User.Address;

            ViewBag.paymentStatus = orders.PaymentStatus;
            ViewBag.orderStatus = orders.OrderStatus;

            ViewBag.total = orders.TotalAmount;

            return View(ordersManages);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> UpdateOrder(int? orderId, string userEmail, int? paymentStatus, int? deliveryStatus)
        {
            //return Json(orderId);
            if (orderId == null)
            {
                return NotFound("Order Id Not Found");
            }
            if (paymentStatus == null && deliveryStatus == null)
            {
                TempData["error"] = "Please select valid status of both";
                return RedirectToAction("ViewOrder", new { id = orderId });

            }
            if (userEmail == null)
            {
                TempData["error"] = "Email Not Found";
                return RedirectToAction("ViewOrder", new { id = orderId });

            }

            var order = await myDbContext.Orders.FindAsync(orderId);

            if (order == null)
            {
                TempData["error"] = "Order Not Found";
                return RedirectToAction("ViewOrder", new { id = orderId });

            }

            if (paymentStatus == 1 && deliveryStatus == 1)
            {
                //this.EmailSetting();
                var smtpClient = new SmtpClient(emailSettings.Host)
                {
                    Port = emailSettings.Port,
                    Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailSettings.UserName),
                    Subject = "Payment & Delivery of Order",
                    Body = "Your Payment and Delivery has been completed successfully for Order Id: " + orderId,
                    IsBodyHtml = false,
                };

                mailMessage.To.Add(userEmail);
                mailMessage.CC.Add(emailSettings.CCEmail);
                await smtpClient.SendMailAsync(mailMessage);

                order.PaymentStatus = "Paid";
                order.OrderStatus = "Completed";
                await myDbContext.SaveChangesAsync();
                TempData["success"] = "Order and Payment Status Updated Successfully";
                return RedirectToAction("CompletedOrders");

            }
            if (order.PaymentStatus != "Paid")
            {

                if (paymentStatus == 1)
                {
                    //this.EmailSetting();
                    var smtpClient = new SmtpClient(emailSettings.Host)
                    {
                        Port = emailSettings.Port,
                        Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                        EnableSsl = true
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(emailSettings.UserName),
                        Subject = "Payment of Order",
                        Body = "Your payment has been completed successfully for Order Id: " + orderId,
                        IsBodyHtml = false,
                    };

                    mailMessage.To.Add(userEmail);
                    mailMessage.CC.Add(emailSettings.CCEmail);
                    await smtpClient.SendMailAsync(mailMessage);

                    order.PaymentStatus = "Paid";
                    await myDbContext.SaveChangesAsync();
                    TempData["success"] = "Payment Status Updated Successfully";
                    return RedirectToAction("ViewOrder", new { id = orderId });

                }

            }
            if (order.OrderStatus != "Completed")
            {


                if (deliveryStatus == 1)
                {
                    //this.EmailSetting();
                    var smtpClient = new SmtpClient(emailSettings.Host)
                    {
                        Port = emailSettings.Port,
                        Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                        EnableSsl = true
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(emailSettings.UserName),
                        Subject = "Delivery of Order",
                        Body = "Your Delivery has been completed successfully for Order Id: " + orderId,
                        IsBodyHtml = false,
                    };

                    mailMessage.To.Add(userEmail);
                    mailMessage.CC.Add(emailSettings.CCEmail);
                    await smtpClient.SendMailAsync(mailMessage);

                    order.OrderStatus = "Completed";
                    await myDbContext.SaveChangesAsync();
                    TempData["success"] = "Delivery Status Updated Successfully";
                    return RedirectToAction("ViewOrder", new { id = orderId });

                }
            }
            TempData["nochange"] = "Nothing Changes Apply";
            return RedirectToAction("ViewOrder", new { id = orderId });

        }

        public SmtpClient EmailSetting()
        {
            var smtpClient = new SmtpClient(emailSettings.Host)
            {
                Port = emailSettings.Port,
                Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                EnableSsl = true
            };

            return smtpClient;
        }

        public async Task<IActionResult> ShowReviews()
        {
            var review = await myDbContext.Reviews
                .Include(x => x.User)
                .ToListAsync();

            List<ReviewsManage> reviewsManages = new List<ReviewsManage>();

            foreach (var items in review)
            {
                string productName = "unknown";
                string image = "default";
                string productType = "unknown";
                if (items.ProductType == "Medicine")
                {
                    var medicines = await myDbContext.Medicines.Where(x => x.Id == items.ProductId).FirstOrDefaultAsync();

                    if (medicines != null)
                    {
                        productName = medicines.MedicineName;
                        image = medicines.Image;
                        productType = "Medicine";
                    }
                }
                else if (items.ProductType == "MedicalInstrument")
                {
                    var machines = await myDbContext.MedicalInstruments.Where(x => x.Id == items.ProductId).FirstOrDefaultAsync();

                    if (machines != null)
                    {
                        productName = machines.Name;
                        image = machines.Image;
                        productType = "MedicalInstrument";
                    }
                }

                reviewsManages.Add(new ReviewsManage
                {
                    Name = items.User.Name,
                    Email = items.User.Email,
                    Phone = items.User.Phone,
                    ProductName = productName,
                    ProductType = productType,
                    Image = image,
                    Rating = items.Rating,
                    Comment = items.Comment

                });
            }
            return View(reviewsManages);
        }
    }
}
