using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [StringLength(50, ErrorMessage = "Имя пользователя не должно превышать 50 символов")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; }

        public string Role { get; set; } = "User";

        [StringLength(100)]
        public string? FullName { get; set; }

        public int? GroupId { get; set; }
        public Group? Group { get; set; }
    }

    public class Teacher
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Полное имя обязательно")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Предмет обязателен")]
        [StringLength(100)]
        public string Subject { get; set; }
    }

    public class Course
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название курса обязательно")]
        [StringLength(100)]
        public string CourseName { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(500)]
        public string VideoUrl { get; set; }

        [Required]
        public decimal Price { get; set; }

        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
    public class UserCourse
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public DateTime PurchaseDate { get; set; }
    }
    public class Attendance
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int ScheduleId { get; set; }
        public Schedule Schedule { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public bool IsPresent { get; set; }
    }
    public class AttendanceStats
    {
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Total => Present + Absent;
    }
    public class Schedule
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Дата обязательна")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Время обязательно")]
        public TimeSpan Time { get; set; }

        [Required(ErrorMessage = "Курс обязателен")]
        public int CourseId { get; set; }
        public Course Course { get; set; }

        [Required(ErrorMessage = "Преподаватель обязателен")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required(ErrorMessage = "Аудитория обязательна")]
        [StringLength(50)]
        public string Classroom { get; set; }
    }

    public class Group
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название группы обязательно")]
        [StringLength(50)]
        public string Name { get; set; }
    }

    public class Grade
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Студент обязателен")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required(ErrorMessage = "Курс обязателен")]
        public int CourseId { get; set; }
        public Course Course { get; set; }

        [Required(ErrorMessage = "Оценка обязательна")]
        [Range(0, 100, ErrorMessage = "Оценка должна быть от 0 до 100")]
        public int Score { get; set; }

        public DateTime DateAssigned { get; set; }
    }

    public class UserSchedule
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public int ScheduleId { get; set; }
        public Schedule Schedule { get; set; }
    }

    public class GroupSchedule
    {
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public int ScheduleId { get; set; }
        public Schedule Schedule { get; set; }
    }

}