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
    public class GradesModel : PageModel
    {
        private readonly AppDbContext _context;

        public GradesModel(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Grades = new List<Grade>();
            Students = new List<User>();
            Courses = new List<Course>();
            StudentCourseIds = new Dictionary<int, List<int>>();
        }

        public List<Grade> Grades { get; set; }
        public List<User> Students { get; set; }
        public List<Course> Courses { get; set; }
        public bool IsTeacher { get; set; }
        public int? UserId { get; set; }
        public Dictionary<int, List<int>> StudentCourseIds { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                if (!UserId.HasValue)
                {
                    TempData["Error"] = "Пользователь не авторизован.";
                    Console.WriteLine("OnGetAsync: UserId not found in session.");
                    return RedirectToPage("/Login");
                }

                var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
                IsTeacher = userRole == "teacher";
                Console.WriteLine($"OnGetAsync: UserId={UserId}, Role={userRole}, IsTeacher={IsTeacher}");

                if (IsTeacher)
                {
                    Students = await _context.Users
                        .Where(u => u.Role == "Student")
                        .Where(u => _context.UserSchedules
                            .Any(us => us.UserId == u.Id && _context.Schedules
                                .Any(s => s.Id == us.ScheduleId && s.UserId == UserId.Value)))
                        .ToListAsync();
                    Console.WriteLine($"OnGetAsync: Loaded {Students.Count} students for UserId={UserId}");

                    Courses = await _context.Schedules
                        .Where(s => s.UserId == UserId.Value)
                        .Select(s => s.Course)
                        .Distinct()
                        .ToListAsync();
                    Console.WriteLine($"OnGetAsync: Loaded {Courses.Count} courses for UserId={UserId}");

                    Grades = await _context.Grades
                        .Where(g => _context.Schedules
                            .Any(s => s.CourseId == g.CourseId && s.UserId == UserId.Value))
                        .Include(g => g.Course)
                        .Include(g => g.User)
                        .OrderBy(g => g.DateAssigned)
                        .ToListAsync();
                    Console.WriteLine($"OnGetAsync: Loaded {Grades.Count} grades for teacher UserId={UserId}");

                    // Формирование StudentCourseIds для фильтрации студентов и курсов
                    foreach (var student in Students)
                    {
                        var courseIds = await _context.UserSchedules
                            .Where(us => us.UserId == student.Id)
                            .Join(_context.Schedules,
                                us => us.ScheduleId,
                                s => s.Id,
                                (us, s) => s.CourseId)
                            .Distinct()
                            .ToListAsync();
                        StudentCourseIds[student.Id] = courseIds;
                    }
                    Console.WriteLine($"OnGetAsync: Loaded StudentCourseIds for {StudentCourseIds.Count} students");
                }
                else
                {
                    Grades = await _context.Grades
                        .Where(g => g.UserId == UserId.Value)
                        .Include(g => g.Course)
                        .OrderBy(g => g.DateAssigned)
                        .ToListAsync();
                    Console.WriteLine($"OnGetAsync: Loaded {Grades.Count} grades for student UserId={UserId}");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ошибка при загрузке данных. Попробуйте снова.";
                Console.WriteLine($"OnGetAsync: Exception for UserId={UserId}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Grades = new List<Grade>();
                Students = new List<User>();
                Courses = new List<Course>();
                StudentCourseIds = new Dictionary<int, List<int>>();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddGradeAsync(int UserId, int CourseId, int Score)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
                Console.WriteLine($"OnPostAddGradeAsync: Received UserId={UserId}, CourseId={CourseId}, Score={Score}, Session UserId={userId}, Role={userRole}");

                if (!userId.HasValue || userRole != "teacher")
                {
                    TempData["Error"] = "Только преподаватели могут добавлять оценки.";
                    Console.WriteLine($"OnPostAddGradeAsync: Unauthorized access, UserId={userId}, Role={userRole}");
                    return RedirectToPage("/Login");
                }

                if (UserId <= 0 || CourseId <= 0)
                {
                    TempData["Error"] = "Неверный студент или курс.";
                    Console.WriteLine($"OnPostAddGradeAsync: Invalid UserId={UserId} or CourseId={CourseId}");
                    return await LoadPageDataAndReturnPageAsync();
                }

                if (Score < 0 || Score > 100)
                {
                    TempData["Error"] = "Оценка должна быть от 0 до 100.";
                    Console.WriteLine($"OnPostAddGradeAsync: Invalid score={Score}");
                    return await LoadPageDataAndReturnPageAsync();
                }

                var studentExists = await _context.Users.AnyAsync(u => u.Id == UserId && u.Role == "Student");
                if (!studentExists)
                {
                    TempData["Error"] = "Студент не найден.";
                    Console.WriteLine($"OnPostAddGradeAsync: Student UserId={UserId} not found");
                    return await LoadPageDataAndReturnPageAsync();
                }

                var courseValid = await _context.Schedules.AnyAsync(s => s.CourseId == CourseId && s.UserId == userId.Value);
                if (!courseValid)
                {
                    TempData["Error"] = "Вы не преподаете этот курс.";
                    Console.WriteLine($"OnPostAddGradeAsync: CourseId={CourseId} not taught by UserId={userId}");
                    return await LoadPageDataAndReturnPageAsync();
                }

                var studentEnrolled = await _context.UserSchedules.AnyAsync(us => us.UserId == UserId && us.Schedule.CourseId == CourseId);
                if (!studentEnrolled)
                {
                    TempData["Error"] = "Этот студент не записан на курс.";
                    Console.WriteLine($"OnPostAddGradeAsync: Student UserId={UserId} not enrolled in CourseId={CourseId}");
                    return await LoadPageDataAndReturnPageAsync();
                }

                var existingGrade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.UserId == UserId && g.CourseId == CourseId);
                if (existingGrade != null)
                {
                    existingGrade.Score = Score;
                    existingGrade.DateAssigned = DateTime.Now;
                    Console.WriteLine($"OnPostAddGradeAsync: Updated existing grade: UserId={UserId}, CourseId={CourseId}, New Score={Score}, GradeId={existingGrade.Id}");
                }
                else
                {
                    var grade = new Grade
                    {
                        UserId = UserId,
                        CourseId = CourseId,
                        Score = Score,
                        DateAssigned = DateTime.Now
                    };
                    _context.Grades.Add(grade);
                    Console.WriteLine($"OnPostAddGradeAsync: Added new grade: UserId={UserId}, CourseId={CourseId}, Score={Score}");
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Оценка {Score} успешно добавлена для студента (ID: {UserId}) по курсу (ID: {CourseId}).";
                Console.WriteLine($"OnPostAddGradeAsync: Grade saved successfully for UserId={UserId}, CourseId={CourseId}, Score={Score}");
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ошибка при добавлении оценки. Проверьте данные и попробуйте снова.";
                Console.WriteLine($"OnPostAddGradeAsync: Exception for UserId={UserId}, CourseId={CourseId}, Score={Score}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return await LoadPageDataAndReturnPageAsync();
            }
        }

        private async Task<IActionResult> LoadPageDataAndReturnPageAsync()
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
                IsTeacher = userRole == "teacher";

                if (IsTeacher)
                {
                    Students = await _context.Users
                        .Where(u => u.Role == "Student")
                        .Where(u => _context.UserSchedules
                            .Any(us => us.UserId == u.Id && _context.Schedules
                                .Any(s => s.Id == us.ScheduleId && s.UserId == UserId.Value)))
                        .ToListAsync();
                    Courses = await _context.Schedules
                        .Where(s => s.UserId == UserId.Value)
                        .Select(s => s.Course)
                        .Distinct()
                        .ToListAsync();
                    Grades = await _context.Grades
                        .Where(g => _context.Schedules
                            .Any(s => s.CourseId == g.CourseId && s.UserId == UserId.Value))
                        .Include(g => g.Course)
                        .Include(g => g.User)
                        .OrderBy(g => g.DateAssigned)
                        .ToListAsync();

                    foreach (var student in Students)
                    {
                        var courseIds = await _context.UserSchedules
                            .Where(us => us.UserId == student.Id)
                            .Join(_context.Schedules,
                                us => us.ScheduleId,
                                s => s.Id,
                                (us, s) => s.CourseId)
                            .Distinct()
                            .ToListAsync();
                        StudentCourseIds[student.Id] = courseIds;
                    }
                }
                else
                {
                    Grades = await _context.Grades
                        .Where(g => g.UserId == UserId.Value)
                        .Include(g => g.Course)
                        .OrderBy(g => g.DateAssigned)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadPageDataAndReturnPageAsync: Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
                StudentCourseIds = new Dictionary<int, List<int>>();
            }
            return Page();
        }
    }
}