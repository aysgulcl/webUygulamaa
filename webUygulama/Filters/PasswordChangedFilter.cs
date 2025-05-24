using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using webUygulama.Models;

namespace webUygulama.Filters
{
    public class PasswordChangedFilter : IAsyncActionFilter
    {
        private readonly UserManager<User> _userManager;

        public PasswordChangedFilter(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = await _userManager.GetUserAsync(context.HttpContext.User);
            
            if (user != null && !user.PasswordChanged)
            {
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    // Eğer zaten ChangePassword sayfasında değilse, yönlendir
                    if (!(context.RouteData.Values["controller"]?.ToString()?.Equals("Account", StringComparison.OrdinalIgnoreCase) == true &&
                          context.RouteData.Values["action"]?.ToString()?.Equals("ChangePassword", StringComparison.OrdinalIgnoreCase) == true))
                    {
                        context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
                        return;
                    }
                }
            }

            await next();
        }
    }
} 