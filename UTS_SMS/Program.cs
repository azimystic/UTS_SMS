using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using SMS;
using UTS_SMS.Models;
using SMS.Services;

var builder = WebApplication.CreateBuilder(args);

// Add response compression for better performance
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});

// Add caching
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(maxRetryCount: 3)));


// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Session expires after 10 minutes of inactivity
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.SlidingExpiration = true;

    // When expired → immediately redirect instead of showing partial content
    options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
    {
        OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect("/Account/Login");
            return Task.CompletedTask;
        },
        OnRedirectToLogin = context =>
        {
            context.Response.Redirect("/Account/Login");
            return Task.CompletedTask;
        }
    };
});

// Add custom services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IExtraChargeService, ExtraChargeService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISalaryDeductionService, SalaryDeductionService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<ReportService>();

// Add background services
builder.Services.AddHostedService<SalaryDeductionBackgroundService>();
builder.Services.AddHostedService<NotificationBackgroundService>();
builder.Services.AddHostedService<LeaveBalanceRolloverService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseResponseCompression();
app.UseResponseCaching();
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 30 days
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Initialize roles and create default admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userService = services.GetRequiredService<IUserService>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    try
    {
        await userService.InitializeRolesAsync();
        
        var adminExists = await userManager.GetUsersInRoleAsync("Admin");
        if (!adminExists.Any())
        {
            var result = await userService.CreateAdminUserAsync("campus1@gmail.com", "System Administrator");
            if (result.Succeeded)
            {
                Console.WriteLine("✅ Default admin user created: campus1@gmail.com");
            }
        }
        
        var ownerExists = await userManager.GetUsersInRoleAsync("Owner");
        if (!ownerExists.Any())
        {
            var result = await userService.CreateOwnerUserAsync("owner@gmail.com", "School Owner");
            if (result.Succeeded)
            {
                Console.WriteLine("✅ Default owner user created: owner@gmail.com");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error during initialization: {ex.Message}");
    }
}

app.Run();