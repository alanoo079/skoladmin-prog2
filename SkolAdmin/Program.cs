using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();


app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseStaticFiles();

List<Person> people = new();
List<Course> courses = new();
List<Grade> grades = new();

app.MapGet("/", async context => await context.Response.SendFileAsync("index.html"));

app.MapPost("/addStudent", ([FromBody] Student s) =>
{
    people.Add(s);
    return Results.Ok(s);
});

app.MapPost("/addTeacher", ([FromBody] Teacher t) =>
{
    people.Add(t);
    return Results.Ok(t);
});

app.MapGet("/students", () => people.OfType<Student>());
app.MapGet("/teachers", () => people.OfType<Teacher>());

app.MapPost("/addCourse", ([FromBody] Course c) =>
{
    courses.Add(c);
    return Results.Ok(c);
});

app.MapGet("/courses", () => courses);

app.MapPost("/assignGrade", ([FromBody] Grade g) =>
{
    grades.Add(g);
    return Results.Ok(g);
});

app.MapGet("/grades", () => grades);

app.MapGet("/search", (string query) =>
{
    var person = people.FirstOrDefault(p =>
        p.Id.ToString() == query ||
        $"{p.FirstName} {p.LastName}".Trim().Equals(query, StringComparison.OrdinalIgnoreCase)
    );

    if (person == null)
        return Results.NotFound("inte hittad bruh");

    var isStudent = person is Student;
    var isTeacher = person is Teacher;

    var enrolledCourses = courses
        .Where(c => isStudent && c.StudentIds.Contains(person.Id) || isTeacher && c.TeacherId == person.Id)
        .ToList();

    var personGrades = isStudent
        ? grades.Where(g => g.StudentId == person.Id).ToList()
        : new List<Grade>();

    return Results.Ok(new
    {
        Id = person.Id,
        Name = $"{person.FirstName} {person.LastName}",
        Type = isStudent ? "Student" : "Teacher",
        Courses = enrolledCourses.Select(c => new { c.CourseCode, c.Name }),
        Grades = personGrades.Select(g => new { g.CourseCode, g.Level, g.Date })
    });
});

app.MapGet("/searchSuggestions", (string query) =>
{
    var matches = people
        .Where(p =>
            p.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
            $"{p.FirstName} {p.LastName}".Contains(query, StringComparison.OrdinalIgnoreCase))
        .Take(5)
        .Select(p => new {
            p.Id,
            Name = $"{p.FirstName} {p.LastName}",
            Type = p is Student ? "Student" : "Teacher"
        });

    return Results.Ok(matches);
});



app.Run();

abstract class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public virtual string VisaInfo()
    {
        return $"ID: {Id}, Namn: {FirstName} {LastName}";
    }
}

class Teacher : Person
{
    public string Name => $"{FirstName} {LastName}";
}

class Student : Person
{
    public string Name => $"{FirstName} {LastName}";
}


class Course
{
    public string CourseCode { get; set; } = "";
    public string Name { get; set; } = "";
    public int TeacherId { get; set; }
    public List<int> StudentIds { get; set; } = new();
}

class Grade
{
    public string CourseCode { get; set; } = "";
    public int StudentId { get; set; }
    public GradeLevel Level { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
}

enum GradeLevel
{
    A, B, C, D, E, F
}

