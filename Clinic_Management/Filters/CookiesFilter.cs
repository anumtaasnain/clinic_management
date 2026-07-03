using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Clinic_Management.Models;

namespace Clinic_Management.Filters
{
    public class CookiesFilter : ActionFilterAttribute
    {
        public async override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var cookies = context.HttpContext.Request.Cookies;
            if (context.HttpContext.Request.Cookies.Keys.Contains("id") && !context.HttpContext.Session.Keys.Contains("id"))
            {
                //var user = await myDbContext.Users.FindAsync(Convert.ToInt32(cookies["id"]));
                context.HttpContext.Session.SetString("id", cookies["id"]);
                context.HttpContext.Session.SetString("name", cookies["name"]);
                context.HttpContext.Session.SetString("email", cookies["email"]);
                context.HttpContext.Session.SetString("role", cookies["role"]);
                context.HttpContext.Session.SetString("image", cookies["image"]);
                //context.HttpContext.Session.SetString("password", user.Password);

                if (cookies["role"] == "3")
                {
                    context.HttpContext.Session.SetString("gender", cookies["gender"]);
                    context.HttpContext.Session.SetString("medicalhistory", cookies["medicalhistory"]);
                }
                if (cookies["role"] == "0" || cookies["role"] == "1" || cookies["role"] == "3")
                {
                    context.HttpContext.Session.SetString("phone", cookies["phone"]);
                    context.HttpContext.Session.SetString("address", cookies["address"]);
                }
                else
                {
                    context.HttpContext.Session.SetString("staff_role", cookies["staff_role"]);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
