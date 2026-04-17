using Lab4.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Lab4.Tests;

public class StudentRepositoryTests
{
    public readonly AppDbContext Context;

    public StudentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ValidStudent_SavesSuccessfullyAsync()
    {
        // Arrange
        var repo = new StudentRepository(Context);
        var student = new Student
        {
            FullName = "John Doe",
            Email = "john@example.com",
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        await repo.AddAsync(student);

        // Assert
        var saved = await Context.Students.FirstOrDefaultAsync(s => s.Email == "john@example.com");
        saved.ShouldNotBeNull();
        saved.FullName.ShouldBe("John Doe");
        saved.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesEnrollmentsAsync()
    {
        // Arrange
        var course = new Course { Title = "Testing 101", Credits = 3 };
        var student = new Student
        {
            FullName = "Jane Smith",
            Email = "jane@example.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course, Grade = 95 }
            }
        };
        Context.Students.Add(student);
        await Context.SaveChangesAsync();

        var repo = new StudentRepository(Context);

        // Act
        var result = await repo.GetByIdAsync(student.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Enrollments.ShouldNotBeNull();
        result.Enrollments.Count.ShouldBe(1);
        result.Enrollments.First().Grade.ShouldBe(95m);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllStudentsAsync()
    {
        // Arrange
        Context.Students.Add(new Student { FullName = "S1", Email = "s1@test.com" });
        Context.Students.Add(new Student { FullName = "S2", Email = "s2@test.com" });
        await Context.SaveChangesAsync();

        var repo = new StudentRepository(Context);

        // Act
        var students = await repo.GetAllAsync();

        // Assert
        students.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesStudentAsync()
    {
        // Arrange
        var student = new Student { FullName = "Old Name", Email = "test@test.com" };
        Context.Students.Add(student);
        await Context.SaveChangesAsync();

        var repo = new StudentRepository(Context);

        // Act
        student.FullName = "New Name";
        await repo.UpdateAsync(student);

        // Assert
        var updated = await Context.Students.FindAsync(student.Id);
        updated.FullName.ShouldBe("New Name");
    }

    [Fact]
    public async Task DeleteAsync_RemovesStudentAsync()
    {
        // Arrange
        var student = new Student { FullName = "To Delete", Email = "delete@test.com" };
        Context.Students.Add(student);
        await Context.SaveChangesAsync();

        var repo = new StudentRepository(Context);

        // Act
        await repo.DeleteAsync(student.Id);

        // Assert
        var deleted = await Context.Students.FindAsync(student.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task GetTopStudentsAsync_ReturnsOrderedByAverageGradeAsync()
    {
        // Arrange
        var course1 = new Course { Title = "Math", Credits = 4 };
        var course2 = new Course { Title = "Science", Credits = 3 };

        var studentA = new Student
        {
            FullName = "Alice", Email = "alice@test.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course1, Grade = 70 },
                new Enrollment { Course = course2, Grade = 80 }
            }
        };
        var studentB = new Student
        {
            FullName = "Bob", Email = "bob@test.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course1, Grade = 90 },
                new Enrollment { Course = course2, Grade = 95 }
            }
        };

        Context.Students.AddRange(studentA, studentB);
        await Context.SaveChangesAsync();

        var repo = new StudentRepository(Context);

        // Act
        var top = await repo.GetTopStudentsAsync(1);

        // Assert
        top.Count.ShouldBe(1);
        top.First().FullName.ShouldBe("Bob");
    }
}
