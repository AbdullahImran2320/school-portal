using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolPortal.API.Data;
using SchoolPortal.API.Models;
using SchoolPortal.API.Repositories;
using SchoolPortal.API.Services;
using System.Text;
using System.Diagnostics;



var builder = WebApplication.CreateBuilder(args);

// Production desktop build: keep the console clean for non-technical users.
// EF Core command/migration diagnostics are intentionally not written to the
// console; application-level failures are still handled by ASP.NET Core.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<SchoolPortalDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// CORS is only needed in Development, when the Angular dev server (ng serve on
// :4200) calls this API from a different origin. In production the Angular
// build is served as same-origin static files from wwwroot, so no cross-origin
// requests occur and no CORS policy should be active.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular", policy =>
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });
}

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
   
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value!.Errors.Count > 0)
            .Select(e => new { field = e.Key, errors = e.Value!.Errors.Select(x => x.ErrorMessage) });
        return new BadRequestObjectResult(new { message = "Validation failed", errors });
    };
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IFeeComponentRepository, FeeComponentRepository>();
builder.Services.AddScoped<IFeeEngineService, FeeEngineService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();


builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddHttpClient("LicenseServer", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// In the installed desktop application, opening the executable itself should
// be enough: once Kestrel is ready, open the default browser automatically.
// The launcher shortcut also starts this same executable, so there is no
// separate web-server step for the user.
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://localhost:5000",
            UseShellExecute = true
        });
    }
    catch
    {
        // Browser launch is a convenience only; the web application remains running.
    }
});

// Serve the Angular production build (copied into wwwroot at publish time)
// as static files. This is what lets the installer run a single process on
// a single port instead of standing up a separate web server for the SPA.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var errorFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = errorFeature?.Error;

        // Full exception details only in Development — never leak stack
        // traces or internal messages to whatever's calling this in production.
        var detail = app.Environment.IsDevelopment() ? ex?.Message : null;
        await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred.", detail });
    });
});



// Seed one class and one parent so you can test Student CRUD immediately
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchoolPortalDbContext>();
    // Licensing table is created idempotently so an existing client database
    // receives the trial/license store without requiring EF CLI on the client machine.
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS LicenseInfos (
            Id INTEGER NOT NULL CONSTRAINT PK_LicenseInfos PRIMARY KEY AUTOINCREMENT,
            TrialStartDate TEXT NOT NULL,
            TrialEndDate TEXT NOT NULL,
            IsActivated INTEGER NOT NULL,
            LicenseKey TEXT NULL,
            LicenseStartDate TEXT NULL,
            LicenseEndDate TEXT NULL,
            LastSeenDate TEXT NULL,
            InstallationId TEXT NULL,
            SignedLicense TEXT NULL,
            LastOnlineValidationUtc TEXT NULL,
            OfflineGraceUntilUtc TEXT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
        """);

    // The license table is maintained outside EF migrations because it must be
    // available even when an older client database is opened by a newer build.
    // Check the actual SQLite schema before adding compatibility columns so a
    // normal restart never produces "duplicate column" errors in the console.
    var connection = db.Database.GetDbConnection();
    await connection.OpenAsync();

    var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    await using (var command = connection.CreateCommand())
    {
        command.CommandText = "PRAGMA table_info('LicenseInfos');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            existingColumns.Add(reader.GetString(1));
        }
    }

    var compatibilityColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["InstallationId"] = "TEXT NULL",
        ["SignedLicense"] = "TEXT NULL",
        ["LastOnlineValidationUtc"] = "TEXT NULL",
        ["OfflineGraceUntilUtc"] = "TEXT NULL"
    };

    foreach (var column in compatibilityColumns)
    {
        if (existingColumns.Contains(column.Key))
            continue;

        await db.Database.ExecuteSqlRawAsync(
            $"ALTER TABLE LicenseInfos ADD COLUMN {column.Key} {column.Value}");
    }

    await connection.CloseAsync();

    var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();
    await licenseService.InitializeAsync();

    // Migrate (not EnsureCreated) — this is what lets a future migration
    // actually apply itself to the school's already-deployed SchoolPortal.db
    // the next time the installed app starts, with no developer present to
    // run `dotnet ef database update` by hand.
    db.Database.Migrate();


    if (!db.Users.Any())
    {
        var hasher = new PasswordHasher<User>();

        var admin = new User { Username = "admin", FullName = "Administrator", Role = UserRole.Admin };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

        var accountant = new User { Username = "accountant", FullName = "Accounts Office", Role = UserRole.Accountant };
        accountant.PasswordHash = hasher.HashPassword(accountant, "Accountant@123");

        var teacher = new User { Username = "teacher", FullName = "Teacher", Role = UserRole.Teacher };
        teacher.PasswordHash = hasher.HashPassword(teacher, "Teacher@123");

        db.Users.AddRange(admin, accountant, teacher);
        db.SaveChanges();
    }


    if (!db.Classes.Any())
    {
        var classNames = new[]
        {
        "Playgroup Junior", "Nursery", "Prep",
        "Class 1", "Class 2", "Class 3", "Class 4", "Class 5",
        "Class 6", "Class 7", "Class 8", "Class 9", "Class 10"
    };
        for (int i = 0; i < classNames.Length; i++)
            db.Classes.Add(new SchoolClass { ClassName = classNames[i], Section = "", AcademicYear = "2026", PromotionOrder = i + 1 });

        db.SaveChanges();
    }


    if (!db.FeeComponents.Any())
    {
        string TierFor(string className) =>
            className is "Playgroup Junior" or "Nursery" or "Prep" ? "ECCE" :
            int.Parse(className.Split(' ')[1]) <= 5 ? "1-5" : "6-10";

        foreach (var cls in db.Classes.ToList())
        {
            var tier = TierFor(cls.ClassName);

            db.FeeComponents.Add(new FeeComponent
            {
                ClassId = cls.ClassId,
                ComponentName = "Admission Fee",
                Amount = 10000,
                Frequency = FeeFrequency.OneTime,
                AcademicYear = "2026"
            });

            if (tier == "ECCE")
            {
                db.FeeComponents.AddRange(
                    new FeeComponent { ClassId = cls.ClassId, ComponentName = "Exam Fee", Amount = 4000, Frequency = FeeFrequency.Yearly, AcademicYear = "2026" },
                    new FeeComponent { ClassId = cls.ClassId, ComponentName = "Stationery Fee", Amount = 2000, Frequency = FeeFrequency.Yearly, AcademicYear = "2026" },
                    new FeeComponent { ClassId = cls.ClassId, ComponentName = "Activity Fee", Amount = 3000, Frequency = FeeFrequency.Yearly, AcademicYear = "2026" },
                    new FeeComponent { ClassId = cls.ClassId, ComponentName = "Monthly Fee", Amount = 4000, Frequency = FeeFrequency.Monthly, AcademicYear = "2026" }
                );
            }
            else if (tier == "1-5")
            {
                db.FeeComponents.AddRange(
                    new FeeComponent { ClassId = cls.ClassId, ComponentName = "Exam Fee", Amount = 5000, Frequency = FeeFrequency.Yearly, AcademicYear = "2026" },
                    new FeeComponent { ClassId = cls.ClassId, ComponentName = "Monthly Fee", Amount = 5000, Frequency = FeeFrequency.Monthly, AcademicYear = "2026" }
                );
            }
            else
            {
                db.FeeComponents.AddRange(
                    new FeeComponent { ClassId = cls.ClassId, ComponentName = "Exam Fee", Amount = 6000, Frequency = FeeFrequency.Yearly, AcademicYear = "2026" },
                    new FeeComponent { ClassId = cls.ClassId, ComponentName = "Monthly Fee", Amount = 6000, Frequency = FeeFrequency.Monthly, AcademicYear = "2026" }
                );
            }
        }
        db.SaveChanges();
    }


    if (!db.Parents.Any())
    {
        db.Parents.Add(new Parent
        {
            FatherName = "Muhammad Imran",
            FatherMobile = "0300-1234567",
            FatherOccupation = "Businessman",
            MotherName = "Uzma Imran",
            MotherMobile = "0300-7654321",
            PrimaryGuardian = PrimaryGuardian.Father,
            Address = "Model Town, Lahore"
        });
    }

    db.SaveChanges();
}
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAngular");
}
app.UseAuthentication();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;

    var bypass = path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("/api/license", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

    if (bypass || context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    if (context.User.Identity?.IsAuthenticated == true)
    {
        var licenseService = context.RequestServices.GetRequiredService<ILicenseService>();
        if (!await licenseService.CanUsePortalAsync())
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                code = "LICENSE_EXPIRED",
                message = "The Bright Grammar School Portal license has expired. Please renew or activate a valid license."
            });
            return;
        }
    }

    await next();
});

app.MapControllers();

// SPA fallback: any GET request that isn't an API route, a Swagger route, or
// an actual static file (JS/CSS/images already served by UseStaticFiles above)
// falls back to index.html so Angular's client-side router can handle it.
// This is what stops a hard refresh on a deep link like /students from 404ing.
app.MapFallback(async context =>
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/api") || path.StartsWithSegments("/swagger"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var indexFile = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
    await context.Response.SendFileAsync(indexFile);
});

app.Run();
