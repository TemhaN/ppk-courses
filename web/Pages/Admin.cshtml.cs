using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using web.Data;
using web.Models;

namespace web.Pages
{
    public class AdminModel : PageModel
    {
        private readonly AppDbContext _context;

        public AdminModel(AppDbContext context)
        {
            _context = context;
        }

        public List<User> Users { get; set; }
        public List<Schedule> Schedules { get; set; }
        public List<Course> Courses { get; set; }
        public List<User> Teachers { get; set; }
        public List<Group> Groups { get; set; }
        public Dictionary<int, string> ScheduleGroupNames { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? TeacherId { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? GroupId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string UserSearch { get; set; }
        [BindProperty]
        public string CourseSearch { get; set; }

        private async Task LoadModelDataAsync()
        {
            var userQuery = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(UserSearch))
            {
                UserSearch = UserSearch.Trim();
                userQuery = userQuery.Where(u => u.Username.Contains(UserSearch) || u.Email.Contains(UserSearch));
            }
            Users = await userQuery.ToListAsync();

            var courseQuery = _context.Courses.AsQueryable();
            if (!string.IsNullOrEmpty(CourseSearch))
            {
                CourseSearch = CourseSearch.Trim();
                courseQuery = courseQuery.Where(c => c.CourseName.Contains(CourseSearch));
            }
            Courses = await courseQuery.ToListAsync();

            var scheduleQuery = _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.User)
                .AsQueryable();

            if (TeacherId.HasValue)
            {
                scheduleQuery = scheduleQuery.Where(s => s.UserId == TeacherId.Value);
            }
            if (GroupId.HasValue)
            {
                scheduleQuery = scheduleQuery
                    .Where(s => _context.GroupSchedules.Any(gs => gs.GroupId == GroupId.Value && gs.ScheduleId == s.Id));
            }

            Schedules = await scheduleQuery.OrderBy(s => s.Date).ThenBy(s => s.Time).ToListAsync();

            ScheduleGroupNames = await _context.GroupSchedules
                .Where(gs => Schedules.Select(s => s.Id).Contains(gs.ScheduleId))
                .Join(_context.Groups,
                    gs => gs.GroupId,
                    g => g.Id,
                    (gs, g) => new { gs.ScheduleId, GroupName = g.Name })
                .GroupBy(x => x.ScheduleId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => string.Join(", ", g.Select(x => x.GroupName)));

            Teachers = await _context.Users.Where(u => u.Role == "Teacher").ToListAsync();
            Groups = await _context.Groups.ToListAsync();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ModelState.Clear();
            await LoadModelDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    ModelState.AddModelError("", "Пользователь не найден.");
                    await LoadModelDataAsync();
                    return Page();
                }

                var grades = await _context.Grades.Where(g => g.UserId == userId).ToListAsync();
                if (grades.Any())
                {
                    _context.Grades.RemoveRange(grades);
                }

                var userCourses = await _context.UserCourses.Where(uc => uc.UserId == userId).ToListAsync();
                if (userCourses.Any())
                {
                    _context.UserCourses.RemoveRange(userCourses);
                }

                var attendances = await _context.Attendances.Where(a => a.UserId == userId).ToListAsync();
                if (attendances.Any())
                {
                    _context.Attendances.RemoveRange(attendances);
                }

                var userSchedules = await _context.UserSchedules.Where(us => us.UserId == userId).ToListAsync();
                if (userSchedules.Any())
                {
                    _context.UserSchedules.RemoveRange(userSchedules);
                }

                if (user.Role == "Teacher")
                {
                    var schedules = await _context.Schedules.Where(s => s.UserId == userId).ToListAsync();
                    if (schedules.Any())
                    {
                        var groupSchedules = await _context.GroupSchedules
                            .Where(gs => schedules.Select(s => s.Id).Contains(gs.ScheduleId))
                            .ToListAsync();
                        if (groupSchedules.Any())
                        {
                            _context.GroupSchedules.RemoveRange(groupSchedules);
                        }
                        _context.Schedules.RemoveRange(schedules);
                    }
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Ошибка при удалении пользователя. Возможно, пользователь связан с другими данными.");
                Console.WriteLine(ex.InnerException?.Message);
            }

            await LoadModelDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostChangeRoleAsync(int userId, string newRole)
        {
            if (string.IsNullOrEmpty(newRole) || !new[] { "Student", "Teacher", "Admin" }.Contains(newRole))
            {
                ModelState.AddModelError("", "Некорректная роль");
                await LoadModelDataAsync();
                return Page();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError("", "Пользователь не найден");
                await LoadModelDataAsync();
                return Page();
            }

            user.Role = newRole;
            if (newRole != "Student")
            {
                user.GroupId = null;
            }

            await _context.SaveChangesAsync();
            await LoadModelDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteCourseAsync(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            await LoadModelDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddCourseAsync(string CourseName, string Description, decimal Price, string VideoUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(CourseName) || Price < 0)
                {
                    ModelState.AddModelError("", "Название курса обязательно, а цена не может быть отрицательной.");
                    await LoadModelDataAsync();
                    return Page();
                }

                if (await _context.Courses.AnyAsync(c => c.CourseName == CourseName))
                {
                    ModelState.AddModelError("", "Курс с таким названием уже существует.");
                    await LoadModelDataAsync();
                    return Page();
                }

                var course = new Course
                {
                    CourseName = CourseName.Trim(),
                    Description = Description?.Trim(),
                    Price = Price,
                    VideoUrl = string.IsNullOrEmpty(VideoUrl) ? null : VideoUrl.Trim()
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Ошибка при добавлении курса. Проверьте данные и попробуйте снова.");
                Console.WriteLine(ex.InnerException?.Message);
            }

            await LoadModelDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddGroupAsync(string GroupName)
        {
            try
            {
                if (string.IsNullOrEmpty(GroupName))
                {
                    ModelState.AddModelError("", "Название группы обязательно.");
                    await LoadModelDataAsync();
                    return Page();
                }

                if (await _context.Groups.AnyAsync(g => g.Name == GroupName))
                {
                    ModelState.AddModelError("", "Группа с таким названием уже существует.");
                    await LoadModelDataAsync();
                    return Page();
                }

                var group = new Group
                {
                    Name = GroupName.Trim()
                };

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Ошибка при добавлении группы. Проверьте данные и попробуйте снова.");
                Console.WriteLine(ex.InnerException?.Message);
            }

            await LoadModelDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditGroupAsync(int groupId, string GroupName)
        {
            try
            {
                if (string.IsNullOrEmpty(GroupName))
                {
                    ModelState.AddModelError("", "Название группы обязательно.");
                    await LoadModelDataAsync();
                    return Page();
                }

                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    ModelState.AddModelError("", "Группа не найдена.");
                    await LoadModelDataAsync();
                    return Page();
                }

                if (await _context.Groups.AnyAsync(g => g.Name == GroupName && g.Id != groupId))
                {
                    ModelState.AddModelError("", "Группа с таким названием уже существует.");
                    await LoadModelDataAsync();
                    return Page();
                }

                group.Name = GroupName.Trim();
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Ошибка при редактировании группы. Проверьте данные и попробуйте снова.");
                Console.WriteLine(ex.InnerException?.Message);
            }

            await LoadModelDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteGroupAsync(int groupId)
        {
            try
            {
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    ModelState.AddModelError("", "Группа не найдена.");
                    await LoadModelDataAsync();
                    return Page();
                }

                var groupSchedules = await _context.GroupSchedules.Where(gs => gs.GroupId == groupId).ToListAsync();
                if (groupSchedules.Any())
                {
                    _context.GroupSchedules.RemoveRange(groupSchedules);
                }

                var users = await _context.Users.Where(u => u.GroupId == groupId).ToListAsync();
                foreach (var user in users)
                {
                    user.GroupId = null;
                }

                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Ошибка при удалении группы. Возможно, группа связана с другими данными.");
                Console.WriteLine(ex.InnerException?.Message);
            }

            await LoadModelDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddScheduleAsync(int CourseId, int TeacherId, int[] GroupIds, DateTime Date, string Time, string Classroom)
        {
            try
            {
                if (Date < DateTime.Today)
                {
                    ModelState.AddModelError("", "Дата не может быть в прошлом.");
                    await LoadModelDataAsync();
                    return Page();
                }

                if (!TimeSpan.TryParse(Time, out var parsedTime))
                {
                    ModelState.AddModelError("", "Некорректный формат времени.");
                    await LoadModelDataAsync();
                    return Page();
                }

                var course = await _context.Courses.FindAsync(CourseId);
                if (course == null)
                {
                    ModelState.AddModelError("", "Курс не существует.");
                    await LoadModelDataAsync();
                    return Page();
                }

                var teacher = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == TeacherId && u.Role == "Teacher");
                if (teacher == null)
                {
                    ModelState.AddModelError("", "Преподаватель не существует или не имеет роль 'Teacher'.");
                    await LoadModelDataAsync();
                    return Page();
                }

                if (GroupIds == null || !GroupIds.Any())
                {
                    ModelState.AddModelError("", "Не выбрано ни одной группы.");
                    await LoadModelDataAsync();
                    return Page();
                }

                foreach (var groupId in GroupIds)
                {
                    if (!await _context.Groups.AnyAsync(g => g.Id == groupId))
                    {
                        ModelState.AddModelError("", $"Группа с ID {groupId} не существует.");
                        await LoadModelDataAsync();
                        return Page();
                    }
                }

                var schedule = new Schedule
                {
                    CourseId = CourseId,
                    UserId = TeacherId,
                    Date = Date,
                    Time = parsedTime,
                    Classroom = Classroom?.Trim()
                };

                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();

                foreach (var groupId in GroupIds)
                {
                    _context.GroupSchedules.Add(new GroupSchedule
                    {
                        GroupId = groupId,
                        ScheduleId = schedule.Id
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Ошибка при добавлении расписания. Проверьте данные и попробуйте снова.");
                Console.WriteLine($"Ошибка: {ex.InnerException?.Message}");
            }

            await LoadModelDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteScheduleAsync(int scheduleId)
        {
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }
            await LoadModelDataAsync();
            return Page();
        }
    }
}