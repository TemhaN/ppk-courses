using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using web.Data;
using web.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace web.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;

        public RegisterModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User User { get; set; }
        public List<Group> Groups { get; set; }
        public List<Course> Courses { get; set; }
        public bool ShowPaymentModal { get; set; }
        public string SelectedCourseName { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Groups = await _context.Groups.ToListAsync();
            Courses = await _context.Courses.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int courseId)
        {
            Groups = await _context.Groups.ToListAsync();
            Courses = await _context.Courses.ToListAsync();

            if (!ModelState.IsValid)
                return Page();

            if (User.Role == "Student" && string.IsNullOrEmpty(User.FullName))
            {
                ModelState.AddModelError("", "Укажите полное имя пользователя");
                return Page();
            }

            if (courseId <= 0 || !await _context.Courses.AnyAsync(c => c.Id == courseId))
            {
                ViewData["CourseError"] = "Выбран недопустимый курс";
                return Page();
            }

            if (await _context.Users.AnyAsync(u => u.Username == User.Username))
            {
                ModelState.AddModelError("User.Username", "Имя пользователя уже занято");
                return Page();
            }
            if (await _context.Users.AnyAsync(u => u.Email == User.Email))
            {
                ModelState.AddModelError("User.Email", "Email уже зарегистрирован");
                return Page();
            }

            try
            {
                User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(User.PasswordHash);
                User.Role = "Student";

                _context.Users.Add(User);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("UserId", User.Id);
                HttpContext.Session.SetString("UserRole", User.Role.ToLower());

                TempData["SelectedCourseId"] = courseId;
                var selectedCourse = await _context.Courses.FindAsync(courseId);
                SelectedCourseName = selectedCourse?.CourseName ?? "Неизвестный курс";

                var schedule = await _context.Schedules
                    .Where(s => s.CourseId == courseId && s.Date >= DateTime.Today)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.Time)
                    .FirstOrDefaultAsync();

                if (schedule != null)
                {
                    var existing = await _context.UserSchedules
                        .AnyAsync(us => us.UserId == User.Id && us.ScheduleId == schedule.Id);
                    if (!existing)
                    {
                        _context.UserSchedules.Add(new UserSchedule
                        {
                            UserId = User.Id,
                            ScheduleId = schedule.Id
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    TempData["Warning"] = "Расписание для курса не найдено. Вы записаны условно.";
                }

                ShowPaymentModal = true;
                return Page();
            }
            catch
            {
                ModelState.AddModelError("", "Ошибка при регистрации. Попробуйте снова.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostPayAsync(int userId, int courseId, string cardNumber, string cardHolder, string expiryMonth, string expiryYear, string cvv)
        {
            Groups = await _context.Groups.ToListAsync();
            Courses = await _context.Courses.ToListAsync();

            var user = await _context.Users.FindAsync(userId);
            var course = await _context.Courses.FindAsync(courseId);
            if (user == null || course == null)
            {
                ViewData["PaymentError"] = "Пользователь или курс не найдены.";
                ShowPaymentModal = true;
                return Page();
            }

            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length != 16 || !cardNumber.All(char.IsDigit))
            {
                ViewData["PaymentError"] = "Номер карты должен состоять из 16 цифр.";
                ShowPaymentModal = true;
                return Page();
            }
            if (string.IsNullOrEmpty(cardHolder) || cardHolder.Length < 2)
            {
                ViewData["PaymentError"] = "Укажите корректное имя владельца карты.";
                ShowPaymentModal = true;
                return Page();
            }
            if (string.IsNullOrEmpty(expiryMonth) || string.IsNullOrEmpty(expiryYear) ||
                !int.TryParse(expiryMonth, out int month) || !int.TryParse(expiryYear, out int year) ||
                month < 1 || month > 12 || year < 25 || year > 99)
            {
                ViewData["PaymentError"] = "Укажите корректный месяц (01–12) или год (25–99).";
                ShowPaymentModal = true;
                return Page();
            }
            var currentYear = DateTime.Now.Year % 100;
            var currentMonth = DateTime.Now.Month;
            if (year < currentYear || (year == currentYear && month < currentMonth))
            {
                ViewData["PaymentError"] = "Срок действия карты истек.";
                ShowPaymentModal = true;
                return Page();
            }
            if (string.IsNullOrEmpty(cvv) || cvv.Length != 3 || !cvv.All(char.IsDigit))
            {
                ViewData["PaymentError"] = "CVV должен состоять из 3 цифр.";
                ShowPaymentModal = true;
                return Page();
            }

            _context.UserCourses.Add(new UserCourse
            {
                UserId = userId,
                CourseId = courseId,
                PurchaseDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Оплата прошла успешно! Курс {course.CourseName} добавлен.";
            return RedirectToPage("/Index");
        }
    }
}