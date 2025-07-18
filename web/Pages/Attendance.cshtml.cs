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
    public class AttendanceModel : PageModel
    {
        private readonly AppDbContext _context;

        public AttendanceModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Attendance> Attendances { get; set; } = new List<Attendance>();
        public List<User> Students { get; set; } = new List<User>();
        public List<Schedule> Schedules { get; set; } = new List<Schedule>();
        public List<Group> Groups { get; set; } = new List<Group>();
        public List<Course> Courses { get; set; } = new List<Course>();
        public bool IsTeacher { get; set; }
        public AttendanceStats AttendanceStats { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
            IsTeacher = userRole == "teacher";

            if (!userId.HasValue)
            {
                TempData["Error"] = "Необходимо войти в систему.";
                return RedirectToPage("/Login");
            }

            Groups = await _context.Groups.ToListAsync() ?? new List<Group>();
            Courses = await _context.Courses
                .Where(c => IsTeacher ? _context.Schedules.Any(s => s.CourseId == c.Id && s.UserId == userId.Value) : true)
                .ToListAsync() ?? new List<Course>();

            if (IsTeacher)
            {
                Schedules = await _context.Schedules
                    .Include(s => s.Course)
                    .Where(s => s.UserId == userId.Value)
                    .ToListAsync() ?? new List<Schedule>();

                Students = await _context.Users
                    .Where(u => u.Role == "Student")
                    .Where(u => _context.UserSchedules
                        .Any(us => us.UserId == u.Id && _context.Schedules
                            .Any(s => s.Id == us.ScheduleId && s.UserId == userId.Value)))
                    .ToListAsync() ?? new List<User>();

                Attendances = await _context.Attendances
                    .Include(a => a.User)
                    .Include(a => a.Schedule)
                    .ThenInclude(s => s.Course)
                    .Where(a => _context.Schedules.Any(s => s.Id == a.ScheduleId && s.UserId == userId.Value))
                    .OrderBy(a => a.Date)
                    .ToListAsync() ?? new List<Attendance>();

                AttendanceStats = CalculateStats(Attendances);
            }
            else
            {
                Attendances = await _context.Attendances
                    .Include(a => a.User)
                    .Include(a => a.Schedule)
                    .ThenInclude(s => s.Course)
                    .Where(a => a.UserId == userId.Value)
                    .OrderBy(a => a.Date)
                    .ToListAsync() ?? new List<Attendance>();

                AttendanceStats = CalculateStats(Attendances);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostMarkAttendanceAsync(int ScheduleId, int UserId, bool IsPresent)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();

            if (!userId.HasValue || userRole != "teacher")
            {
                TempData["Error"] = "Только преподаватель может отмечать посещаемость.";
                return RedirectToPage("/Login");
            }

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.Id == ScheduleId && s.UserId == userId.Value);
            if (schedule == null)
            {
                TempData["Error"] = "Расписание не найдено.";
                return Page();
            }

            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == UserId && u.Role == "Student");
            if (student == null)
            {
                TempData["Error"] = "Студент не найден.";
                return Page();
            }

            var existingAttendance = await _context.Attendances
                .AnyAsync(a => a.UserId == UserId && a.ScheduleId == ScheduleId);
            if (existingAttendance)
            {
                TempData["Error"] = "Посещаемость уже отмечена.";
                return Page();
            }

            _context.Attendances.Add(new Attendance
            {
                UserId = UserId,
                ScheduleId = ScheduleId,
                Date = DateTime.Now,
                IsPresent = IsPresent
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Посещаемость успешно отмечена.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFilterAsync(int? GroupId, int? CourseId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
            IsTeacher = userRole == "teacher";

            if (!userId.HasValue)
            {
                TempData["Error"] = "Необходимо войти в систему.";
                return RedirectToPage("/Login");
            }

            Groups = await _context.Groups.ToListAsync() ?? new List<Group>();
            Courses = await _context.Courses
                .Where(c => IsTeacher ? _context.Schedules.Any(s => s.CourseId == c.Id && s.UserId == userId.Value) : true)
                .ToListAsync() ?? new List<Course>();

            var query = _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Schedule)
                .ThenInclude(s => s.Course)
                .AsQueryable();

            if (userRole == "teacher")
            {
                query = query.Where(a => _context.Schedules.Any(s => s.Id == a.ScheduleId && s.UserId == userId.Value));
            }
            else if (userRole == "student")
            {
                query = query.Where(a => a.UserId == userId.Value);
            }

            if (GroupId.HasValue)
            {
                query = query.Where(a => a.User.GroupId == GroupId.Value);
            }

            if (CourseId.HasValue)
            {
                query = query.Where(a => a.Schedule.CourseId == CourseId.Value);
            }

            Attendances = await query.OrderBy(a => a.Date).ToListAsync() ?? new List<Attendance>();
            AttendanceStats = CalculateStats(Attendances);

            if (IsTeacher)
            {
                Schedules = await _context.Schedules
                    .Include(s => s.Course)
                    .Where(s => s.UserId == userId.Value)
                    .ToListAsync() ?? new List<Schedule>();

                Students = await _context.Users
                    .Where(u => u.Role == "Student")
                    .Where(u => _context.UserSchedules
                        .Any(us => us.UserId == u.Id && _context.Schedules
                            .Any(s => s.Id == us.ScheduleId && s.UserId == userId.Value)))
                    .ToListAsync() ?? new List<User>();
            }

            return Page();
        }

        private AttendanceStats CalculateStats(List<Attendance> attendances)
        {
            return new AttendanceStats
            {
                Present = attendances.Count(a => a.IsPresent),
                Absent = attendances.Count(a => !a.IsPresent)
            };
        }
    }
}