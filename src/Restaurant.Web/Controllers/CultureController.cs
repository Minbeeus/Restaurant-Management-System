using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Restaurant.Web.Controllers;

[Route("[controller]")]
public class CultureController : Controller
{
    [HttpGet("SetCulture")]
    public IActionResult SetCulture(string culture, string redirectUri)
    {
        if (!string.IsNullOrEmpty(culture))
        {
            HttpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1), 
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                }
            );
        }

        if (string.IsNullOrEmpty(redirectUri) || !Url.IsLocalUrl(redirectUri))
        {
            redirectUri = "/admin/dashboard";
        }

        return Redirect(redirectUri);
    }
}
