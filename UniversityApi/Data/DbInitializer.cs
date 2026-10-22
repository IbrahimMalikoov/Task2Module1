using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniversityApi.Models;

namespace UniversityApi.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await context.Database.MigrateAsync();

        string[] roles = { "Admin", "Teacher", "Student" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (!context.Teachers.Any())
        {
            var teacher1 = new Teacher
            {
                FirstName = "Евгений",
                LastName = "Герцен",
                Email = "e.gertsen@satbayev.university",
                Department = "Программная инженерия"
            };
            var teacher2 = new Teacher
            {
                FirstName = "Фарида",
                LastName = "Абдолдина",
                Email = "f.abdoldina@satbayev.university",
                Department = "Программная инженерия"
            };
            await context.Teachers.AddRangeAsync(teacher1, teacher2);
            await context.SaveChangesAsync();

            var course1 = new Course
            {
                Name = "Разработка веб-сервисов (CSE5032)",
                Description = "Курс по ASP.NET Core Web API, REST и EF Core",
                Credits = 5,
                TeacherId = teacher1.Id
            };
            var course2 = new Course
            {
                Name = "Базы данных и SQL (CSE5021)",
                Description = "Проектирование и оптимизация реляционных баз данных",
                Credits = 4,
                TeacherId = teacher2.Id
            };
            await context.Courses.AddRangeAsync(course1, course2);
            await context.SaveChangesAsync();

            var student1 = new Student
            {
                FirstName = "Аян",
                LastName = "Сериков",
                Email = "ayan.serikov@satbayev.university",
                BirthDate = new DateTime(2003, 5, 14)
            };
            var student2 = new Student
            {
                FirstName = "Арман",
                LastName = "Данияров",
                Email = "arman.daniyarov@satbayev.university",
                BirthDate = new DateTime(2004, 2, 20)
            };
            await context.Students.AddRangeAsync(student1, student2);
            await context.SaveChangesAsync();

            var enrollment1 = new Enrollment
            {
                StudentId = student1.Id,
                CourseId = course1.Id,
                Grade = 95
            };
            var enrollment2 = new Enrollment
            {
                StudentId = student2.Id,
                CourseId = course2.Id,
                Grade = 88
            };
            await context.Enrollments.AddRangeAsync(enrollment1, enrollment2);
            await context.SaveChangesAsync();
        }

        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@satbayev.university",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var teacher1Entity = context.Teachers.FirstOrDefault(t => t.Email == "e.gertsen@satbayev.university");
        var teacherUser = await userManager.FindByNameAsync("teacher");
        if (teacherUser == null && teacher1Entity != null)
        {
            teacherUser = new ApplicationUser
            {
                UserName = "teacher",
                Email = teacher1Entity.Email,
                TeacherId = teacher1Entity.Id,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(teacherUser, "Teacher123!");
            await userManager.AddToRoleAsync(teacherUser, "Teacher");
        }

        var teacher2Entity = context.Teachers.FirstOrDefault(t => t.Email == "f.abdoldina@satbayev.university");
        var teacher2User = await userManager.FindByNameAsync("teacher2");
        if (teacher2User == null && teacher2Entity != null)
        {
            teacher2User = new ApplicationUser
            {
                UserName = "teacher2",
                Email = teacher2Entity.Email,
                TeacherId = teacher2Entity.Id,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(teacher2User, "Teacher123!");
            await userManager.AddToRoleAsync(teacher2User, "Teacher");
        }

        var student1Entity = context.Students.FirstOrDefault(s => s.Email == "ayan.serikov@satbayev.university");
        var studentUser = await userManager.FindByNameAsync("student");
        if (studentUser == null && student1Entity != null)
        {
            studentUser = new ApplicationUser
            {
                UserName = "student",
                Email = student1Entity.Email,
                StudentId = student1Entity.Id,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(studentUser, "Student123!");
            await userManager.AddToRoleAsync(studentUser, "Student");
        }

        var student2Entity = context.Students.FirstOrDefault(s => s.Email == "arman.daniyarov@satbayev.university");
        var student2User = await userManager.FindByNameAsync("student2");
        if (student2User == null && student2Entity != null)
        {
            student2User = new ApplicationUser
            {
                UserName = "student2",
                Email = student2Entity.Email,
                StudentId = student2Entity.Id,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(student2User, "Student123!");
            await userManager.AddToRoleAsync(student2User, "Student");
        }
    }
}
