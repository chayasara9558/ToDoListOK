using Microsoft.EntityFrameworkCore;

namespace TodoApi;

public class ToDoDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Item> Items { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // ניסיון לקבל את מחרוזת החיבור המלאה מ-Render
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            // אם אין מחרוזת אחת מלאה, נבנה אותה מהמשתנים של CleverCloud
            if (string.IsNullOrEmpty(connectionString))
            {
                var host = Environment.GetEnvironmentVariable("MYSQL_ADDON_HOST") ?? "localhost";
                var db = Environment.GetEnvironmentVariable("MYSQL_ADDON_DB") ?? "todo";
                var user = Environment.GetEnvironmentVariable("MYSQL_ADDON_USER") ?? "root";
                var pass = Environment.GetEnvironmentVariable("MYSQL_ADDON_PASSWORD") ?? "your_password";
                var port = Environment.GetEnvironmentVariable("MYSQL_ADDON_PORT") ?? "3306";

                connectionString = $"Server={host};Port={port};Database={db};Uid={user};Pwd={pass};SSL Mode=Required;TrustServerCertificate=True;";
            }
            
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 30)); 
            optionsBuilder.UseMySql(connectionString, serverVersion);
        }
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging(); // יעזור לך לראות שגיאות מפורטות בלוגים
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity => {
            entity.ToTable("users"); 
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Item>(entity => {
            entity.ToTable("items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
        });
    }
}
