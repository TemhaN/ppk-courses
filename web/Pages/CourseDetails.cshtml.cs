using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using web.Data;
using web.Models;

namespace web.Pages
{
    public class CourseDetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public CourseDetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public Course Course { get; set; }
        public int? UserId { get; set; }
        public bool IsCoursePurchased { get; set; }
        public bool IsTeacher { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
                IsTeacher = userRole == "teacher";

                Course = await _context.Courses
                    .Include(c => c.Schedules)
                    .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (Course == null)
                {
                    TempData["Error"] = "Курс не найден, проверьте данные.";
                    return RedirectToPage("/Schedule");
                }

                if (UserId.HasValue)
                {
                    IsCoursePurchased = await _context.UserCourses
                        .AnyAsync(uc => uc.UserId == UserId.Value && uc.CourseId == id);
                }
                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Произошла ошибка при загрузке курса.";
                return RedirectToPage("/Schedule");
            }
        }
    }
}