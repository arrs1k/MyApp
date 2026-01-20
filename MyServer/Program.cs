

using MyServer.Models;
using Microsoft.EntityFrameworkCore;
using MyServer.Data;

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

string connection = builder.Configuration.GetConnectionString("DefaultConnection"); //+

// добавляем контекст ApplicationContext в качестве сервиса в приложение
builder.Services.AddDbContext<MyAppContext>(options => options.UseNpgsql(connection)); //+

var app = builder.Build();

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

app.MapDelete("/api/users/{id}", (MyAppContext db, string id) =>
{
    // получаем пользователя по id
    Person? person = db.Persons.FirstOrDefault(u => u.Id == id);

    // если не найден, отправляем статусный код и сообщение об ошибке
    if (person == null) return Results.NotFound(new { message = "Пользователь не найден" });

    // если пользователь найден, удаляем его
    db.Persons.Remove(person);

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