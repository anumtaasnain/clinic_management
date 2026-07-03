using Clinic_Management.Models;

namespace Clinic_Management.Filters
{
    public class SessionRestoreMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionRestoreMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, myDbContext db)
        {
            var session = context.Session;

            if (!session.Keys.Contains("id") && context.User.Identity.IsAuthenticated)
            {
                string email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                if (!string.IsNullOrEmpty(email))
                {
                    var user = db.Users.FirstOrDefault(u => u.Email == email);
                    if (user != null)
                    {
                        session.SetString("id", user.Id.ToString());
                        session.SetString("role", user.Role.ToString() ?? "");
                        session.SetString("confirmotp", "done" ?? "no");
                    }
                }
            }

            await _next(context);
        }
    }

}
