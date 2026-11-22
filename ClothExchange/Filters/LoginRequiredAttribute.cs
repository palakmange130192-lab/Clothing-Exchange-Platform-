using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClothExchange.Filters;

// Action Filter: runs BEFORE an action. If nobody is logged in, send them to the Login page.
public class LoginRequiredAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetInt32("UserId") == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
