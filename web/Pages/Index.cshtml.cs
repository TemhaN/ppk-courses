using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using web.Data;
using web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public int? UserId { get; set; }
        public string Message { get; set; }
        public List<Course> Courses { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            UserId = HttpContext.Session.GetInt32("UserId");
            Message = UserId.HasValue ? $"Добро пожаловать, пользователь с ID: {UserId}" : "Вы не авторизованы.";

            var query = _context.Courses.AsQueryable();
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(c => c.CourseName.Contains(SearchTerm) || c.Description.Contains(SearchTerm));
            }
            Courses = await query.Take(6).ToListAsync();
        }
    }
}