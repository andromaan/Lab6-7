using FluentValidation;
using Lab4.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
        
var connectionString = builder.Configuration["DB_CONNECTION_STRING"] ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Setup EF Core InMemory for API Testing
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("RestTestDb"));
}
            
builder.Services.AddScoped<StudentRepository>();
builder.Services.AddScoped<IValidator<CreateStudentRequest>, CreateStudentRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateStudentRequest>, UpdateStudentRequestValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
try
{
    var retries = 5;
    while (retries > 0)
    {
        try
        {
            var created = db.Database.EnsureCreated();
            Console.WriteLine($"Database created: {created}");

            // Seed data ONLY for real database to avoid concurrent issues with in-memory testing
            if (!string.IsNullOrEmpty(connectionString) && !db.Students.Any())
            {
                db.Students.AddRange(Enumerable.Range(1, 100).Select(i => new Student
                {
                    FullName = $"Student {i}",
                    Email = $"student{i}@example.com",
                    EnrollmentDate = DateTime.UtcNow.AddDays(-i)
                }));
                db.SaveChanges();
                Console.WriteLine("Seeded 100 students.");
            }
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during EnsureCreated: {ex.Message}");
            retries--;
            if (retries == 0) throw;
            Thread.Sleep(1000);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error in database initialization: {ex}");     
}

app.MapControllers();
app.MapHealthChecks("/health/ready");

app.Run();
