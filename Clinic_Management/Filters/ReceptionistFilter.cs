using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ReceptionistFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var cookie = context.HttpContext.Request.Cookies;

        if (cookie["adminRemember"] != null)
        {
            if (string.IsNullOrEmpty(session.GetString("adminRememberExpire")))
            {
                session.SetString("adminRememberExpire", DateTime.Now.AddHours(24).ToString());
            }

            var expireStr = session.GetString("adminRememberExpire");
            if (!string.IsNullOrEmpty(expireStr) && DateTime.TryParse(expireStr, out var expireTime))
            {
                if (DateTime.Now >= expireTime)
                {
                    if (context.Controller is Controller controller)
                    {
                        controller.TempData["alert"] = "Your 24 hours completed successfully, Please request new OTP to access Dashboard";
                    }
                    context.HttpContext.Response.Cookies.Delete("adminRemember");
                    session.Remove("confirmotp");
                    session.Remove("adminRememberExpire");
                    context.Result = new RedirectToActionResult("VerifyOtp", "User", null);
                    return;
                }
                else
                {
                    session.SetString("confirmotp", "done");
                }
            }
        }
        if (session.GetString("role") == "1")
        {
            return;
        }

        if (!session.Keys.Contains("staff_role") || session.GetString("staff_role") != "Receptionist")
        {
            context.Result = new RedirectToActionResult("Login", "User", null);
        }
        else if (!session.Keys.Contains("confirmotp"))
        {
            if (context.Controller is Controller controller)
            {
                controller.TempData["alert"] = "You are not verify till now! Please verify the code we sent to your email";
            }
            context.Result = new RedirectToActionResult("VerifyOtp", "User", null);
        }

        base.OnActionExecuting(context);
    }
}
