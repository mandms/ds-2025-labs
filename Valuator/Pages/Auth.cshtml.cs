using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Valuator.DTOs;

namespace Valuator.Pages
{
    public class AuthModel : PageModel
    {
        [BindProperty]
        public LoginUserDto Input { get; set; }
        private IShardManager _shardManager;
        private Utils.IPasswordHasher _passwordHasher;
        public string? ErrorMessage { get; set; }

        public AuthModel(IShardManager shardManager, Utils.IPasswordHasher passwordHasher)
        {
            _shardManager = shardManager;
            _passwordHasher = passwordHasher;
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect("/");
            }
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect("/");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _shardManager.GetUser(Input.Username);

            if (user is null)
            {
                ErrorMessage = "User with username or password does not exists";
                return Page();
            }

            if (!_passwordHasher.Verify(Input.Password, user.Password))
            {
                ErrorMessage = "User with username or password does not exists";
                return Page();
            }

            var claims = new List<Claim> { new(ClaimTypes.Name, user.Username) };
            // создаем объект ClaimsIdentity
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return Redirect("/");
        }
    }
}
