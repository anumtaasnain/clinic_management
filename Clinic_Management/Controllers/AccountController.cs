using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Clinic_Management.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Clinic_Management.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly myDbContext myDbContext;
        public AccountController(myDbContext myDbContext)
        {
            this.myDbContext = myDbContext;
        }
        public IActionResult GoogleLogin(string? email, string? role, Models.User._Gender? Gendergoogle, string? MedicalHistorygoogle)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("/");
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", new { role = role, Gender = Gendergoogle, MedicalHistory = MedicalHistorygoogle }) ?? "/"
            };

            if (!string.IsNullOrEmpty(email))
            {
                properties.Items["login_hint"] = email;  // 👈 this tells Google to suggest this email
            }

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }


        public async Task<IActionResult> GoogleResponse(string role, Models.User._Gender? Gender, string? MedicalHistory)
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var picture = result.Principal.FindFirst("picture")?.Value;

            TempData["success"] = "Login Successfully";

            // Check your DB here
            var user = myDbContext.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                if (role == "patient")
                {
                    user = new User
                    {
                        Name = name,
                        Email = email,
                        Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                        Phone = null,
                        Address = null,
                        Gender = Gender,
                        MedicalHistory = MedicalHistory,
                        Created_at = DateTime.Now,
                        Updated_at = DateTime.Now,
                        Role = 3,
                        Image = picture ?? "user.png",
                        Verified = Models.User._Verified.Yes,
                        Provider = Models.User._Provider.Google
                    };
                }
                else
                {
                    user = new User
                    {
                        Name = name,
                        Email = email,
                        Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                        Phone = null,
                        Address = null,
                        Gender = null,
                        Created_at = DateTime.Now,
                        Updated_at = DateTime.Now,
                        Role = 0,
                        Image = picture ?? "user.png",
                        Verified = Models.User._Verified.Yes,
                        Provider = Models.User._Provider.Google
                    };
                }


                myDbContext.Users.Add(user);
                await myDbContext.SaveChangesAsync();
                TempData["success"] = "Registered Successfully";
            }

            HttpContext.Session.SetString("id", user.Id.ToString());
            HttpContext.Session.SetString("name", user.Name);
            HttpContext.Session.SetString("email", user.Email);
            HttpContext.Session.SetString("image", user.Image);
            HttpContext.Session.SetString("role", user.Role.ToString());
            HttpContext.Session.SetString("provider", user.Provider.ToString());

            HttpContext.Session.SetString("confirmotp", "done");
            //return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public IActionResult WhoAmI()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Json(claims);
        }

        public async Task<IActionResult> Logout(string? returnUrl = "/")
        {
            // Only sign out of the local cookie (this clears authentication)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Optionally clear session and cookies too
            HttpContext.Session.Clear();

            // Remove custom cookies if needed
            Response.Cookies.Delete("id");
            Response.Cookies.Delete("name");
            Response.Cookies.Delete("image");

            //return Redirect(Request.Headers["Referer"].ToString());
            string referer = Request.Headers["Referer"].ToString();
            string redirectUrl = !string.IsNullOrEmpty(referer) ? referer : "/";

            return Redirect(redirectUrl);
        }
    }
}
