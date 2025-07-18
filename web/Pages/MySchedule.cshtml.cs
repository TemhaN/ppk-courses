using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using web.Data;
using web.Models;

namespace web.Pages
{
    public class MyScheduleModel : PageModel
    {
        private readonly AppDbContext _context;

        public MyScheduleModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Schedule> Schedules { get; set; } = new List<Schedule>();
        public List<Group> Groups { get; set; } = new List<Group>();
        public int? UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? GroupId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Date { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                if (!UserId.HasValue)
                {
                    TempData["Error"] = "Пользователь не авторизован.";
                    return RedirectToPage("/Login");
                }

                Groups = await _context.Groups.ToListAsync() ?? new List<Group>();

                if (Date.HasValue && Date.Value < DateTime.Today)
                {
                    ModelState.AddModelError("Date", "Выберите дату не ранее сегодняшнего дня");
                    return Page();
                }

                var query = _context.UserSchedules
                    .Where(us => us.UserId == UserId.Value)
                    .Include(us => us.Schedule)
                    .ThenInclude(s => s.Course)
                    .Include(us => us.Schedule)
                    .ThenInclude(s => s.User)
                    .Select(us => us.Schedule)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    query = query.Where(s => s.Course.CourseName.Contains(SearchQuery) || s.User.FullName.Contains(SearchQuery));
                }

                if (GroupId.HasValue)
                {
                    query = query.Join(_context.GroupSchedules,
                        s => s.Id,
                        gs => gs.ScheduleId,
                        (s, gs) => new { Schedule = s, GroupSchedule = gs })
                        .Where(x => x.GroupSchedule.GroupId == GroupId.Value)
                        .Select(x => x.Schedule);
                }

                if (Date.HasValue)
                {
                    query = query.Where(s => s.Date == Date.Value.Date);
                }

                Schedules = await query.OrderBy(s => s.Date)
                                      .ThenBy(s => s.Time)
                                      .ToListAsync() ?? new List<Schedule>();

                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка загрузки расписания: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                if (!UserId.HasValue)
                {
                    TempData["Error"] = "Пользователь не авторизован.";
                    return RedirectToPage("/Login");
                }

                var query = _context.UserSchedules
                    .Where(us => us.UserId == UserId.Value)
                    .Include(us => us.Schedule)
                    .ThenInclude(s => s.Course)
                    .Include(us => us.Schedule)
                    .ThenInclude(s => s.User)
                    .Select(us => us.Schedule)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    query = query.Where(s => s.Course.CourseName.Contains(SearchQuery) || s.User.FullName.Contains(SearchQuery));
                }

                if (GroupId.HasValue)
                {
                    query = query.Join(_context.GroupSchedules,
                        s => s.Id,
                        gs => gs.ScheduleId,
                        (s, gs) => new { Schedule = s, GroupSchedule = gs })
                        .Where(x => x.GroupSchedule.GroupId == GroupId.Value)
                        .Select(x => x.Schedule);
                }

                if (Date.HasValue)
                {
                    query = query.Where(s => s.Date == Date.Value.Date);
                }

                var schedules = await query.OrderBy(s => s.Date)
                                          .ThenBy(s => s.Time)
                                          .ToListAsync();

                if (!schedules.Any())
                {
                    TempData["Error"] = "Нет данных для экспорта.";
                    return Page();
                }

                var groupSchedules = await _context.GroupSchedules
                    .Include(gs => gs.Group)
                    .Where(gs => schedules.Select(s => s.Id).Contains(gs.ScheduleId))
                    .ToListAsync();

                var grades = await _context.Grades
                    .Where(g => g.UserId == UserId.Value && schedules.Select(s => s.CourseId).Contains(g.CourseId))
                    .ToListAsync();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Мое расписание");

                    worksheet.Cells[1, 1].Value = "Дата";
                    worksheet.Cells[1, 2].Value = "Время";
                    worksheet.Cells[1, 3].Value = "Курс";
                    worksheet.Cells[1, 6].Value = "Преподаватель";
                    worksheet.Cells[1, 8].Value = "Аудитория";
                    worksheet.Cells[1, 9].Value = "Группы";
                    worksheet.Cells[1, 10].Value = "Оценка";
                    worksheet.Cells[1, 11].Value = "Дата оценки";

                    using (var range = worksheet.Cells[1, 1, 1, 9])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Font.Name = "Calibri";
                        range.Style.Font.Size = 12;
                    }

                    using (var range = worksheet.Cells[1, 1, 1, 11])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Font.Size = 12;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    for (int i = 0; i < schedules.Count; i++)
                    {
                        var schedule = schedules[i];

                        worksheet.Cells[i + 2, 1].Value = schedule.Date.ToString("dd.MM.yyyy");
                        worksheet.Cells[i + 2, 2].Value = schedule.Time.ToString(@"hh\:mm");
                        worksheet.Cells[i + 2, 3].Value = schedule.Course?.CourseName ?? "Не указан";
                        worksheet.Cells[i + 2, 6].Value = schedule.User?.FullName ?? "Не указан";
                        worksheet.Cells[i + 2, 8].Value = schedule.Classroom ?? "Не указана";

                        var groups = groupSchedules
                            .Where(gs => gs.ScheduleId == schedule.Id)
                            .Select(gs => gs.Group?.Name)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .ToList();
                        worksheet.Cells[i + 2, 9].Value = groups.Any() ? string.Join(", ", groups) : "Без групп";

                        var grade = grades.FirstOrDefault(g => g.CourseId == schedule.CourseId);
                        worksheet.Cells[i + 2, 10].Value = grade != null ? grade.Score.ToString() : "-";
                        worksheet.Cells[i + 2, 11].Value = grade != null ? grade.DateAssigned.ToString("dd.MM.yyyy") : "-";

                        using (var range = worksheet.Cells[i + 2, 1, i + 2, 11])
                        {
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }
                    }

                    worksheet.Cells.AutoFitColumns();
                    worksheet.View.FreezePanes(2, 1);

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    Response.Headers.Add("Content-Disposition", "attachment; filename=MySchedule.xlsx");
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MySchedule.xlsx");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при экспорте: {ex.Message}";
                return Page();
            }
        }
    }
}