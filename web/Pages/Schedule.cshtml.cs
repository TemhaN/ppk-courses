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
    public class ScheduleModel : PageModel
    {
        private readonly AppDbContext _context;

        public ScheduleModel(AppDbContext context)
        {
            _context = context;
        }

        public bool IsTeacher { get; set; }
        public List<Schedule> Schedules { get; set; } = new List<Schedule>();
        public List<Group> Groups { get; set; } = new List<Group>();
        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? GroupId { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? Date { get; set; }
        public int? UserId { get; set; }
        public bool ShowPaymentModal { get; set; }
        public string SelectedCourseName { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
                IsTeacher = userRole == "teacher";

                Groups = await _context.Groups.ToListAsync() ?? new List<Group>();

                if (Date.HasValue && Date.Value < DateTime.Today)
                {
                    ModelState.AddModelError("Date", "Выберите дату не ранее сегодняшнего дня");
                    return Page();
                }

                var query = _context.Schedules
                    .Include(s => s.User)
                    .Include(s => s.Course)
                    .AsQueryable();

                // Фильтрация по поиску для преподавателя
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    query = query.Where(s =>
                        s.Course.CourseName.Contains(SearchQuery) ||
                        s.User.FullName.Contains(SearchQuery));
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

        public bool IsEnrolled(int scheduleId)
        {
            if (!UserId.HasValue) return false;
            return _context.UserSchedules
                .Any(us => us.UserId == UserId.Value && us.ScheduleId == scheduleId);
        }

        public async Task<IActionResult> OnPostEnrollAsync(int scheduleId)
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
                if (!UserId.HasValue)
                {
                    TempData["Error"] = "Пользователь не авторизован.";
                    return RedirectToPage("/Login");
                }

                if (userRole == "teacher")
                {
                    TempData["Error"] = "Преподаватели не могут записываться на курсы.";
                    return RedirectToPage();
                }

                var schedule = await _context.Schedules
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.Id == scheduleId);
                if (schedule == null)
                {
                    TempData["Error"] = "Расписание не найдено.";
                    return RedirectToPage();
                }

                var existing = await _context.UserSchedules
                    .AnyAsync(us => us.UserId == UserId.Value && us.ScheduleId == scheduleId);
                if (!existing)
                {
                    _context.UserSchedules.Add(new UserSchedule
                    {
                        UserId = UserId.Value,
                        ScheduleId = scheduleId
                    });
                    await _context.SaveChangesAsync();
                }

                TempData["SelectedScheduleId"] = scheduleId;
                SelectedCourseName = schedule.Course?.CourseName ?? "Неизвестный курс";
                ShowPaymentModal = true;

                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при записи: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostPayAsync(int userId, int scheduleId, string cardNumber, string cardHolder, string expiryMonth, string expiryYear, string cvv)
        {
            try
            {
                UserId = HttpContext.Session.GetInt32("UserId");
                if (!UserId.HasValue || UserId.Value != userId)
                {
                    TempData["Error"] = "Пользователь не авторизован.";
                    return RedirectToPage("/Login");
                }

                var schedule = await _context.Schedules
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.Id == scheduleId);
                if (schedule == null)
                {
                    TempData["Error"] = "Расписание не найдено.";
                    return RedirectToPage();
                }

                if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length != 16 || !cardNumber.All(char.IsDigit))
                {
                    ViewData["PaymentError"] = "Номер карты должен состоять из 16 цифр";
                    ShowPaymentModal = true;
                    SelectedCourseName = schedule.Course?.CourseName ?? "Неизвестный курс";
                    TempData["SelectedScheduleId"] = scheduleId;
                    return Page();
                }
                if (string.IsNullOrEmpty(cardHolder) || cardHolder.Length < 2)
                {
                    ViewData["PaymentError"] = "Некорректное имя владельца карты";
                    ShowPaymentModal = true;
                    SelectedCourseName = schedule.Course?.CourseName ?? "Неизвестный курс";
                    TempData["SelectedScheduleId"] = scheduleId;
                    return Page();
                }
                if (string.IsNullOrEmpty(expiryMonth) || string.IsNullOrEmpty(expiryYear) ||
                    !int.TryParse(expiryMonth, out int month) || !int.TryParse(expiryYear, out int year) ||
                    month < 1 || month > 12 || year < 25 || year > 99)
                {
                    ViewData["PaymentError"] = "Укажите корректный месяц (01–12) или год (25–99)";
                    ShowPaymentModal = true;
                    SelectedCourseName = schedule.Course?.CourseName ?? "Неизвестный курс";
                    TempData["SelectedScheduleId"] = scheduleId;
                    return Page();
                }
                var currentYear = DateTime.Now.Year % 100;
                var currentMonth = DateTime.Now.Month;
                if (year < currentYear || (year == currentYear && month < currentMonth))
                {
                    ViewData["PaymentError"] = "Срок действия карты истек";
                    ShowPaymentModal = true;
                    SelectedCourseName = schedule.Course?.CourseName ?? "Неизвестный курс";
                    TempData["SelectedScheduleId"] = scheduleId;
                    return Page();
                }
                if (string.IsNullOrEmpty(cvv) || cvv.Length != 3 || !cvv.All(char.IsDigit))
                {
                    ViewData["PaymentError"] = "CVV должен состоять из 3 цифр";
                    ShowPaymentModal = true;
                    SelectedCourseName = schedule.Course?.CourseName ?? "Неизвестный курс";
                    TempData["SelectedScheduleId"] = scheduleId;
                    return Page();
                }

                _context.UserCourses.Add(new UserCourse
                {
                    UserId = userId,
                    CourseId = schedule.CourseId,
                    PurchaseDate = DateTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Оплата прошла успешно! Вы записаны на курс: {schedule.Course?.CourseName ?? "Неизвестный курс"}.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при оплате: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            try
            {
                var query = _context.Schedules
                    .Include(s => s.User)
                    .Include(s => s.Course)
                    .AsQueryable();

                // Фильтрация SearchQuery учитывает TeacherName и CourseName
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    query = query.Where(s =>
                        s.Course.CourseName.Contains(SearchQuery) ||
                        s.User.FullName.Contains(SearchQuery));
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

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Расписание");

                    worksheet.Cells[1, 1].Value = "Дата";
                    worksheet.Cells[1, 2].Value = "Время";
                    worksheet.Cells[1, 3].Value = "Курс";
                    worksheet.Cells[1, 5].Value = "Цена";
                    worksheet.Cells[1, 6].Value = "Преподаватель";
                    worksheet.Cells[1, 8].Value = "Аудитория";
                    worksheet.Cells[1, 9].Value = "Группы";

                    using (var range = worksheet.Cells[1, 1, 1, 9])
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
                        worksheet.Cells[i + 2, 5].Value = schedule.Course?.Price ?? 0;
                        worksheet.Cells[i + 2, 6].Value = schedule.User?.FullName ?? "Не указан";
                        worksheet.Cells[i + 2, 8].Value = schedule.Classroom ?? "Не указана";

                        using (var range = worksheet.Cells[1, 1, 1, 9])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Font.Name = "Calibri";
                            range.Style.Font.Size = 12;
                        }

                        var groups = groupSchedules
                            .Where(gs => gs.ScheduleId == schedule.Id)
                            .Select(gs => gs.Group?.Name)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .ToList();
                        worksheet.Cells[i + 2, 9].Value = groups.Any() ? string.Join(", ", groups) : "Без групп";

                        using (var range = worksheet.Cells[i + 2, 1, i + 2, 9])
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

                    Response.Headers.Add("Content-Disposition", "attachment; filename=Schedule.xlsx");
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Schedule.xlsx");
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