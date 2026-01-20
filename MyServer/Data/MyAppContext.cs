namespace MyServer.Data;

using Microsoft.EntityFrameworkCore;
using MyServer.Models;

public class MyAppContext : DbContext
{
    public DbSet<Person> Persons { get; set; }

    public MyAppContext(DbContextOptions<MyAppContext> options)
        : base(options)
    {
        //Database.EnsureCreated();   // создаем базу данных при первом обращении
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>();

        //modelBuilder.Entity<User>().HasData(
        //        new User { Id = 1, Name = "Tom", Age = 37 },
        //        new User { Id = 2, Name = "Bob", Age = 41 },
        //        new User { Id = 3, Name = "Sam", Age = 24 }
        //);
    }
}
