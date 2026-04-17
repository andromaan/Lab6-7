using Lab4.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Lab4.Tests;

public class SqliteRelationalTests
{
    public readonly AppDbContext Context;
    public readonly SqliteConnection Connection;
    
    public SqliteRelationalTests()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(Connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated(); // applies the schema
    }

    [Fact]
    public async Task ForeignKey_EnrollingInNonExistingCourse_ThrowsAsync()
    {
        // Arrange
        using (Context)
        using (Connection)
        {
            var enrollment = new Enrollment
            {
                StudentId = 999, // does not exist
                CourseId = 999,  // does not exist
                Grade = 85
            };

            // Act & Assert
            Context.Enrollments.Add(enrollment);
            var exception = await Should.ThrowAsync<DbUpdateException>(
                () => Context.SaveChangesAsync());
            exception.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateEmail_ThrowsAsync()
    {
        // Arrange
        using (Connection)
        using (Context)
        {
            var student1 = new Student
            {
                FullName = "Alice", Email = "dup@test.com",
                EnrollmentDate = DateTime.UtcNow
            };
            var student2 = new Student
            {
                FullName = "Bob", Email = "dup@test.com", // same email
                EnrollmentDate = DateTime.UtcNow
            };

            Context.Students.Add(student1);
            await Context.SaveChangesAsync();
            Context.Students.Add(student2);

            // Act & Assert
            await Should.ThrowAsync<DbUpdateException>(
                () => Context.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task CascadeDelete_DeletingStudent_RemovesEnrollmentsAsync()
    {
        // Arrange
        using (Connection)
        using (Context)
        {
            var course = new Course { Title = "CS101", Credits = 3 };
            var student = new Student
            {
                FullName = "Charlie", Email = "charlie@test.com",
                EnrollmentDate = DateTime.UtcNow,
                Enrollments = new List<Enrollment>
                {
                    new Enrollment { Course = course, Grade = 88 }
                }
            };
            Context.Students.Add(student);
            await Context.SaveChangesAsync();

            // Act
            Context.Students.Remove(student);
            await Context.SaveChangesAsync();

            // Assert
            var enrollments = await Context.Enrollments.ToListAsync();
            enrollments.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task OptimisticConcurrency_HandlingConcurrentUpdates_ThrowsConcurrencyExceptionAsync()
    {
        // Arrange
        using (Connection)
        using (Context)
        {
            var student = new Student
            {
                FullName = "Original Name",
                Email = "concurrency@test.com",
                EnrollmentDate = DateTime.UtcNow
            };

            Context.Students.Add(student);
            await Context.SaveChangesAsync();

            // Create a second context using the SAME connection
            var options2 = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(Connection)
                .Options;
            using var context2 = new AppDbContext(options2);

            var studentInContext1 = await Context.Students.FindAsync(student.Id);
            var studentInContext2 = await context2.Students.FindAsync(student.Id);

            // Act
            // First user updates the name
            studentInContext1.FullName = "Updated by User 1";
            await Context.SaveChangesAsync();

            // Second user tries to update the name concurrently
            studentInContext2.FullName = "Updated by User 2";
            
            // Assert
            await Should.ThrowAsync<DbUpdateConcurrencyException>(
                () => context2.SaveChangesAsync());
        }
    }
}

