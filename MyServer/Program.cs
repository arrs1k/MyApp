

using MyServer.Models;
using Microsoft.EntityFrameworkCore;
using MyServer.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

var adminRole = new Role("admin");
var userRole = new Role("user");
var people = new List<User>
{
    new User("tom@gmail.com", "12345", adminRole),
    new User("bob@gmail.com", "55555", userRole),
};

var builder = WebApplication.CreateBuilder();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) 
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/accessdenied";
    });
builder.Services.AddAuthorization(); //+

string connection = builder.Configuration.GetConnectionString("DefaultConnection"); 

// добавляем контекст ApplicationContext в качестве сервиса в приложение
builder.Services.AddDbContext<MyAppContext>(options => options.UseNpgsql(connection));

var app = builder.Build();

app.UseAuthentication(); //+
app.UseAuthorization(); //+

app.MapGet("/accessdenied", async (HttpContext context) =>
{
    context.Response.StatusCode = 403;
    await context.Response.WriteAsync("Access Denied");
});
app.MapGet("/login", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    // html-форма для ввода логина/пароля
    string loginForm = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8' />
        <title>METANIT.COM</title>
    </head>
    <body>
        <h2>Login Form</h2>
        <form method='post'>
            <p>
                <label>Email</label><br />
                <input name='email' />
            </p>
            <p>
                <label>Password</label><br />
                <input type='password' name='password' />
            </p>
            <input type='submit' value='Login' />
        </form>
    </body>
    </html>";
    await context.Response.WriteAsync(loginForm);
});

app.MapPost("/login", async (string? returnUrl, HttpContext context) =>
{
    // получаем из формы email и пароль
    var form = context.Request.Form;
    // если email и/или пароль не установлены, посылаем статусный код ошибки 400
    if (!form.ContainsKey("email") || !form.ContainsKey("password"))
        return Results.BadRequest("Email и/или пароль не установлены");
    string email = form["email"];
    string password = form["password"];

    // находим пользователя 
    User? user = people.FirstOrDefault(p => p.Email == email && p.Password == password);
    // если пользователь не найден, отправляем статусный код 401
    if (user is null) return Results.Unauthorized();
    var claims = new List<Claim>
    {
        new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email),
        new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.Name)
    };
    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
    await context.SignInAsync(claimsPrincipal);
    return Results.Redirect(returnUrl ?? "/");
});
// доступ только для роли admin
app.Map("/admin", [Authorize(Roles = "admin")] () => "Admin Panel");

// доступ только для ролей admin и user
app.Map("/", [Authorize(Roles = "admin, user")] (HttpContext context) =>
{
    var login = context.User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
    var role = context.User.FindFirst(ClaimsIdentity.DefaultRoleClaimType);
    return $"Name: {login?.Value}\nRole: {role?.Value}";
});
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return "Данные удалены";
});

app.UseCors("AllowAll");

app.MapGet("/api/users", async (MyAppContext context) =>
{
    var p = await context.Persons.ToListAsync();

    return Results.Json(p);
});

app.MapGet("/api/users/{id}", async (MyAppContext db, string id) =>
{
    // получаем пользователя по id
    Person? person = db.Persons.FirstOrDefault(u => u.Id == id);
    // если не найден, отправляем статусный код и сообщение об ошибке
    if (person == null) return Results.NotFound(new { message = "Пользователь не найден" });

    // если пользователь найден, отправляем его
    return Results.Json(person);
});

app.MapDelete("/api/users/{id}", async (MyAppContext db, string id) =>
{
    // получаем пользователя по id
    Person? person = db.Persons.FirstOrDefault(u => u.Id == id);

    // если не найден, отправляем статусный код и сообщение об ошибке
    if (person == null) return Results.NotFound(new { message = "Пользователь не найден" });

    // если пользователь найден, удаляем его
    db.Persons.Remove(person);

    await db.SaveChangesAsync();

    return Results.Json(person);
});

app.MapPost("/api/users", async (MyAppContext db, Person person) => {

    // устанавливаем id для нового пользователя
    person.Id = Guid.NewGuid().ToString();

    // добавляем пользователя в список
    db.Persons.Add(person);

    await db.SaveChangesAsync();

    return person;
});

app.MapPut("/api/users", (MyAppContext db, Person userData) => {

    // получаем пользователя по id
    var user = db.Persons.FirstOrDefault(u => u.Id == userData.Id);
    // если не найден, отправляем статусный код и сообщение об ошибке
    if (user == null) return Results.NotFound(new { message = "Пользователь не найден" });
    // если пользователь найден, изменяем его данные и отправляем обратно клиенту

    user.Age = userData.Age;
    user.Name = userData.Name;
    user.Gender = userData.Gender;

    return Results.Json(user);
});

app.Run();