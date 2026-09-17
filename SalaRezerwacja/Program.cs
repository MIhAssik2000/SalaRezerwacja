using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalaRezerwacja.Data;
using SalaRezerwacja.Models;
using SalaRezerwacja.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Baza danych ----------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- 2. Logowanie i rejestracja (wymaganie 1) ----------
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// ---------- 3. Logika aplikacji ----------
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUniversityAuthProvider, DevUniversityAuthProvider>();
builder.Services.AddScoped<ScheduleImportService>();
builder.Services.AddHostedService<ReminderBackgroundService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // strony logowania/rejestracji z Identity UI

var app = builder.Build();

// ---------- 4. Migracje i dane startowe ----------
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
