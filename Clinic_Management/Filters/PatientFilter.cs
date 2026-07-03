using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class PatientFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (session.GetString("role") == "1")
        {
            return;
        }
        if (!session.Keys.Contains("id") || session.GetString("role") != "3")
        {
            if (context.Controller is Controller controller)
            {
                controller.TempData["alert"] = "You must be logged in as a Patient";
            }

            context.Result = new RedirectToActionResult("Login", "User", null);
        }

        base.OnActionExecuting(context);
    }
}
