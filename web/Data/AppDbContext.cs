using Microsoft.EntityFrameworkCore;
using web.Models;
using static System.Net.WebRequestMethods;

namespace web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<UserSchedule> UserSchedules { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<GroupSchedule> GroupSchedules { get; set; }
        public DbSet<UserCourse> UserCourses { get; set; }
        public DbSet<Attendance> Attendances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserSchedule>()
                .HasKey(us => new { us.UserId, us.ScheduleId });

            modelBuilder.Entity<UserSchedule>()
                .HasOne(us => us.User)
                .WithMany()
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserSchedule>()
                .HasOne(us => us.Schedule)
                .WithMany()
                .HasForeignKey(us => us.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupSchedule>()
                .HasKey(gs => new { gs.GroupId, gs.ScheduleId });

            modelBuilder.Entity<GroupSchedule>()
                .HasOne(gs => gs.Group)
                .WithMany()
                .HasForeignKey(gs => gs.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupSchedule>()
                .HasOne(gs => gs.Schedule)
                .WithMany()
                .HasForeignKey(gs => gs.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Grade>()
                .HasKey(g => g.Id);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Course)
                .WithMany()
                .HasForeignKey(g => g.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Schedule>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Course)
                .WithMany(c => c.Schedules)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCourse>()
                .HasKey(uc => new { uc.UserId, uc.CourseId });

            modelBuilder.Entity<UserCourse>()
                .HasOne(uc => uc.User)
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCourse>()
                .HasOne(uc => uc.Course)
                .WithMany()
                .HasForeignKey(uc => uc.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attendance>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Schedule)
                .WithMany()
                .HasForeignKey(a => a.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Teacher>().ToTable("Teachers");
            modelBuilder.Entity<Course>().ToTable("Courses");
            modelBuilder.Entity<Schedule>().ToTable("Schedules");
            modelBuilder.Entity<UserSchedule>().ToTable("UserSchedules");
            modelBuilder.Entity<Group>().ToTable("Groups");
            modelBuilder.Entity<Grade>().ToTable("Grades");
            modelBuilder.Entity<GroupSchedule>().ToTable("GroupSchedules");
            modelBuilder.Entity<UserCourse>().ToTable("UserCourses");
            modelBuilder.Entity<Attendance>().ToTable("Attendances");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Course>()
                .Property(c => c.Price)
                .HasPrecision(18, 2);

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "student1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("student1"), Email = "student1@example.com", Role = "Student", FullName = "Иван Иванов", GroupId = 1 },
                new User { Id = 2, Username = "student2", PasswordHash = passwordHash, Email = "student2@example.com", Role = "Student", FullName = "Пётр Петров", GroupId = 1 },
                new User { Id = 3, Username = "student3", PasswordHash = passwordHash, Email = "student3@example.com", Role = "Student", FullName = "Сергей Сергеев", GroupId = 2 },
                new User { Id = 4, Username = "teacher1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher1"), Email = "teacher1@example.com", Role = "Teacher", FullName = "Мария Смирнова" },
                new User { Id = 5, Username = "teacher2", PasswordHash = passwordHash, Email = "teacher2@example.com", Role = "Teacher", FullName = "Елена Кузнецова" },
                new User { Id = 6, Username = "teacher3", PasswordHash = passwordHash, Email = "teacher3@example.com", Role = "Teacher", FullName = "Алексей Соколов" },
                new User { Id = 7, Username = "student4", PasswordHash = passwordHash, Email = "student4@example.com", Role = "Student", FullName = "Анна Сидорова", GroupId = 3 },
                new User { Id = 8, Username = "student5", PasswordHash = passwordHash, Email = "student5@example.com", Role = "Student", FullName = "Дмитрий Козлов", GroupId = 3 },
                new User { Id = 9, Username = "student6", PasswordHash = passwordHash, Email = "student6@example.com", Role = "Student", FullName = "Ольга Морозова", GroupId = 4 },
                new User { Id = 10, Username = "student7", PasswordHash = passwordHash, Email = "student7@example.com", Role = "Student", FullName = "Михаил Попов", GroupId = 4 },
                new User { Id = 11, Username = "teacher4", PasswordHash = passwordHash, Email = "teacher4@example.com", Role = "Teacher", FullName = "Наталья Павлова" },
                new User { Id = 12, Username = "teacher5", PasswordHash = passwordHash, Email = "teacher5@example.com", Role = "Teacher", FullName = "Игорь Васильев" },
                new User { Id = 13, Username = "student8", PasswordHash = passwordHash, Email = "student8@example.com", Role = "Student", FullName = "Екатерина Лебедева", GroupId = 5 },
                new User { Id = 14, Username = "student9", PasswordHash = passwordHash, Email = "student9@example.com", Role = "Student", FullName = "Владимир Орлов", GroupId = 5 },
                new User { Id = 15, Username = "student10", PasswordHash = passwordHash, Email = "student10@example.com", Role = "Student", FullName = "Алина Романова", GroupId = 6 },
                new User { Id = 16, Username = "admin2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin2"), Email = "admin2@example.com", Role = "Admin", FullName = "Администратор Второй" },
                new User { Id = 99, Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"), Email = "admin@example.com", Role = "Admin", FullName = "Администратор" }
            );

            modelBuilder.Entity<Teacher>().HasData(
                new Teacher { Id = 1, FullName = "Мария Смирнова", Subject = "Программирование" },
                new Teacher { Id = 2, FullName = "Елена Кузнецова", Subject = "Веб-разработка" },
                new Teacher { Id = 3, FullName = "Алексей Соколов", Subject = "Базы данных" },
                new Teacher { Id = 4, FullName = "Наталья Павлова", Subject = "Мобильная разработка" },
                new Teacher { Id = 5, FullName = "Игорь Васильев", Subject = "Data Science" }
            );

            modelBuilder.Entity<Course>().HasData(
                new Course { Id = 1, CourseName = "C++ с нуля", Description = "Изучение C++ для начинающих.", VideoUrl = "https://rutube.ru/video/b1be12d48fc7c6fcc6ed7580a87d1d11/", Price = 5000 },
                new Course { Id = 2, CourseName = "Веб-разработка", Description = "HTML, CSS и JavaScript с нуля.", VideoUrl = "https://rutube.ru/video/9ed8e64079ee7a4e99f3ee1c203012e7/", Price = 6000 },
                new Course { Id = 3, CourseName = "SQL и NoSQL", Description = "Основы реляционных и нереляционных баз.", VideoUrl = "https://rutube.ru/video/504bf16fd6d47cc657d55153e8540444/", Price = 4500 },
                new Course { Id = 4, CourseName = "Мобильная разработка", Description = "Android и iOS: основы.", VideoUrl = "https://rutube.ru/video/d56d5657b06bcd46e7c0e3a661c93d1d/", Price = 7000 },
                new Course { Id = 5, CourseName = "UI/UX дизайн", Description = "Photoshop, Figma и основы UX.", VideoUrl = "https://rutube.ru/video/4e4bfb25bfbdc34558040b6ef41f5d21/", Price = 8000 },
                new Course { Id = 6, CourseName = "Python для анализа", Description = "Python, Pandas и визуализация.", VideoUrl = "https://rutube.ru/video/84f2b1258bc9176648c1c6cfed7e4836/", Price = 5500 },
                new Course { Id = 7, CourseName = "3D-моделирование", Description = "Blender: моделирование и анимация.", VideoUrl = "https://rutube.ru/video/498d397e7531494a6c14097fb6bc04fc/", Price = 5200 },
                new Course { Id = 8, CourseName = "DevOps", Description = "CI/CD, Docker и Kubernetes.", VideoUrl = "https://rutube.ru/video/0fcd12bdb7e4e175f73d336e910a0e81/", Price = 7500 },
                new Course { Id = 9, CourseName = "Data Science", Description = "Машинное обучение и анализ данных.", VideoUrl = "https://rutube.ru/video/e648f9c65ad4d856cfbd5cfee155b39c/", Price = 6800 },
                new Course { Id = 10, CourseName = "Геймдев", Description = "Unity и Unreal Engine для начинающих.", VideoUrl = "https://rutube.ru/video/da880629ac5ae50ff3c01424b899986c/", Price = 8200 },
                new Course { Id = 12, CourseName = "Тестирование ПО", Description = "Автоматизация тестов и QA-процессы.", VideoUrl = "https://rutube.ru/video/b8a18b0cd43f04d58e54756a3d1d35b2/", Price = 7200 },
                new Course { Id = 13, CourseName = "Интернет вещей (IoT)", Description = "Встраиваемые системы и Arduino.", VideoUrl = "https://rutube.ru/video/7427d555480f6166d3b0021f6d43cae5/", Price = 6900 },
                new Course { Id = 14, CourseName = "3D-анимация", Description = "Кинематографические эффекты в Blender.", VideoUrl = "https://rutube.ru/video/498d397e7531494a6c14097fb6bc04fc/", Price = 5100 }
            );

            modelBuilder.Entity<Group>().HasData(
                new Group { Id = 1, Name = "КИ-101" },
                new Group { Id = 2, Name = "КИ-102" },
                new Group { Id = 3, Name = "КИ-103" },
                new Group { Id = 4, Name = "КИ-104" },
                new Group { Id = 5, Name = "КИ-201" },
                new Group { Id = 6, Name = "КИ-202" }
            );

            modelBuilder.Entity<Schedule>().HasData(
                new Schedule { Id = 1, Date = new DateTime(2025, 5, 20), Time = new TimeSpan(10, 0, 0), CourseId = 1, UserId = 4, Classroom = "A-101" },
                new Schedule { Id = 2, Date = new DateTime(2025, 5, 20), Time = new TimeSpan(12, 0, 0), CourseId = 2, UserId = 5, Classroom = "B-202" },
                new Schedule { Id = 3, Date = new DateTime(2025, 5, 21), Time = new TimeSpan(9, 0, 0), CourseId = 3, UserId = 6, Classroom = "C-303" },
                new Schedule { Id = 4, Date = new DateTime(2025, 5, 21), Time = new TimeSpan(11, 0, 0), CourseId = 1, UserId = 4, Classroom = "A-101" },
                new Schedule { Id = 5, Date = new DateTime(2025, 5, 22), Time = new TimeSpan(14, 0, 0), CourseId = 2, UserId = 5, Classroom = "B-202" },
                new Schedule { Id = 6, Date = new DateTime(2025, 5, 22), Time = new TimeSpan(16, 0, 0), CourseId = 3, UserId = 6, Classroom = "C-303" },
                new Schedule { Id = 7, Date = new DateTime(2025, 5, 23), Time = new TimeSpan(10, 0, 0), CourseId = 4, UserId = 4, Classroom = "D-404" },
                new Schedule { Id = 8, Date = new DateTime(2025, 5, 23), Time = new TimeSpan(12, 0, 0), CourseId = 5, UserId = 5, Classroom = "E-505" },
                new Schedule { Id = 9, Date = new DateTime(2025, 5, 24), Time = new TimeSpan(9, 0, 0), CourseId = 6, UserId = 6, Classroom = "F-606" },
                new Schedule { Id = 10, Date = new DateTime(2025, 5, 24), Time = new TimeSpan(11, 0, 0), CourseId = 4, UserId = 4, Classroom = "D-404" },
                new Schedule { Id = 11, Date = new DateTime(2025, 5, 25), Time = new TimeSpan(14, 0, 0), CourseId = 5, UserId = 5, Classroom = "E-505" },
                new Schedule { Id = 12, Date = new DateTime(2025, 5, 25), Time = new TimeSpan(16, 0, 0), CourseId = 6, UserId = 6, Classroom = "F-606" },
                new Schedule { Id = 13, Date = new DateTime(2025, 5, 26), Time = new TimeSpan(10, 0, 0), CourseId = 1, UserId = 4, Classroom = "A-101" },
                new Schedule { Id = 14, Date = new DateTime(2025, 5, 26), Time = new TimeSpan(12, 0, 0), CourseId = 2, UserId = 5, Classroom = "B-202" },
                new Schedule { Id = 15, Date = new DateTime(2025, 5, 27), Time = new TimeSpan(9, 0, 0), CourseId = 3, UserId = 6, Classroom = "C-303" },
                new Schedule { Id = 16, Date = new DateTime(2025, 5, 27), Time = new TimeSpan(11, 0, 0), CourseId = 7, UserId = 4, Classroom = "G-707" },
                new Schedule { Id = 17, Date = new DateTime(2025, 5, 28), Time = new TimeSpan(10, 0, 0), CourseId = 8, UserId = 5, Classroom = "H-808" },
                new Schedule { Id = 18, Date = new DateTime(2025, 5, 28), Time = new TimeSpan(12, 0, 0), CourseId = 9, UserId = 6, Classroom = "I-909" },
                new Schedule { Id = 19, Date = new DateTime(2025, 5, 29), Time = new TimeSpan(9, 0, 0), CourseId = 10, UserId = 4, Classroom = "J-1010" },
                new Schedule { Id = 20, Date = new DateTime(2025, 5, 29), Time = new TimeSpan(11, 0, 0), CourseId = 12, UserId = 5, Classroom = "K-1111" },
                new Schedule { Id = 21, Date = new DateTime(2025, 5, 30), Time = new TimeSpan(10, 0, 0), CourseId = 12, UserId = 6, Classroom = "L-1212" },
                new Schedule { Id = 22, Date = new DateTime(2025, 5, 30), Time = new TimeSpan(12, 0, 0), CourseId = 13, UserId = 4, Classroom = "M-1313" },
                new Schedule { Id = 23, Date = new DateTime(2025, 5, 31), Time = new TimeSpan(9, 0, 0), CourseId = 14, UserId = 5, Classroom = "N-1414" },
                new Schedule { Id = 24, Date = new DateTime(2025, 6, 1), Time = new TimeSpan(10, 0, 0), CourseId = 9, UserId = 12, Classroom = "A-101" },
                new Schedule { Id = 25, Date = new DateTime(2025, 6, 1), Time = new TimeSpan(12, 0, 0), CourseId = 10, UserId = 11, Classroom = "B-202" },
                new Schedule { Id = 26, Date = new DateTime(2025, 6, 2), Time = new TimeSpan(9, 0, 0), CourseId = 12, UserId = 6, Classroom = "C-303" },
                new Schedule { Id = 27, Date = new DateTime(2025, 6, 2), Time = new TimeSpan(11, 0, 0), CourseId = 13, UserId = 4, Classroom = "D-404" },
                new Schedule { Id = 28, Date = new DateTime(2025, 6, 3), Time = new TimeSpan(14, 0, 0), CourseId = 14, UserId = 5, Classroom = "E-505" },
                new Schedule { Id = 29, Date = new DateTime(2025, 6, 3), Time = new TimeSpan(16, 0, 0), CourseId = 1, UserId = 4, Classroom = "F-606" },
                new Schedule { Id = 30, Date = new DateTime(2025, 6, 4), Time = new TimeSpan(10, 0, 0), CourseId = 2, UserId = 5, Classroom = "G-707" },
                new Schedule { Id = 31, Date = new DateTime(2025, 6, 4), Time = new TimeSpan(12, 0, 0), CourseId = 3, UserId = 6, Classroom = "H-808" },
                new Schedule { Id = 32, Date = new DateTime(2025, 6, 5), Time = new TimeSpan(9, 0, 0), CourseId = 4, UserId = 11, Classroom = "I-909" },
                new Schedule { Id = 33, Date = new DateTime(2025, 6, 5), Time = new TimeSpan(11, 0, 0), CourseId = 5, UserId = 12, Classroom = "J-1010" },
                new Schedule { Id = 34, Date = new DateTime(2025, 6, 6), Time = new TimeSpan(10, 0, 0), CourseId = 6, UserId = 6, Classroom = "K-1111" },
                new Schedule { Id = 35, Date = new DateTime(2025, 6, 6), Time = new TimeSpan(12, 0, 0), CourseId = 7, UserId = 4, Classroom = "L-1212" },
                new Schedule { Id = 36, Date = new DateTime(2025, 6, 7), Time = new TimeSpan(9, 0, 0), CourseId = 8, UserId = 5, Classroom = "M-1313" }
            );

            modelBuilder.Entity<UserSchedule>().HasData(
                new UserSchedule { UserId = 1, ScheduleId = 1 },
                new UserSchedule { UserId = 1, ScheduleId = 2 },
                new UserSchedule { UserId = 1, ScheduleId = 3 },
                new UserSchedule { UserId = 2, ScheduleId = 4 },
                new UserSchedule { UserId = 2, ScheduleId = 5 },
                new UserSchedule { UserId = 3, ScheduleId = 6 },
                new UserSchedule { UserId = 3, ScheduleId = 7 },
                new UserSchedule { UserId = 1, ScheduleId = 8 },
                new UserSchedule { UserId = 1, ScheduleId = 9 },
                new UserSchedule { UserId = 2, ScheduleId = 10 },
                new UserSchedule { UserId = 2, ScheduleId = 11 },
                new UserSchedule { UserId = 3, ScheduleId = 12 },
                new UserSchedule { UserId = 3, ScheduleId = 13 },
                new UserSchedule { UserId = 1, ScheduleId = 14 },
                new UserSchedule { UserId = 2, ScheduleId = 15 },
                new UserSchedule { UserId = 1, ScheduleId = 16 },
                new UserSchedule { UserId = 2, ScheduleId = 17 },
                new UserSchedule { UserId = 3, ScheduleId = 18 },
                new UserSchedule { UserId = 1, ScheduleId = 19 },
                new UserSchedule { UserId = 2, ScheduleId = 20 },
                new UserSchedule { UserId = 3, ScheduleId = 21 },
                new UserSchedule { UserId = 1, ScheduleId = 22 },
                new UserSchedule { UserId = 2, ScheduleId = 23 },
                new UserSchedule { UserId = 7, ScheduleId = 24 },
                new UserSchedule { UserId = 8, ScheduleId = 25 },
                new UserSchedule { UserId = 9, ScheduleId = 26 },
                new UserSchedule { UserId = 10, ScheduleId = 27 },
                new UserSchedule { UserId = 13, ScheduleId = 28 },
                new UserSchedule { UserId = 14, ScheduleId = 29 },
                new UserSchedule { UserId = 15, ScheduleId = 30 },
                new UserSchedule { UserId = 7, ScheduleId = 31 },
                new UserSchedule { UserId = 8, ScheduleId = 32 },
                new UserSchedule { UserId = 9, ScheduleId = 33 },
                new UserSchedule { UserId = 10, ScheduleId = 34 },
                new UserSchedule { UserId = 13, ScheduleId = 35 },
                new UserSchedule { UserId = 14, ScheduleId = 36 },
                new UserSchedule { UserId = 15, ScheduleId = 1 },
                new UserSchedule { UserId = 7, ScheduleId = 2 },
                new UserSchedule { UserId = 8, ScheduleId = 3 },
                new UserSchedule { UserId = 9, ScheduleId = 4 },
                new UserSchedule { UserId = 10, ScheduleId = 5 },
                new UserSchedule { UserId = 13, ScheduleId = 6 },
                new UserSchedule { UserId = 14, ScheduleId = 7 },
                new UserSchedule { UserId = 15, ScheduleId = 8 }
            );

            modelBuilder.Entity<GroupSchedule>().HasData(
                new GroupSchedule { GroupId = 1, ScheduleId = 1 },
                new GroupSchedule { GroupId = 1, ScheduleId = 2 },
                new GroupSchedule { GroupId = 1, ScheduleId = 3 },
                new GroupSchedule { GroupId = 2, ScheduleId = 4 },
                new GroupSchedule { GroupId = 2, ScheduleId = 5 },
                new GroupSchedule { GroupId = 1, ScheduleId = 16 },
                new GroupSchedule { GroupId = 2, ScheduleId = 17 },
                new GroupSchedule { GroupId = 1, ScheduleId = 18 },
                new GroupSchedule { GroupId = 2, ScheduleId = 19 },
                new GroupSchedule { GroupId = 3, ScheduleId = 24 },
                new GroupSchedule { GroupId = 4, ScheduleId = 25 },
                new GroupSchedule { GroupId = 5, ScheduleId = 26 },
                new GroupSchedule { GroupId = 6, ScheduleId = 27 },
                new GroupSchedule { GroupId = 3, ScheduleId = 28 },
                new GroupSchedule { GroupId = 4, ScheduleId = 29 },
                new GroupSchedule { GroupId = 5, ScheduleId = 30 },
                new GroupSchedule { GroupId = 6, ScheduleId = 31 },
                new GroupSchedule { GroupId = 3, ScheduleId = 32 },
                new GroupSchedule { GroupId = 4, ScheduleId = 33 },
                new GroupSchedule { GroupId = 5, ScheduleId = 34 },
                new GroupSchedule { GroupId = 6, ScheduleId = 35 }
            );

            modelBuilder.Entity<Grade>().HasData(
                new Grade { Id = 1, UserId = 1, CourseId = 1, Score = 85, DateAssigned = DateTime.Now },
                new Grade { Id = 2, UserId = 2, CourseId = 2, Score = 90, DateAssigned = DateTime.Now },
                new Grade { Id = 3, UserId = 1, CourseId = 1, Score = 85, DateAssigned = DateTime.Now },
                new Grade { Id = 4, UserId = 1, CourseId = 2, Score = 90, DateAssigned = DateTime.Now },
                new Grade { Id = 5, UserId = 3, CourseId = 7, Score = 88, DateAssigned = DateTime.Now },
                new Grade { Id = 6, UserId = 2, CourseId = 8, Score = 92, DateAssigned = DateTime.Now },
                new Grade { Id = 7, UserId = 7, CourseId = 9, Score = 87, DateAssigned = DateTime.Now },
                new Grade { Id = 8, UserId = 8, CourseId = 10, Score = 91, DateAssigned = DateTime.Now },
                new Grade { Id = 9, UserId = 9, CourseId = 12, Score = 84, DateAssigned = DateTime.Now },
                new Grade { Id = 10, UserId = 10, CourseId = 13, Score = 89, DateAssigned = DateTime.Now },
                new Grade { Id = 11, UserId = 13, CourseId = 14, Score = 86, DateAssigned = DateTime.Now },
                new Grade { Id = 12, UserId = 14, CourseId = 1, Score = 93, DateAssigned = DateTime.Now },
                new Grade { Id = 13, UserId = 15, CourseId = 2, Score = 88, DateAssigned = DateTime.Now },
                new Grade { Id = 14, UserId = 7, CourseId = 3, Score = 85, DateAssigned = DateTime.Now },
                new Grade { Id = 15, UserId = 8, CourseId = 4, Score = 90, DateAssigned = DateTime.Now },
                new Grade { Id = 16, UserId = 9, CourseId = 5, Score = 87, DateAssigned = DateTime.Now },
                new Grade { Id = 17, UserId = 10, CourseId = 6, Score = 92, DateAssigned = DateTime.Now },
                new Grade { Id = 18, UserId = 13, CourseId = 7, Score = 89, DateAssigned = DateTime.Now }
            );

            modelBuilder.Entity<UserCourse>().HasData(
                new UserCourse { UserId = 1, CourseId = 1 },
                new UserCourse { UserId = 1, CourseId = 2 },
                new UserCourse { UserId = 2, CourseId = 2 },
                new UserCourse { UserId = 2, CourseId = 8 },
                new UserCourse { UserId = 3, CourseId = 3 },
                new UserCourse { UserId = 3, CourseId = 7 },
                new UserCourse { UserId = 7, CourseId = 9 },
                new UserCourse { UserId = 7, CourseId = 3 },
                new UserCourse { UserId = 8, CourseId = 10 },
                new UserCourse { UserId = 8, CourseId = 4 },
                new UserCourse { UserId = 9, CourseId = 12 },
                new UserCourse { UserId = 9, CourseId = 5 },
                new UserCourse { UserId = 10, CourseId = 13 },
                new UserCourse { UserId = 10, CourseId = 6 },
                new UserCourse { UserId = 13, CourseId = 14 },
                new UserCourse { UserId = 13, CourseId = 7 },
                new UserCourse { UserId = 14, CourseId = 1 },
                new UserCourse { UserId = 14, CourseId = 8 },
                new UserCourse { UserId = 15, CourseId = 2 },
                new UserCourse { UserId = 15, CourseId = 9 }
            );

            modelBuilder.Entity<Attendance>().HasData(
                new Attendance { Id = 1, UserId = 1, ScheduleId = 1, IsPresent = true, Date = new DateTime(2025, 5, 20) },
                new Attendance { Id = 2, UserId = 1, ScheduleId = 2, IsPresent = false, Date = new DateTime(2025, 5, 20) },
                new Attendance { Id = 3, UserId = 2, ScheduleId = 4, IsPresent = true, Date = new DateTime(2025, 5, 21) },
                new Attendance { Id = 4, UserId = 2, ScheduleId = 5, IsPresent = true, Date = new DateTime(2025, 5, 22) },
                new Attendance { Id = 5, UserId = 3, ScheduleId = 6, IsPresent = false, Date = new DateTime(2025, 5, 22) },
                new Attendance { Id = 6, UserId = 3, ScheduleId = 7, IsPresent = true, Date = new DateTime(2025, 5, 23) },
                new Attendance { Id = 7, UserId = 1, ScheduleId = 8, IsPresent = true, Date = new DateTime(2025, 5, 23) },
                new Attendance { Id = 8, UserId = 1, ScheduleId = 9, IsPresent = false, Date = new DateTime(2025, 5, 24) },
                new Attendance { Id = 9, UserId = 2, ScheduleId = 10, IsPresent = true, Date = new DateTime(2025, 5, 24) },
                new Attendance { Id = 10, UserId = 2, ScheduleId = 11, IsPresent = true, Date = new DateTime(2025, 5, 25) },
                new Attendance { Id = 11, UserId = 3, ScheduleId = 12, IsPresent = false, Date = new DateTime(2025, 5, 25) },
                new Attendance { Id = 12, UserId = 3, ScheduleId = 13, IsPresent = true, Date = new DateTime(2025, 5, 26) },
                new Attendance { Id = 13, UserId = 1, ScheduleId = 14, IsPresent = true, Date = new DateTime(2025, 5, 26) },
                new Attendance { Id = 14, UserId = 2, ScheduleId = 15, IsPresent = false, Date = new DateTime(2025, 5, 27) },
                new Attendance { Id = 15, UserId = 1, ScheduleId = 16, IsPresent = true, Date = new DateTime(2025, 5, 27) },
                new Attendance { Id = 16, UserId = 2, ScheduleId = 17, IsPresent = true, Date = new DateTime(2025, 5, 28) },
                new Attendance { Id = 17, UserId = 3, ScheduleId = 18, IsPresent = false, Date = new DateTime(2025, 5, 28) },
                new Attendance { Id = 18, UserId = 1, ScheduleId = 19, IsPresent = true, Date = new DateTime(2025, 5, 29) },
                new Attendance { Id = 19, UserId = 2, ScheduleId = 20, IsPresent = true, Date = new DateTime(2025, 5, 29) },
                new Attendance { Id = 20, UserId = 3, ScheduleId = 21, IsPresent = false, Date = new DateTime(2025, 5, 30) },
                new Attendance { Id = 21, UserId = 1, ScheduleId = 22, IsPresent = true, Date = new DateTime(2025, 5, 30) },
                new Attendance { Id = 22, UserId = 2, ScheduleId = 23, IsPresent = true, Date = new DateTime(2025, 5, 31) },
                new Attendance { Id = 23, UserId = 7, ScheduleId = 24, IsPresent = true, Date = new DateTime(2025, 6, 1) },
                new Attendance { Id = 24, UserId = 8, ScheduleId = 25, IsPresent = false, Date = new DateTime(2025, 6, 1) },
                new Attendance { Id = 25, UserId = 9, ScheduleId = 26, IsPresent = true, Date = new DateTime(2025, 6, 2) },
                new Attendance { Id = 26, UserId = 10, ScheduleId = 27, IsPresent = true, Date = new DateTime(2025, 6, 2) },
                new Attendance { Id = 27, UserId = 13, ScheduleId = 28, IsPresent = false, Date = new DateTime(2025, 6, 3) },
                new Attendance { Id = 28, UserId = 14, ScheduleId = 29, IsPresent = true, Date = new DateTime(2025, 6, 3) },
                new Attendance { Id = 29, UserId = 15, ScheduleId = 30, IsPresent = true, Date = new DateTime(2025, 6, 4) },
                new Attendance { Id = 30, UserId = 7, ScheduleId = 31, IsPresent = false, Date = new DateTime(2025, 6, 4) },
                new Attendance { Id = 31, UserId = 8, ScheduleId = 32, IsPresent = true, Date = new DateTime(2025, 6, 5) },
                new Attendance { Id = 32, UserId = 9, ScheduleId = 33, IsPresent = true, Date = new DateTime(2025, 6, 5) },
                new Attendance { Id = 33, UserId = 10, ScheduleId = 34, IsPresent = false, Date = new DateTime(2025, 6, 6) },
                new Attendance { Id = 34, UserId = 13, ScheduleId = 35, IsPresent = true, Date = new DateTime(2025, 6, 6) },
                new Attendance { Id = 35, UserId = 14, ScheduleId = 36, IsPresent = true, Date = new DateTime(2025, 6, 7) }
            );
        }
    }
}