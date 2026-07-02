using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class StationeryContext : DbContext
    {
        public StationeryContext(DbContextOptions<StationeryContext> options)
            : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SpendingLimit> SpendingLimits { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            // Configure Brand -> Category relationship
            modelBuilder.Entity<Brand>(entity =>
            {
                entity.HasMany(b => b.Categories)
                    .WithOne(c => c.Brand)
                    .HasForeignKey(c => c.BrandID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Category -> Item relationship
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasMany(c => c.Items)
                    .WithOne(i => i.Category)
                    .HasForeignKey(i => i.CategoryID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Request relationships
            modelBuilder.Entity<Request>(entity =>
            {
                // Employee relationship
                entity.HasOne(r => r.Employee)
                    .WithMany(e => e.Requests)
                    .HasForeignKey(r => r.EmployeeNumber)
                    .OnDelete(DeleteBehavior.Cascade);

                // Item relationship
                entity.HasOne(r => r.Item)
                    .WithMany()
                    .HasForeignKey(r => r.ItemID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure SpendingLimit unique constraint
            modelBuilder.Entity<SpendingLimit>()
                .HasIndex(s => s.Role)
                .IsUnique();

            // Seed initial admin employee
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    EmployeeNumber = 101,
                    Name = "Shakeel",
                    Password = "admin01",
                    Email = "Shakeel@gmail.com",
                    Role = "Admin",
                    SuperiorEmployeeNumber = null
                },
                new Employee
                {
                    EmployeeNumber = 102,
                    Name = "Muteeba",
                    Password = "manager01",
                    Email = "Muteeba@gmail.com",
                    Role = "Manager",
                    SuperiorEmployeeNumber = 101
                }, 
                new Employee
                {
                    EmployeeNumber = 103,
                    Name = "Omama",
                    Password = "manager02",
                    Email = "Omama@gmail.com",
                    Role = "Manager",
                    SuperiorEmployeeNumber = 101
                },
                new Employee
                {
                    EmployeeNumber = 104,
                    Name = "Pardhan",
                    Password = "employee01",
                    Email = "Pardhan@gmail.com",
                    Role = "Employee",
                    SuperiorEmployeeNumber = 102
                },
                new Employee
                {
                    EmployeeNumber = 105,
                    Name = "Sheezan",
                    Password = "sheezan01",
                    Email = "Sheezan@gmail.com",
                    Role = "Employee",
                    SuperiorEmployeeNumber = 103
                }
            );
            modelBuilder.Entity<Brand>().HasData(
    new Brand { BrandID = 1, BrandName = "Dollar" },
    new Brand { BrandID = 2, BrandName = "Pioneer" },
    new Brand { BrandID = 3, BrandName = "Leitz" }
);
            modelBuilder.Entity<Category>().HasData(
               new Category { CategoryID = 1, CategoryName = "Pencil", BrandID = 2 },
               new Category { CategoryID = 2, CategoryName = "Eraser", BrandID = 1 },
               new Category { CategoryID = 3, CategoryName = "Pen", BrandID = 3 }
           );
            modelBuilder.Entity<Item>().HasData(
                new Item { ItemID = 1, ItemName = "Shakalaka Bombom", QuantityAvailable = 200, Cost = 40, CategoryID = 2, BrandID=1 },
new Item { ItemID = 2, ItemName = "Parker Jotter Pen", QuantityAvailable = 150, Cost = 120, CategoryID = 1, BrandID = 2 },
new Item { ItemID = 3, ItemName = "Faber-Castell Eraser", QuantityAvailable = 300, Cost = 20, CategoryID = 3, BrandID = 3 },
new Item { ItemID = 4, ItemName = "Classmate Notebook", QuantityAvailable = 100, Cost = 80, CategoryID = 2, BrandID = 3 },
new Item { ItemID = 5, ItemName = "3M Post-it Notes", QuantityAvailable = 250, Cost = 60, CategoryID = 3, BrandID = 1 },
new Item { ItemID = 6, ItemName = "Staedtler HB Pencil", QuantityAvailable = 500, Cost = 15, CategoryID = 1, BrandID = 2 },
new Item { ItemID = 7, ItemName = "Pentel Marker Pen", QuantityAvailable = 180, Cost = 50, CategoryID = 1, BrandID = 3 },
new Item { ItemID = 8, ItemName = "Leitz Lever Arch File", QuantityAvailable = 120, Cost = 150, CategoryID = 2, BrandID = 2 },
new Item { ItemID = 9, ItemName = "Camlin Geometry Box", QuantityAvailable = 90, Cost = 200, CategoryID = 3, BrandID = 2 },
new Item { ItemID = 10, ItemName = "Deli Stapler", QuantityAvailable = 160, Cost = 75, CategoryID = 2, BrandID = 1 }

                );
            // Optional: Seed initial spending limits
            modelBuilder.Entity<SpendingLimit>().HasData(
                new SpendingLimit { Id = 1, Role = "Employee", Limit = 1000 },
                new SpendingLimit { Id = 2, Role = "Manager", Limit = 5000 },
                new SpendingLimit { Id = 3, Role = "Admin", Limit = 100000 }
            );
        }
    }
}