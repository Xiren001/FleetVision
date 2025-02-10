using FleetVision.DBContext;
using FleetVision.Models;
using FleetVision.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // Add SignalR

// Register DbContexts
builder.Services.AddDbContext<FleetVisionContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register Identity with the correct user model
builder.Services.AddIdentity<Users, IdentityRole>(option =>
{
    option.Password.RequireDigit = true;
    option.Password.RequireLowercase = true;
    //option.Password.RequireUppercase = true;
    //option.Password.RequireNonAlphanumeric = true;
    option.Password.RequiredLength = 12;

    option.User.RequireUniqueEmail = true;
    option.SignIn.RequireConfirmedAccount = false;
    option.SignIn.RequireConfirmedEmail = false;
    option.SignIn.RequireConfirmedPhoneNumber = false;

    //option.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    //option.Lockout.MaxFailedAccessAttempts = 3;
    //option.Lockout.AllowedForNewUsers = true;

    //option.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
    //option.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
    //option.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultProvider;
    //option.Tokens.ChangePhoneNumberTokenProvider = TokenOptions.DefaultProvider;
    //option.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;

})
    .AddEntityFrameworkStores<AppDbContext>() // Ensure Identity uses AppDbContext
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

await SeedService.SeedDatabase(app.Services);

// Configure the HTTP request pipeline.
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

app.Run();
