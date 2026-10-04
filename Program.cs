using E_Learning.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//
// ============================================================
// 1. APPLICATION SERVICES CONFIGURATION
// ============================================================
//
// Services registered here are added to ASP.NET Core's
// Dependency Injection (DI) container. These services can
// then be injected into controllers, Razor Pages, and other
// application components.
//

// Read the database connection string from appsettings.json
// or another configured configuration source.
// The application cannot start without a valid connection string.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// Register Entity Framework Core and configure it to use
// SQL Server with the connection string defined above.
//
// ApplicationDbContext is responsible for communicating with
// the application's database and managing entities such as
// users, roles, and other application data.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Enable the Entity Framework Core developer exception page.
// This provides more detailed database/migration errors during
// development and is intended for development environments.
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


// ------------------------------------------------------------
// ASP.NET Core Identity
// ------------------------------------------------------------
//
// Configure Identity for user authentication and authorization.
//
// IdentityUser:
//   Represents users managed by ASP.NET Core Identity.
//
// AddRoles<IdentityRole>:
//   Enables role-based authorization, allowing users to be
//   assigned roles such as Admin, Teacher, or Student.
//
// AddEntityFrameworkStores:
//   Stores Identity users, roles, and related data in the
//   application's Entity Framework Core database.
//

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Users do not need to confirm their account before signing in.
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();


// Register MVC controllers and views so the application can
// handle requests through controllers and Razor views.
builder.Services.AddControllersWithViews();


//
// ============================================================
// 2. BUILD THE APPLICATION
// ============================================================
//

// Build the WebApplication using the services and configuration
// registered above.
var app = builder.Build();


//
// ============================================================
// 3. DATABASE SEEDING
// ============================================================
//
// SeedData.InitializeAsync() inserts required initial data
// into the database, such as default users, roles, or other
// application-specific records.
//
// A service scope is created because scoped services such as
// ApplicationDbContext should be resolved within an appropriate
// dependency-injection scope.
//

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    await SeedData.InitializeAsync(services);
}


//
// ============================================================
// 4. HTTP REQUEST PIPELINE CONFIGURATION
// ============================================================
//
// Middleware controls how incoming HTTP requests are processed.
// The order of these middleware components is important.
//

if (app.Environment.IsDevelopment())
{
    // In development, show the Entity Framework Core migration
    // endpoint. This makes it easier to apply pending database
    // migrations while developing the application.
    app.UseMigrationsEndPoint();
}
else
{
    // In production, redirect unhandled exceptions to the
    // application's generic error page instead of exposing
    // detailed exception information to users.
    app.UseExceptionHandler("/Home/Error");

    // Enable HTTP Strict Transport Security (HSTS).
    // This tells browsers to use HTTPS when communicating with
    // the application for future requests.
    app.UseHsts();
}


// Redirect HTTP requests to HTTPS to keep communication secure.
app.UseHttpsRedirection();


// Enable ASP.NET Core routing so incoming URLs can be matched
// to controllers, actions, Razor Pages, and other endpoints.
app.UseRouting();


// Enable authorization middleware.
//
// Authorization determines whether the currently authenticated
// user has permission to access a requested resource.
app.UseAuthorization();


// Serve static files such as CSS, JavaScript, images, and other
// assets used by the application's frontend.
app.MapStaticAssets();


//
// ============================================================
// 5. MVC ROUTING
// ============================================================
//
// Define the default MVC route used when a URL does not explicitly
// specify a controller and action.
//
// Example:
//     /
//     -> EntryController
//     -> Index action
//
// Therefore, the application's default entry point is:
//
//     EntryController.Index()
//
// The {id?} parameter is optional and can be used for routes such as:
//     /Entry/Details/5
//

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Entry}/{action=Index}/{id?}")
    .WithStaticAssets();


//
// ============================================================
// 6. RAZOR PAGES
// ============================================================
//
// Map Razor Pages endpoints.
//
// This is required for pages implemented using Razor Pages,
// including ASP.NET Core Identity's default UI.
//

app.MapRazorPages()
   .WithStaticAssets();


//
// ============================================================
// 7. START THE APPLICATION
// ============================================================
//
// Start the ASP.NET Core application and begin listening for
// incoming HTTP requests.
//

app.Run();