using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web.Data;
using web.Models;

namespace web.Pages
{
    public class StudentDashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public StudentDashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public int? UserId { get; set; }
        public bool IsStudent { get; set; }
        public string StudentName { get; set; }
        public string GroupName { get; set; }
        public double AverageGrade { get; set; }
        public double CourseProgress { get; set; }
        public List<StudentCourseInfo> Courses { get; set; } = new List<StudentCourseInfo>();
        public List<StudentGradeInfo> Grades { get; set; } = new List<StudentGradeInfo>();

        public class StudentCourseInfo
        {
            public string CourseName { get; set; }
            public string TeacherName { get; set; }
            public Schedule NextSchedule { get; set; }
        }

        public class StudentGradeInfo
        {
            public string CourseName { get; set; }
            public int Score { get; set; }
            public DateTime DateAssigned { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
                IsStudent = userRole == "student";

                if (!UserId.HasValue || !IsStudent)
                {
                    TempData["Error"] = "Требуется вход для студента.";
                    return RedirectToPage("/Index");
                }

                // Информация о студенте
                var user = await _context.Users
                    .Include(u => u.Group)
                    .FirstOrDefaultAsync(u => u.Id == UserId.Value);
                if (user == null)
                {
                    TempData["Error"] = "Пользователь не найден.";
                    return RedirectToPage("/Index");
                }

                StudentName = user.FullName;
                GroupName = user.Group?.Name;

                // Оценки
                Grades = await _context.Grades
                    .Include(g => g.Course)
                    .Where(g => g.UserId == UserId.Value)
                    .Select(g => new StudentGradeInfo
                    {
                        CourseName = g.Course.CourseName,
                        Score = g.Score,
                        DateAssigned = g.DateAssigned
                    })
                    .OrderBy(g => g.DateAssigned)
                    .ToListAsync();

                AverageGrade = Grades.Any() ? Grades.Average(g => g.Score) : 0;

                // Курсы и расписание
                Courses = await _context.UserCourses
                    .Include(uc => uc.Course)
                    .ThenInclude(c => c.Schedules)
                    .ThenInclude(s => s.User)
                    .Where(uc => uc.UserId == UserId.Value)
                    .Select(uc => new StudentCourseInfo
                    {
                        CourseName = uc.Course.CourseName,
                        TeacherName = uc.Course.Schedules.FirstOrDefault().User == null
                            ? null
                            : uc.Course.Schedules.FirstOrDefault().User.FullName,
                        NextSchedule = uc.Course.Schedules
                            .Where(s => s.Date >= DateTime.Today)
                            .OrderBy(s => s.Date)
                            .ThenBy(s => s.Time)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // Прогресс курса (процент завершенных занятий)
                var totalSchedules = await _context.UserSchedules
                    .Include(us => us.Schedule)
                    .Where(us => us.UserId == UserId.Value)
                    .CountAsync();
                var pastSchedules = await _context.UserSchedules
                    .Include(us => us.Schedule)
                    .Where(us => us.UserId == UserId.Value && us.Schedule.Date < DateTime.Today)
                    .CountAsync();
                CourseProgress = totalSchedules > 0 ? (pastSchedules * 100.0 / totalSchedules) : 0;

                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка загрузки данных: {ex.Message}";
                return Page();
            }
        }
    }
}