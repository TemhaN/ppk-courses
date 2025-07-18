using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using web.Data;
using web.Models;
using System.Net.Http.Headers;
using Models = web.Models;

namespace web.Pages
{
    public class TeacherDashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public TeacherDashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public int? UserId { get; set; }
        public bool IsTeacher { get; set; }
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Models.Group> Groups { get; set; } = new List<Models.Group>();
        public int TotalStudents { get; set; }
        public double AverageGrade { get; set; }
        public double AttendancePercentage { get; set; }
        public List<StudentGradeInfo> StudentGrades { get; set; } = new List<StudentGradeInfo>();
        public List<CourseStat> CourseStats { get; set; } = new List<CourseStat>();
        public List<AttendanceStat> AttendanceStats { get; set; } = new List<AttendanceStat>();
        public List<TopStudent> TopStudents { get; set; } = new List<TopStudent>();

        [BindProperty(SupportsGet = true)]
        public int? CourseId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? GroupId { get; set; }

        public class StudentGradeInfo
        {
            public string StudentName { get; set; }
            public string GroupName { get; set; }
            public string CourseName { get; set; }
            public int Score { get; set; }
            public DateTime DateAssigned { get; set; }
        }

        public class CourseStat
        {
            public string CourseName { get; set; }
            public double AverageGrade { get; set; }
            public int StudentCount { get; set; }
            public double AttendanceRate { get; set; }
        }

        public class AttendanceStat
        {
            public string GroupName { get; set; }
            public int Present { get; set; }
            public int Absent { get; set; }
            public double AttendanceRate { get; set; }
        }

        public class TopStudent
        {
            public string StudentName { get; set; }
            public string GroupName { get; set; }
            public double AverageGrade { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
                IsTeacher = userRole == "teacher";

                if (!UserId.HasValue || !IsTeacher)
                {
										TempData["Error"] = "Требуется вход для преподавателя.";
                    return RedirectToPage("/Index");
                }

								// Получение курсов, связанных с преподавателем
                Courses = await _context.Schedules
                    .Where(s => s.UserId == UserId.Value)
                    .Select(s => s.Course)
                    .Distinct()
                    .ToListAsync();


                // Получение групп, связанных с расписанием
                Groups = await _context.GroupSchedules
                    .Where(gs => _context.Schedules
                        .Any(s => s.Id == gs.ScheduleId && s.UserId == UserId.Value))
                    .Select(gs => gs.Group)
                    .Distinct()
                    .ToListAsync();

                // Запрос оценок
                var gradesQuery = _context.Grades
                    .Include(g => g.User)
                    .ThenInclude(u => u.Group)
                    .Include(g => g.Course)
                    .Where(g => _context.Schedules
                        .Any(s => s.CourseId == g.CourseId && s.UserId == UserId.Value));

                // Фильтрация
                if (CourseId.HasValue)
                {
                    gradesQuery = gradesQuery.Where(g => g.CourseId == CourseId.Value);
                }
                if (GroupId.HasValue)
                {
                    gradesQuery = gradesQuery.Where(g => g.User.GroupId == GroupId.Value);
                }

                // Список оценок
                StudentGrades = await gradesQuery
                    .Select(g => new StudentGradeInfo
                    {
                        StudentName = g.User.FullName,
                        GroupName = g.User.Group != null ? g.User.Group.Name : null,
                        CourseName = g.Course.CourseName,
                        Score = g.Score,
                        DateAssigned = g.DateAssigned
                    })
                    .OrderBy(g => g.StudentName)
                    .ThenBy(g => g.DateAssigned)
                    .ToListAsync();

                // Общая статистика
                TotalStudents = await _context.UserSchedules
                    .Where(us => _context.Schedules
                        .Any(s => s.Id == us.ScheduleId && s.UserId == UserId.Value))
                    .Select(us => us.UserId)
                    .Distinct()
                    .CountAsync();

                AverageGrade = StudentGrades.Any() ? StudentGrades.Average(g => g.Score) : 0;

                // Посещаемость
                var attendanceQuery = _context.Attendances
                    .Where(a => _context.Schedules
                        .Any(s => s.Id == a.ScheduleId && s.UserId == UserId.Value));

                var totalAttendances = await attendanceQuery.CountAsync();
                var presentAttendances = await attendanceQuery
                    .Where(a => a.IsPresent)
                    .CountAsync();
                AttendancePercentage = totalAttendances > 0 ? (presentAttendances * 100.0 / totalAttendances) : 0;

                // Статистика по курсам
                CourseStats = await _context.Grades
                    .Where(g => _context.Schedules
                        .Any(s => s.CourseId == g.CourseId && s.UserId == UserId.Value))
                    .GroupBy(g => g.Course)
                    .Select(g => new CourseStat
                    {
                        CourseName = g.Key.CourseName,
                        AverageGrade = g.Average(x => x.Score),
                        StudentCount = g.Select(x => x.UserId).Distinct().Count(),
                        AttendanceRate = _context.Attendances
                            .Where(a => a.IsPresent &&
                                _context.Schedules.Any(s =>
                                    s.Id == a.ScheduleId &&
                                    s.CourseId == g.Key.Id &&
                                    s.UserId == UserId.Value))
                            .Count() * 100.0 /
                            (_context.Attendances
                                .Where(a => _context.Schedules.Any(s =>
                                    s.Id == a.ScheduleId &&
                                    s.CourseId == g.Key.Id &&
                                    s.UserId == UserId.Value))
                            .Count() + 1)
                    })
                    .OrderBy(cs => cs.CourseName)
                    .ToListAsync();

                // Статистика посещаемости по группам
                AttendanceStats = await _context.Attendances
                    .Where(a => _context.Schedules
                        .Any(s => s.Id == a.ScheduleId && s.UserId == UserId.Value))
                    .GroupBy(a => a.User.Group)
                    .Select(g => new AttendanceStat
                    {
                        GroupName = g.Key.Name,
                        Present = g.Count(a => a.IsPresent),
                        Absent = g.Count(a => !a.IsPresent),
                        AttendanceRate = g.Count() > 0 ? (g.Count(a => a.IsPresent) * 100.0 / g.Count()) : 0
                    })
                    .OrderByDescending(a => a.AttendanceRate)
                    .ToListAsync();

                // Лучшие студенты (топ-5)
                TopStudents = await _context.Grades
                    .Where(g => _context.Schedules
                        .Any(s => s.CourseId == g.CourseId && s.UserId == UserId.Value))
                    .GroupBy(g => new { g.User.FullName, g.User.Group.Name })
                    .Select(g => new TopStudent
                    {
                        StudentName = g.Key.FullName,
                        GroupName = g.Key.Name,
                        AverageGrade = g.Average(x => x.Score)
                    })
                    .OrderByDescending(s => s.AverageGrade)
                    .Take(5)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка загрузки данных: {ex.Message}";
                return Page();
            }
        }

		public async Task<IActionResult> OnPostExportToExcelAsync()
    {
        try
        {
            UserId = HttpContext.Session.GetInt32("UserId");
            if (!UserId.HasValue)
            {
                TempData["Error"] = "Пользователь не авторизован.";
                return Forbid();
            }


            var gradesQuery = _context.Grades
                .Include(g => g.User)
                .ThenInclude(u => u.Group)
                .Include(g => g.Course)
                .Where(g => _context.Schedules
                    .Any(s => s.CourseId == g.CourseId && s.UserId == UserId.Value));

            if (CourseId.HasValue)
            {
                gradesQuery = gradesQuery.Where(g => g.CourseId == CourseId.Value);
            }
            if (GroupId.HasValue)
            {
                gradesQuery = gradesQuery.Where(g => g.User.GroupId == GroupId.Value);
            }

            var grades = await gradesQuery
                .Select(g => new
                {
                    g.User.FullName,
                    GroupName = g.User.Group != null ? g.User.Group.Name : "Без группы",
                    g.Course.CourseName,
                    g.Score,
                    g.DateAssigned
                })
                .ToListAsync();

								Console.WriteLine($"Получено записей для экспорта: {grades.Count}");
                if (!grades.Any())
                {
                    TempData["Error"] = "Нет данных для экспорта.";
                    return Page();
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Оценки студентов");

								worksheet.Cells[1, 1].Value = "Студент";
                worksheet.Cells[1, 2].Value = "Группа";
                worksheet.Cells[1, 3].Value = "Курс";
                worksheet.Cells[1, 4].Value = "Оценка";
                worksheet.Cells[1, 5].Value = "Дата";

            for (var i = 0; i < grades.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = grades[i].FullName;
                worksheet.Cells[i + 2, 2].Value = grades[i].GroupName;
                worksheet.Cells[i + 2, 3].Value = grades[i].CourseName;
                worksheet.Cells[i + 2, 4].Value = grades[i].Score;
                worksheet.Cells[i + 2, 5].Value = grades[i].DateAssigned.ToString("dd.MM.yyyy");
            }

            worksheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;
            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Оценки_студентов_{DateTime.Now:yyyyMMdd}.xlsx";
            var contentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileNameStar = fileName
            };
            Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

            Console.WriteLine("Файл успешно создан, возвращается ответ");
            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
								Console.WriteLine($"Ошибка экспорта: {ex.Message}\n{ex.StackTrace}");
                TempData["Error"] = $"Ошибка при экспорте: {ex.Message}";
                return Page();
        }
    }
}
}