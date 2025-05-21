using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.DTOs;
using Valuator.Utils;

namespace Valuator.Pages
{
    public class RegistrationModel : PageModel
    {
        [BindProperty]
        public CreateUserDto Input { get; set; }
        private IShardManager _shardManager;
        private IPasswordHasher _passwordHasher;

        public RegistrationModel(IShardManager shardManager, IPasswordHasher passwordHasher)
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
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var userExists = await _shardManager.UserExists(Input.Username);
            if (userExists)
            {
                ModelState.AddModelError(string.Empty, "Username or email already exists.");
                return Page();
            }

            var user = new Models.User
            {
                Username = Input.Username,
                Password = _passwordHasher.Hash(Input.Password)
            };

            await _shardManager.AddUser(user);

            return Redirect("/auth");
        }
    }
}
