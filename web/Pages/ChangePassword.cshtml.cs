using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using web.Data;
using web.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace web.Pages
{
    public class ChangePasswordModel : PageModel
    {
        private readonly AppDbContext _context;

        public ChangePasswordModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Старый пароль обязателен")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Новый пароль обязателен")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Новый пароль должен быть от 6 до 100 символов")]
        public string NewPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }

        public IActionResult OnGet()
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToPage("/Login");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null || !BCrypt.Net.BCrypt.Verify(OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError("OldPassword", "Неверный старый пароль");
                return Page();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Пароль успешно изменен";
            return RedirectToPage("/Index");
        }
    }
}