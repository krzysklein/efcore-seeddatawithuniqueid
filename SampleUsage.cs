using Microsoft.EntityFrameworkCore;
using System;

public class DatabaseContext : DbContext
{
    public DbSet<Foo> Foos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This will seed DbSet<Foo> with hashed Id property
        modelBuilder.Entity<Foo>().SeedDataWithUniqueId(
            new { Name = "Foo 1" },
            new { Name = "Foo 2" }
        );
    }
}
public class Foo
{
    public Guid Id { get; }
    public string Name { get; private set; }

    private Foo() { }

    public Foo(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }
}