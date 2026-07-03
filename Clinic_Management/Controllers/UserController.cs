using Clinic_Management.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using static System.Net.WebRequestMethods;
using Stripe;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Clinic_Management.Migrations;

namespace Clinic_Management.Controllers
{
    public class UserController : Controller
    {
        private readonly myDbContext myDbContext;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly EmailSettings emailSettings;
        private readonly TwilioSettings twilioSettings;
        private readonly GoogleReCAPTCHA googleReCAPTCHA;
        public UserController(
            myDbContext myDbContext,
            IWebHostEnvironment webHostEnvironment,
            IOptions<EmailSettings> emailSettings,
            IOptions<TwilioSettings> twilioSettings,
            IOptions<GoogleReCAPTCHA> googleReCAPTCHA
            )
        {
            this.webHostEnvironment = webHostEnvironment;
            this.myDbContext = myDbContext;
            this.emailSettings = emailSettings.Value;
            this.twilioSettings = twilioSettings.Value;
            this.googleReCAPTCHA = googleReCAPTCHA.Value;
        }

        private async Task SendingEmail(string email, string subject, string body)
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
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };

            mailMessage.To.Add(email);

            mailMessage.CC.Add(emailSettings.CCEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }

        private async Task OTPEmail(int otp)
        {
            string body = $"Your OTP Code\", \"Your OTP is: {otp}";
            string subject = "Email Verification";
            await this.SendingEmail(HttpContext.Session.GetString("email"), subject, body);
        }
        private async Task<bool> VerifiedUser()
        {
            var verifyOtp = await myDbContext.VerifiedUsers.AnyAsync(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id")));
            return verifyOtp;
        }

        private async Task<bool> VerifyCaptcha(string token)
        {
            var secret = googleReCAPTCHA.SecretKey;
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={token}",
                null
            );
            var jsonString = await response.Content.ReadAsStringAsync();
            dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
            return result.success == true;
        }
        public IActionResult Register(string? email = null, string? returnUrl = null)
        {
            ViewBag.SiteKey = googleReCAPTCHA.SiteKey;
            try
            {
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    var urlBytes = Convert.FromBase64String(returnUrl);
                    var decodeUrl = Encoding.UTF8.GetString(urlBytes);
                    if (Url.IsLocalUrl(decodeUrl))
                    {
                        TempData["returnUrl"] = decodeUrl;
                    }
                }
            }
            catch
            {
                TempData["returnUrl"] = null;
            }

            if (email != null)
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("id")))
                {
                    TempData["error"] = "You are already logged in";
                }
            }
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("id")))
            {
                ViewBag.email = email;
                return View();
            }

            return RedirectToAction("Index", "Home");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Register(User user, IFormFile? Image, string? remember)
        {
            //return Json(DateTime.Now.AddMinutes(5).ToString());
            ViewBag.SiteKey = googleReCAPTCHA.SiteKey;
            if (string.IsNullOrEmpty(user.Phone))
            {
                ModelState.AddModelError("Phone", "Phone is required");
            }
            if (string.IsNullOrEmpty(user.Address))
            {
                ModelState.AddModelError("Address", "Address is required");
            }
            if (user.Gender != null)
            {
                TempData["userType"] = "patient";
                if (string.IsNullOrEmpty(user.MedicalHistory))
                {
                    ModelState.AddModelError("MedicalHistory", "Medical History is required");
                }
            }
            if (ModelState.IsValid)
            {
                var isCaptchaValid = await this.VerifyCaptcha(user.RecaptchaToken);
                if (!isCaptchaValid)
                {
                    ModelState.AddModelError("RecaptchaToken", "ReCAPTCHA verification failed.");
                    return View(user);
                }
                //return Json(Image);
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

                if (user_email == null)
                {
                    var hash = new PasswordHasher<User>();
                    user.Password = hash.HashPassword(user, user.Password);
                    if (user.Gender == null)
                    {
                        user.Role = 0;
                    }
                    else
                    {
                        user.Role = 3;
                    }

                    await myDbContext.Users.AddAsync(user);
                    await myDbContext.SaveChangesAsync();

                    var user_id = await myDbContext.Users.Where(x => x.Email == user.Email).FirstOrDefaultAsync();

                    HttpContext.Session.SetString("id", user_id.Id.ToString());
                    HttpContext.Session.SetString("name", user.Name);
                    HttpContext.Session.SetString("email", user.Email);
                    HttpContext.Session.SetString("phone", user.Phone);
                    HttpContext.Session.SetString("address", user.Address);
                    HttpContext.Session.SetString("role", user.Role.ToString());
                    HttpContext.Session.SetString("image", user.Image);
                    HttpContext.Session.SetString("password", user.Password);

                    if (user.Role == 3)
                    {
                        HttpContext.Session.SetString("gender", user.Gender.ToString());
                        HttpContext.Session.SetString("medicalhistory", user.MedicalHistory);
                    }

                    this.CookiesSet(user, remember);

                    var otp = new Random().Next(100000, 999999);

                    HttpContext.Session.SetString("otp", otp.ToString());
                    HttpContext.Session.SetString("otpExpiry", DateTime.Now.AddMinutes(5).ToString());

                    await this.OTPEmail(otp);

                    TempData["otpsuccess"] = $"We sent OTP to {HttpContext.Session.GetString("email")}! Please enter OTP to continue";
                    return RedirectToAction("VerifyOtp");
                }
                else
                {
                    //ModelState.AddModelError("Email", "Email Already Exist");
                    ViewBag.email = user.Email;
                    TempData["email_err"] = "Email Already Exists";
                    return View(user);
                }
            }
            ViewBag.email = user.Email;
            return View(user);
        }

        public async Task<JsonResult> checkEmail(string email)
        {
            bool user_email;
            if (HttpContext.Session.GetString("id") == null)
            {
                user_email = await myDbContext.Users
                    .Where(x => x.Email == email)
                    .AnyAsync();
            }
            else
            {
                user_email = await myDbContext.Users
                    .Where(x => x.Email == email && x.Id != Convert.ToInt32(HttpContext.Session.GetString("id")))
                    .AnyAsync();
            }

            if (user_email)
            {
                return Json("Email Already Exist");
            }
            else
            {
                return Json("");
            }
        }

        public IActionResult Login(string? returnUrl)
        {
            ViewBag.SiteKey = googleReCAPTCHA.SiteKey;
            //return Json(returnUrl.ToArray());
            //return Json(returnUrl);
            try
            {
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    var bytesUrl = Convert.FromBase64String(returnUrl);
                    var decodeUrl = Encoding.UTF8.GetString(bytesUrl);
                    if (Url.IsLocalUrl(decodeUrl))
                    {
                        TempData["returnUrl"] = decodeUrl;
                    }
                }
            }
            catch
            {
                TempData["returnUrl"] = null;
            }
            //TempData["returnUrl"] = returnUrl;
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("id")))
            {
                return View();
            }

            return RedirectToAction("Index", "Home");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Login(User user, string? returnUrl, string remember)
        {
            ViewBag.SiteKey = googleReCAPTCHA.SiteKey;
            var userData = await myDbContext.Users.Where(x => x.Email == user.Email).FirstOrDefaultAsync();
            if (userData != null && userData.Provider == Models.User._Provider.Google)
            {
                if (userData.Role == 0)
                {
                    return RedirectToAction("GoogleLogin", "Account", new { email = user.Email, role = "user" });
                }
                else
                {
                    return RedirectToAction("GoogleLogin", "Account", new { email = user.Email, role = "patient" });
                }
            }
            if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                TempData["error"] = "Please Fill All Fields";
                return View(user);
            }
            var isCaptchaValid = await this.VerifyCaptcha(user.RecaptchaToken);
            if (!isCaptchaValid)
            {
                ModelState.AddModelError("RecaptchaToken", "ReCAPTCHA verification failed.");
                return View(user);
            }

            //return Json(userData);
            if (userData != null)
            {
                var hash = new PasswordHasher<User>();

                var result = hash.VerifyHashedPassword(user, userData.Password, user.Password);

                if (result == PasswordVerificationResult.Success)
                {
                    this.SessionsSet(userData);

                    this.CookiesSet(userData, remember);

                    var verifyUser = await myDbContext.VerifiedUsers.Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id"))).FirstOrDefaultAsync();

                    if (verifyUser != null)
                    {
                        HttpContext.Session.SetString("confirmotp", "done");
                    }

                    if (userData.Role == 0 || userData.Role == 3)
                    {
                        TempData["successLogin"] = "Successfully Login";

                        if (TempData["returnUrl"] == null)
                        {
                            returnUrl = null;
                        }
                        else
                        {
                            returnUrl = TempData["returnUrl"].ToString();
                        }
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        if (HttpContext.Request.Cookies["adminRemember"] == null)
                        {
                            var otp = new Random().Next(100000, 999999);
                            HttpContext.Session.SetString("otp", otp.ToString());
                            HttpContext.Session.SetString("otpExpiry", DateTime.Now.AddMinutes(5).ToString());

                            await this.OTPEmail(otp);

                            return RedirectToAction("VerifyOtp");
                        }
                        else
                        {
                            HttpContext.Session.SetString("confirmotp", "done");
                            return RedirectToAction("Index", "Admin");
                        }
                    }
                }
                else
                {
                    TempData["error"] = "Password is incorrect";
                    return View(user);
                }
            }
            else
            {
                TempData["error"] = "Email is incorrect";
                return View(user);
            }
            return View(user);
        }

        public async Task<IActionResult> VerifyOtp()
        {
            if (!HttpContext.Session.Keys.Contains("id"))
            {
                return RedirectToAction("Index", "Home");
            }

            if (HttpContext.Session.GetString("role") == "0")
            {
                if (await this.VerifiedUser())
                {
                    TempData["otperror"] = "You are already verified";
                    return RedirectToAction("Index", "Home");
                }
            }

            if (HttpContext.Request.Cookies["adminRemember"] != null)
            {
                TempData["otpinfo"] = "You can access Admin Panel without get OTP for 24 hours, If you would like to get new OTP for login then please hit super logout button";
                return RedirectToAction("Index", "Admin");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string otp, string adminRemember)
        {
            if (!HttpContext.Session.Keys.Contains("id"))
            {
                TempData["otperror"] = "Please Login First to get new OTP";
                return RedirectToAction("Login");
            }

            string sessionOtp = HttpContext.Session.GetString("otp");
            string expiryTimeStr = HttpContext.Session.GetString("otpExpiry");
            if (otp == null)
            {
                TempData["otperror"] = "Please Enter OTP";
                return View();
            }

            if (otp != HttpContext.Session.GetString("otp"))
            {
                TempData["error"] = "OTP is invalid";
                return View();
            }

            if (HttpContext.Session.Keys.Contains("id") && HttpContext.Session.GetString("role") == "0")
            {
                if (!string.IsNullOrEmpty(sessionOtp) && !string.IsNullOrEmpty(expiryTimeStr))
                {
                    DateTime expiry = DateTime.Parse(expiryTimeStr);
                    if (DateTime.Now <= expiry)
                    {
                        if (otp == HttpContext.Session.GetString("otp"))
                        {
                            await myDbContext.VerifiedUsers.AddAsync(new VerifiedUser
                            {
                                UserId = Convert.ToInt32(HttpContext.Session.GetString("id")),
                                OTP = Convert.ToInt32(otp),
                                Verified = Models.VerifiedUser._Verified.Yes
                            });
                            var markVerified = await myDbContext.Users.FindAsync(Convert.ToInt32(HttpContext.Session.GetString("id")));
                            if (markVerified != null)
                            {
                                markVerified.Verified = Models.User._Verified.Yes;
                            }
                            await myDbContext.SaveChangesAsync();

                            HttpContext.Session.Remove("otp");
                            HttpContext.Session.Remove("otpExpiry");

                            HttpContext.Session.SetString("confirmotp", "done");

                            TempData["success"] = "Successfully Registered";

                            string subject = "User Registration";
                            string body = $"Congratulations! Hi, {HttpContext.Session.GetString("name")}, You have successfully registered";
                            await this.SendingEmail(HttpContext.Session.GetString("email"), subject, body);
                            //string returnUrl = TempData["ret"]
                            if (TempData["returnUrl"] == null)
                            {
                                return RedirectToAction("Index", "Home");
                            }
                            return Redirect(TempData["returnUrl"].ToString());
                        }
                    }
                    else
                    {
                        this.ExpireOTP();
                        return View();
                    }
                }
            }
            if (
                (HttpContext.Session.Keys.Contains("id") && HttpContext.Session.GetString("role") == "1") ||
                (HttpContext.Session.Keys.Contains("id") && HttpContext.Session.Keys.Contains("staff_role"))
            )
            {
                if (!string.IsNullOrEmpty(sessionOtp) && !string.IsNullOrEmpty(expiryTimeStr))
                {
                    //TempData["error"] = "hello";
                    DateTime expiryTime = DateTime.Parse(expiryTimeStr);

                    if (DateTime.Now <= expiryTime)
                    {
                        if (adminRemember == "yes")
                        {
                            var CookieOptions = new CookieOptions
                            {
                                Expires = DateTime.Now.AddSeconds(60),
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Strict
                            };
                            Response.Cookies.Append("adminRemember", adminRemember);
                        }
                        if (otp == HttpContext.Session.GetString("otp"))
                        {
                            HttpContext.Session.Remove("otp");
                            HttpContext.Session.Remove("otpExpiry");
                            HttpContext.Session.SetString("confirmotp", "done");
                            TempData["successLogin"] = "Login Successfully";
                            return RedirectToAction("Index", "Admin");
                        }
                    }
                    else
                    {
                        this.ExpireOTP();
                        return View();
                    }
                }
            }
            return View();
        }

        private void ExpireOTP()
        {
            TempData["otperror"] = "OTP has expired. Please request a new one.";
            HttpContext.Session.Remove("otp");
            HttpContext.Session.Remove("otpExpiry");
        }

        public async Task<IActionResult> ResendOtp()
        {
            if (HttpContext.Session.GetString("otpprocess") == "true")
            {
                TempData["otperror"] = "OTP is already being sent. Please wait...";
                return RedirectToAction("VerifyOtp");
            }
            if (HttpContext.Session.Keys.Contains("id"))
            {
                if (await this.VerifiedUser())
                {
                    TempData["otperror"] = "You are already verified";
                    return RedirectToAction("Index", "Home");
                }

                var lastOtpSentStr = HttpContext.Session.GetString("lastOtpSent");
                if (lastOtpSentStr != null)
                {
                    DateTime lastOtpSent = DateTime.Parse(lastOtpSentStr);
                    if (DateTime.Now < lastOtpSent.AddMinutes(1))
                    {
                        TimeSpan remaining = lastOtpSent.AddMinutes(1) - DateTime.Now;
                        TempData["otperror"] = $"Please wait {remaining.Seconds} seconds before requesting another OTP.";
                        //TempData["disable"] = "disabled";
                        return RedirectToAction("VerifyOtp");
                    }
                }

                HttpContext.Session.SetString("otpprocess", true.ToString());
                try
                {
                    var otp = new Random().Next(100000, 999999);

                    HttpContext.Session.SetString("otp", otp.ToString());
                    HttpContext.Session.SetString("otpExpiry", DateTime.Now.AddMinutes(5).ToString());
                    HttpContext.Session.SetString("lastOtpSent", DateTime.Now.ToString());

                    string subject = "Again Email Verification";
                    string body = $"Your OTP Code, Your new OTP is: {otp}";

                    await this.SendingEmail(HttpContext.Session.GetString("email"), subject, body);

                    TempData["otpsuccess"] = "OTP sent successfully to email " + HttpContext.Session.GetString("email");
                    return RedirectToAction("VerifyOtp");
                }
                finally
                {
                    HttpContext.Session.Remove("otpprocess");
                }
            }

            TempData["otperror"] = "Please Login First to get new OTP";
            return RedirectToAction("Login");
        }

        public IActionResult ForgotPassword()
        {
            if (!HttpContext.Session.Keys.Contains("id"))
            {
                return View();
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> GetAccount(int? id)
        {
            if (id == null)
            {
                return NotFound("Id is missing");
            }

            var user = await myDbContext.Users.FindAsync(id);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> SearchAccount(string? mobile, string? email)
        {
            if (mobile != null)
            {
                var user = await myDbContext.Users.Where(x => x.Phone == mobile).FirstOrDefaultAsync();

                if (user == null)
                {
                    return RedirectToAction("GetAccount", new { id = 0 });
                }

                return RedirectToAction("GetAccount", new { id = user.Id });
            }
            else if (email != null)
            {
                var user = await myDbContext.Users.Where(x => x.Email == email).FirstOrDefaultAsync();

                if (user == null)
                {
                    return RedirectToAction("GetAccount", new { id = 0 });
                }

                if (user.Image.StartsWith("http"))
                {
                    //string referer = Request.Headers["Referer"];
                    //string url = string.IsNullOrEmpty(referer) ? "/" : referer;
                    TempData["autherror"] = "You signed up using Google. Please use Google to log in!";
                    return Redirect("Login");
                }

                return RedirectToAction("GetAccount", new { id = user.Id });
            }
            TempData["error"] = "Please search by at least one field";
            return RedirectToAction("ForgotPassword");
        }

        public async Task<IActionResult> ResetPasswordOptions(int? id)
        {
            if (HttpContext.Session.Keys.Contains("id"))
            {
                return RedirectToAction("ChangePassword", new { id = id });
            }

            if (id == null)
            {
                return NotFound("Id is missing");
            }

            var user = await myDbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User Not Found");
            }

            var otpRequest = await myDbContext.OtpLogs.Where(x => x.PhoneNumber == user.Phone && x.RequestedAt.Date == DateTime.Today).CountAsync();
            if (otpRequest >= 2)
            {
                TempData["otperror"] = "Your Daily OTP via Phone Number limit reached. Please request OTP Via Email";
                return View(user);
            }

            TempData["otpinfo"] = "You can get only 2 OTP per day via Phone Number, so please be careful";
            return View(user);
        }
        public async Task<IActionResult> EnterCode(int? id)
        {
            if (HttpContext.Session.Keys.Contains("id"))
            {
                TempData["otpsuccess"] = "You are verified successfully";
                return RedirectToAction("ChangePassword", new { id = id });
            }

            var user = await myDbContext.Users.FindAsync(id);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> SendCode(int? userId, string method)
        {
            //DateTime dateTime = new DateTime();
            //return Json(DateTime.Today);
            //return Json(DateTime.Now.Date);
            if (userId == null)
            {
                return NotFound("Id is missing");
            }

            var user = await myDbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User Not Found");
            }

            if (method == "password")
            {
                return RedirectToAction("EnterPasswordLogin", new { id = userId });
            }

            if (method == "email")
            {
                var otp = new Random().Next(100000, 999999);

                HttpContext.Session.SetString("otp", otp.ToString());
                HttpContext.Session.SetString("otpExpiry", DateTime.Now.AddMinutes(5).ToString());

                string body = $"Your OTP Code\", \"Your OTP is: {otp}";
                string subject = "Password Reset";

                await this.SendingEmail(user.Email, subject, body);

                HttpContext.Session.SetString("codeByEmail", true.ToString());

                TempData["otpsuccess"] = $"Hi {user.Name}, We sent OTP to {user.Email}! Please enter OTP to login your account";
                return RedirectToAction("EnterCode", new { id = userId });
            }
            else if (method == "phone")
            {
                var otpRequest = await myDbContext.OtpLogs
                    .Where(x => x.PhoneNumber == user.Phone && x.RequestedAt.Date == DateTime.Today)
                    .CountAsync();

                if (otpRequest >= 2)
                {
                    TempData["otperror"] = "Daily OTP limit reached. Try again tomorrow. Please request another OTP via Email";
                    return RedirectToAction("EnterCode", new { id = userId });
                }

                TwilioClient.Init(twilioSettings.AccountSid, twilioSettings.AuthToken);

                var otp = new Random().Next(100000, 999999);

                HttpContext.Session.SetString("otp", otp.ToString());
                HttpContext.Session.SetString("otpExpiry", DateTime.Now.AddMinutes(5).ToString());

                var message = MessageResource.Create(
                    to: new Twilio.Types.PhoneNumber(this.ConvertToE164Format(localNumber: user.Phone)),
                    from: new PhoneNumber(twilioSettings.FromPhoneNumber),
                    body: $"Your OTP code is: {otp}"
                    );

                OtpLog otpLog = new OtpLog
                {
                    UserId = user.Id,
                    PhoneNumber = user.Phone,
                    Otp = otp,
                    MessageSid = message.Sid,
                };

                await myDbContext.OtpLogs.AddAsync(otpLog);
                await myDbContext.SaveChangesAsync();

                HttpContext.Session.SetString("codeByPhone", true.ToString());
                HttpContext.Session.SetString("codeByPhoneDone", true.ToString());
                HttpContext.Session.SetString("messageSid", message.Sid);

                TempData["otpsuccess"] = $"Hi {user.Name}, We sent OTP to {user.Phone}! Please enter OTP to login your account, Message SID: {message.Sid}";
                return RedirectToAction("EnterCode", new { id = userId });
            }
            TempData["error"] = "Something went wrong";
            return RedirectToAction("ResetPasswordOptions");
        }

        private string ConvertToE164Format(string localNumber)
        {
            if (localNumber == null)
            {
                return "No Number Found";
            }

            localNumber = localNumber.Replace(" ", "").Replace("-", "");

            if (localNumber.StartsWith("0") && localNumber.Length == 11)
            {
                return "+92" + localNumber.Substring(1);
            }

            if (localNumber.StartsWith("+92") && localNumber.Length == 13)
            {
                return localNumber;
            }

            throw new ArgumentException("Invalid Pakistani phone number format.");
        }

        public async Task<IActionResult> PasswordResetVerifyOTP(string otp, int? userId)
        {
            var user = await myDbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User Not Found");
            }
            if (otp == null)
            {
                TempData["otperror"] = "Please Enter OTP";
                //return View("SendCode",user);
                return RedirectToAction("EnterCode", new { id = userId });
            }
            string sessionotp = HttpContext.Session.GetString("otp");
            string otpExpiryStr = HttpContext.Session.GetString("otpExpiry");

            if (!string.IsNullOrEmpty(sessionotp) && !string.IsNullOrEmpty(otpExpiryStr))
            {
                DateTime otpExpiry = DateTime.Parse(otpExpiryStr);

                if (DateTime.Now <= otpExpiry)
                {
                    if (otp == sessionotp)
                    {
                        await myDbContext.VerifiedUsers.
                            AddAsync(new VerifiedUser
                            {
                                UserId = Convert.ToInt32(userId),
                                OTP = Convert.ToInt32(otp),
                                Verified = Models.VerifiedUser._Verified.Yes,
                                PasswordReset = Models.VerifiedUser._PasswordReset.Yes,
                                MessageSid = HttpContext.Session.GetString("messageSid")
                            });
                        await myDbContext.SaveChangesAsync();

                        HttpContext.Session.Remove("codeByPhoneDone");
                        HttpContext.Session.Remove("codeByEmail");
                        HttpContext.Session.Remove("otp");
                        HttpContext.Session.Remove("otpExpiry");
                        HttpContext.Session.SetString("confirmotp", "done");

                        this.SessionsSet(user);

                        TempData["otpsuccess"] = "Verified Successfully";
                        return RedirectToAction("ChangePassword", new { id = userId });
                    }
                    else
                    {
                        TempData["otperror"] = "OTP is invalid";
                        return RedirectToAction("EnterCode", new { id = userId });
                    }
                }
                else
                {
                    this.ExpireOTP();
                    return RedirectToAction("EnterCode", new { id = userId });
                }
            }
            else
            {
                TempData["otperror"] = "Please request new OTP";
                return RedirectToAction("EnterCode", new { id = userId });
            }
        }

        public async Task<IActionResult> PasswordResetResendOtp(int? userId)
        {
            if (HttpContext.Session.GetString("otpprocess") == "true")
            {
                TempData["otperror"] = "OTP is already being sent. Please wait...";
                return RedirectToAction("EnterCode", new { id = userId });
            }
            if (userId != null)
            {
                var user = await myDbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User Not Found");
                }

                var lastOtpSentStr = HttpContext.Session.GetString("lastOtpSent");
                if (lastOtpSentStr != null)
                {
                    DateTime lastOtpSent = DateTime.Parse(lastOtpSentStr);
                    if (DateTime.Now < lastOtpSent.AddMinutes(1))
                    {
                        TimeSpan remaining = lastOtpSent.AddMinutes(1) - DateTime.Now;
                        TempData["otperror"] = $"Please wait {remaining.Seconds} seconds before requesting another OTP.";
                        //TempData["disable"] = "disabled";
                        return RedirectToAction("EnterCode", new { id = userId });
                    }
                }
                HttpContext.Session.SetString("otpprocess", true.ToString());
                try
                {
                    var otp = new Random().Next(100000, 999999);
                    HttpContext.Session.SetString("otp", otp.ToString());
                    HttpContext.Session.SetString("otpExpiry", DateTime.Now.AddMinutes(5).ToString());
                    HttpContext.Session.SetString("lastOtpSent", DateTime.Now.ToString());

                    string subject = "Password Reset";
                    string body = $"Your OTP Code, Your new OTP for password reset is: {otp}";

                    await this.SendingEmail(user.Email, subject, body);

                    TempData["otpsuccess"] = $"OTP sent successfully to email {user.Email} for password reset";
                    return RedirectToAction("EnterCode", new { id = userId });
                }
                finally
                {
                    HttpContext.Session.Remove("otpprocess");
                }
            }
            return NotFound("Id is missing");
        }

        public async Task<IActionResult> EnterPasswordLogin(int? id)
        {
            if (id == null)
            {
                return NotFound("Id is missing");
            }

            var user = await myDbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User Not Found");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EnterPasswordLogin(int? Id, string Email, string Password, string? Remember)
        {
            if (Id == null)
            {
                return NotFound("Id is missing");
            }
            var user = await myDbContext.Users.FindAsync(Id);
            if (!ModelState.IsValid)
            {
                return View(user);
            }
            var userData = await myDbContext.Users.Where(x => x.Email == Email).FirstOrDefaultAsync();

            if (user != null)
            {
                var hash = new PasswordHasher<User>();

                PasswordVerificationResult result = hash.VerifyHashedPassword(userData, userData.Password, Password);

                if (result == PasswordVerificationResult.Success)
                {
                    this.SessionsSet(userData);

                    this.CookiesSet(userData, Remember);

                    var verifyUser = await myDbContext.VerifiedUsers.Where(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetString("id"))).FirstOrDefaultAsync();
                    if (verifyUser != null)
                    {
                        HttpContext.Session.SetString("confirmotp", "done");
                    }

                    if (userData.Role == 0 || userData.Role == 3)
                    {
                        TempData["successLogin"] = "Successfully Login";

                        if (!string.IsNullOrEmpty(TempData["returnUrl"].ToString()) && Url.IsLocalUrl(TempData["returnUrl"].ToString()))
                        {
                            return Redirect(TempData["returnUrl"].ToString());
                        }
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    TempData["error"] = "Password is incorrect";
                    return View(user);
                }
            }
            else
            {
                TempData["error"] = "Email is incorrect";
                return View(user);
            }

            return View(user);
        }

        public async Task<IActionResult> ChangePassword(int? id)
        {
            if (id == null)
            {
                return NotFound("Id is missing");
            }

            if (id != Convert.ToInt32(HttpContext.Session.GetString("id")))
            {
                return NotFound("Aage bhaag rhe ho na");
            }

            var user = await myDbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User Not Found");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int? userId, string password)
        {
            if (userId == null)
            {
                return NotFound("Id is missing");
            }

            var user = await myDbContext.Users.FindAsync(userId);
            if (userId != Convert.ToInt32(HttpContext.Session.GetString("id")))
            {
                TempData["error"] = "User Not Match with the specified info";
                return View(user);
            }
            if (user == null)
            {
                TempData["error"] = "User Not Found";
                return View(user);
            }
            if (password == null)
            {
                TempData["error"] = "Please Enter Password";
                return View(user);
            }
            if (password.Length < 6)
            {
                TempData["error"] = "Password length must be greater than 6";
                return View(user);
            }

            PasswordHasher<User> hash = new PasswordHasher<User>();
            user.Password = hash.HashPassword(user, password);

            myDbContext.Users.Update(user);
            await myDbContext.SaveChangesAsync();

            TempData["otpsuccess"] = "Your Password Changed Successfully and Login successfully";

            string subject = "Password Changed";
            string body = $"Hi {HttpContext.Session.GetString("name")}!, Your password updated successfully on {DateTime.Now.ToString()}";
            await this.SendingEmail(HttpContext.Session.GetString("email"), subject, body);

            if (TempData["returnUrl"] != null)
            {
                return Redirect(TempData["returnUrl"].ToString());
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Skip()
        {
            string subject = "Account Login";
            string message = $"Your account login with OTP on {DateTime.Now.ToString()}";
            await this.SendingEmail(HttpContext.Session.GetString("email"), subject, message);
            TempData["otpinfo"] = "You are not change your password yet";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> SkipandContinue(int? id)
        {
            var user = await myDbContext.Users.FindAsync(id);
            this.CookiesSet(user, "true");
            TempData["success"] = "Successfully Login";
            return RedirectToAction("Index", "Home");
        }

        public void SessionsSet(User user)
        {
            HttpContext.Session.SetString("id", user.Id.ToString());
            HttpContext.Session.SetString("name", user.Name);
            HttpContext.Session.SetString("email", user.Email);
            HttpContext.Session.SetString("role", user.Role.ToString());
            HttpContext.Session.SetString("image", user.Image);
            HttpContext.Session.SetString("password", user.Password);
            if (user.Role == 3)
            {
                HttpContext.Session.SetString("gender", user.Gender.ToString());
                HttpContext.Session.SetString("medicalhistory", user.MedicalHistory);
            }
            if (user.Role == 0 || user.Role == 1 || user.Role == 3)
            {
                HttpContext.Session.SetString("phone", user.Phone);
                HttpContext.Session.SetString("address", user.Address);
            }
            else
            {
                HttpContext.Session.SetString("staff_role", user.Staff_Role.ToString());
            }
        }

        private void CookiesSet(User userData, string? remember)
        {
            if (remember == "true")
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
                if (userData.Role == 3)
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
        }

        //[AuthenticationFilter]
        public IActionResult Logout()
        {
            if (HttpContext.Session.Keys.Contains("id"))
            {
                if (Request.Cookies["remember_info_shown"] != null)
                {
                    Response.Cookies.Delete("remember_info_shown");
                }
                HttpContext.Session.Clear();
                Response.Cookies.Delete("id");
                Response.Cookies.Delete("name");
                Response.Cookies.Delete("email");
                Response.Cookies.Delete("role");
                Response.Cookies.Delete("image");

                if (Request.Cookies["role"] == "3")
                {
                    Response.Cookies.Delete("gender");
                    Response.Cookies.Delete("medicalhistory");
                }

                if (Request.Cookies["role"] == "0" || Request.Cookies["role"] == "1" || Request.Cookies["role"] == "3")
                {
                    Response.Cookies.Delete("phone");
                    Response.Cookies.Delete("address");
                }

                else
                {
                    Response.Cookies.Delete("staff_role");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult SuperLogout()
        {
            this.Logout();

            if (HttpContext.Request.Cookies["adminRemember"] != null)
            {
                Response.Cookies.Delete("adminRemember");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
