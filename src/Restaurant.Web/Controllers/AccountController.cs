using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Restaurant.Web.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    [HttpGet("Login")]
    public async Task<IActionResult> Login([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return Redirect("/admin/login");
        }

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
        {
            return Redirect("/admin/login");
        }

        var jwtToken = handler.ReadJwtToken(token);
        
        var claims = new List<Claim>();
        foreach (var claim in jwtToken.Claims)
        {
            claims.Add(claim);

            if (claim.Type == "role" && !claims.Any(c => c.Type == ClaimTypes.Role && c.Value == claim.Value))
            {
                claims.Add(new Claim(ClaimTypes.Role, claim.Value));
            }
            if ((claim.Type == "unique_name" || claim.Type == "name") && !claims.Any(c => c.Type == ClaimTypes.Name && c.Value == claim.Value))
            {
                claims.Add(new Claim(ClaimTypes.Name, claim.Value));
            }
            if ((claim.Type == "nameid" || claim.Type == "sub") && !claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == claim.Value))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
            }
        }
        
        // Thêm chính token vào claim để sau này lấy ra dùng cho API calls
        claims.Add(new Claim("jwt_token", token));

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = jwtToken.ValidTo
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity), 
            authProperties);

        return Redirect("/admin/dashboard");
    }

    [HttpGet("Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/admin/login");
    }
}
