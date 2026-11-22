using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClothExchange.Filters
{
    // Action filter: runs BEFORE an action. If the user is not logged in,
    // we stop and send them to the login page.
    public class LoginRequiredAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            int? userId = context.HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}
