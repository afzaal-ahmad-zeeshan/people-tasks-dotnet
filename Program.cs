using Microsoft.EntityFrameworkCore;
using hello_dotnet.Data;
using hello_dotnet.Models;
using TaskStatus = hello_dotnet.Models.TaskStatus;

var builder = WebApplication.CreateBuilder(args);

var usePg = string.Equals(Environment.GetEnvironmentVariable("USE_PG_EFCORE"), "true", StringComparison.OrdinalIgnoreCase);
var useMySql = string.Equals(Environment.GetEnvironmentVariable("USE_MYSQL_EFCORE"), "true", StringComparison.OrdinalIgnoreCase);
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (usePg && !string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else if (useMySql && !string.IsNullOrEmpty(connectionString))
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
    else
    {
        options.UseInMemoryDatabase("PeopleDb");
    }
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

// --- Landing pages ---

string Layout(string title, string body) => $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{title}} - Hello .NET</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: system-ui, -apple-system, sans-serif; color: #1a1a2e; background: #f8f9fa; }
        nav { background: #1a1a2e; padding: 1rem 2rem; display: flex; gap: 2rem; align-items: center; }
        nav a { color: #e0e0e0; text-decoration: none; font-weight: 500; }
        nav a:hover { color: #fff; }
        nav .brand { color: #fff; font-size: 1.25rem; font-weight: 700; margin-right: auto; }
        .container { max-width: 800px; margin: 3rem auto; padding: 0 2rem; }
        h1 { font-size: 2.5rem; margin-bottom: 1rem; }
        p { font-size: 1.1rem; line-height: 1.7; color: #444; margin-bottom: 1rem; }
        .hero { background: linear-gradient(135deg, #667eea, #764ba2); color: #fff; padding: 4rem 2rem; text-align: center; }
        .hero h1 { color: #fff; }
        .hero p { color: #e0e0e0; }
        .card { background: #fff; border-radius: 8px; padding: 2rem; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
        label { display: block; font-weight: 600; margin-bottom: 0.25rem; }
        input, textarea { width: 100%; padding: 0.5rem; margin-bottom: 1rem; border: 1px solid #ccc; border-radius: 4px; font-size: 1rem; }
        textarea { resize: vertical; min-height: 120px; }
        button { background: #667eea; color: #fff; border: none; padding: 0.75rem 2rem; border-radius: 4px; font-size: 1rem; cursor: pointer; }
        button:hover { background: #5a6fd6; }
    </style>
</head>
<body>
    <nav>
        <a class="brand" href="/">Hello .NET</a>
        <a href="/">Home</a>
        <a href="/about">About</a>
        <a href="/contact">Contact</a>
    </nav>
    {{body}}
    <footer style="text-align:center; padding:2rem; margin-top:3rem; border-top:1px solid #ddd; color:#888; font-size:0.9rem;">
        <a href="/openapi/v1.json" style="color:#667eea;">OpenAPI Spec</a>
    </footer>
</body>
</html>
""";

app.MapGet("/", () => Results.Content(Layout("Home", """
<div class="hero">
    <h1>Welcome to Hello .NET</h1>
    <p>A minimal ASP.NET Core application with a clean landing page.</p>
</div>
<div class="container">
    <div class="card">
        <h2 style="margin-bottom:0.5rem;">Get Started</h2>
        <p>This app exposes a REST API at <code>/api/people</code> and serves these landing pages.</p>
    </div>
</div>
"""), "text/html"));

app.MapGet("/about", () => Results.Content(Layout("About", """
<div class="container">
    <h1>About</h1>
    <div class="card">
        <p>Hello .NET is a lightweight web application built with ASP.NET Core minimal APIs and Entity Framework Core with an in-memory database.</p>
        <p>It demonstrates how to combine a simple REST API with server-rendered HTML pages in a single project.</p>
    </div>
</div>
"""), "text/html"));

app.MapGet("/contact", () => Results.Content(Layout("Contact", """
<div class="container">
    <h1>Contact</h1>
    <div class="card">
        <form>
            <label for="name">Name</label>
            <input type="text" id="name" name="name" placeholder="Your name" />
            <label for="email">Email</label>
            <input type="email" id="email" name="email" placeholder="you@example.com" />
            <label for="message">Message</label>
            <textarea id="message" name="message" placeholder="How can we help?"></textarea>
            <button type="submit">Send</button>
        </form>
    </div>
</div>
"""), "text/html"));

// --- People endpoints ---

var people = app.MapGroup("/api/people");

people.MapGet("/", async (AppDbContext db) =>
    await db.People.Select(p => new
    {
        p.Id,
        p.Name,
        p.Age,
        p.Hobbies
    }).ToListAsync());

people.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.People.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id)
        is Person person
            ? Results.Ok(new
            {
                person.Id,
                person.Name,
                person.Age,
                person.Hobbies,
                Tasks = person.Tasks.Select(t => new { t.Id, t.Title, Status = t.Status.ToString() })
            })
            : Results.NotFound());

people.MapPost("/", async (CreatePersonRequest request, AppDbContext db) =>
{
    var person = new Person
    {
        Name = request.Name,
        Age = request.Age,
        Hobbies = request.Hobbies ?? []
    };
    db.People.Add(person);
    await db.SaveChangesAsync();
    return Results.Created($"/api/people/{person.Id}", new { person.Id, person.Name, person.Age, person.Hobbies });
});

people.MapPut("/{id:int}", async (int id, UpdatePersonRequest request, AppDbContext db) =>
{
    var person = await db.People.FindAsync(id);
    if (person is null) return Results.NotFound();

    person.Name = request.Name;
    person.Age = request.Age;
    person.Hobbies = request.Hobbies ?? [];
    await db.SaveChangesAsync();
    return Results.Ok(new { person.Id, person.Name, person.Age, person.Hobbies });
});

people.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var person = await db.People.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id);
    if (person is null) return Results.NotFound();

    db.People.Remove(person);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// --- Task endpoints ---

var tasks = app.MapGroup("/api/people/{personId:int}/tasks");

tasks.MapGet("/", async (int personId, AppDbContext db) =>
{
    if (!await db.People.AnyAsync(p => p.Id == personId))
        return Results.NotFound();

    var items = await db.Tasks
        .Where(t => t.PersonId == personId)
        .Select(t => new { t.Id, t.Title, Status = t.Status.ToString() })
        .ToListAsync();

    return Results.Ok(items);
});

tasks.MapGet("/{taskId:int}", async (int personId, int taskId, AppDbContext db) =>
{
    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.PersonId == personId);
    return task is not null
        ? Results.Ok(new { task.Id, task.Title, Status = task.Status.ToString(), task.PersonId })
        : Results.NotFound();
});

tasks.MapPost("/", async (int personId, CreateTaskRequest request, AppDbContext db) =>
{
    if (!await db.People.AnyAsync(p => p.Id == personId))
        return Results.NotFound();

    var task = new TaskItem
    {
        Title = request.Title,
        Status = request.Status ?? TaskStatus.Draft,
        PersonId = personId
    };
    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    return Results.Created($"/api/people/{personId}/tasks/{task.Id}",
        new { task.Id, task.Title, Status = task.Status.ToString(), task.PersonId });
});

tasks.MapPut("/{taskId:int}", async (int personId, int taskId, UpdateTaskRequest request, AppDbContext db) =>
{
    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.PersonId == personId);
    if (task is null) return Results.NotFound();

    task.Title = request.Title;
    task.Status = request.Status;
    await db.SaveChangesAsync();
    return Results.Ok(new { task.Id, task.Title, Status = task.Status.ToString(), task.PersonId });
});

tasks.MapDelete("/{taskId:int}", async (int personId, int taskId, AppDbContext db) =>
{
    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.PersonId == personId);
    if (task is null) return Results.NotFound();

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

// --- Request DTOs ---

record CreatePersonRequest(string Name, int Age, List<string>? Hobbies);
record UpdatePersonRequest(string Name, int Age, List<string>? Hobbies);
record CreateTaskRequest(string Title, TaskStatus? Status);
record UpdateTaskRequest(string Title, TaskStatus Status);
