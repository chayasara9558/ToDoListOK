using Microsoft.EntityFrameworkCore;
using TodoApi; 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// --- 1. הגדרות שירותים (Services) ---

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin() // במצב ששניהם באותו שרת, זה פחות קריטי אבל עוזר לבדיקות
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddDbContext<ToDoDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// מפתח סודי ל-JWT
var key = Encoding.ASCII.GetBytes("A_Very_Long_Secret_Key_For_My_Todo_App_123!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// --- 2. הגדרות Pipeline (Middleware) ---

// הגדרות להגשת ה-React מתוך תיקיית wwwroot
app.UseDefaultFiles(); 
app.UseStaticFiles();

// ניסיון יצירת טבלאות עם טיפול בשגיאות
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS Users (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Username VARCHAR(255) NOT NULL,
                Password VARCHAR(255) NOT NULL
            );
        ");
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS Items (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(255),
                IsComplete TINYINT(1),
                UserId INT
            );
        ");
        Console.WriteLine("Database tables checked/created successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration failed: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// --- 3. נתיבים (Routes) ---

// חשוב: אם ה-React לא נטען, הנתיב הזה יוודא שה-API חי
app.MapGet("/health", () => "API is Running!");

// הרשמה (Register)
app.MapPost("/register", async (ToDoDbContext db, User newUser) =>
{
    if (string.IsNullOrEmpty(newUser.Username) || string.IsNullOrEmpty(newUser.Password))
        return Results.BadRequest(new { message = "Username and password are required" });

    var exists = await db.Users.AnyAsync(u => u.Username == newUser.Username);
    if (exists) return Results.BadRequest(new { message = "Username already exists" });

    db.Users.Add(newUser);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "User registered successfully" });
});

// התחברות (Login)
app.MapPost("/login", async (ToDoDbContext db, User loginUser) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => 
        u.Username == loginUser.Username && u.Password == loginUser.Password);
    
    if (user is null) return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] { 
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("id", user.Id.ToString()) 
        }),
        Expires = DateTime.UtcNow.AddDays(7),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { token = tokenHandler.WriteToken(token) });
});

// שליפת משימות
app.MapGet("/items", async (ToDoDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirst("id")?.Value;
    if (userIdClaim == null) return Results.Unauthorized();

    var userTasks = await db.Items
        .Where(t => t.UserId == int.Parse(userIdClaim))
        .ToListAsync();

    return Results.Ok(userTasks);
}).RequireAuthorization();

// הוספת משימה
app.MapPost("/items", async (ToDoDbContext db, Item newItem, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirst("id")?.Value;
    if (userIdClaim == null) return Results.Unauthorized();

    newItem.UserId = int.Parse(userIdClaim);
    db.Items.Add(newItem);
    await db.SaveChangesAsync();
    return Results.Created($"/items/{newItem.Id}", newItem);
}).RequireAuthorization();

// מחיקת משימה
app.MapDelete("/items/{id}", async (ToDoDbContext db, int id, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirst("id")?.Value;
    if (userIdClaim == null) return Results.Unauthorized();

    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    
    if (item.UserId != int.Parse(userIdClaim)) return Results.Forbid();

    db.Items.Remove(item);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Deleted", id });
}).RequireAuthorization();

// פתרון לבעיית ה-Refresh ב-React: כל נתיב לא מזוהה יחזיר את index.html
app.MapFallbackToFile("index.html");

app.Run();
