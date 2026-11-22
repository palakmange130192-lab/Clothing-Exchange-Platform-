using ClothExchange.Data;
using ClothExchange.Middleware;

var builder = WebApplication.CreateBuilder(args);

// MVC (controllers + Razor views)
builder.Services.AddControllersWithViews(options =>
{
    // Don't treat every non-nullable string as [Required] automatically
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Session is used to remember the logged-in user (it needs a place to store its data)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Dependency Injection: one DbHelper object shared by all controllers
builder.Services.AddSingleton<DbHelper>();

var app = builder.Build();

// Create the database and tables the first time the app runs
app.Services.GetRequiredService<DbHelper>().EnsureCreated();

// Middleware pipeline (order matters)
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseMiddleware<RequestLoggerMiddleware>();   // our own middleware (prints each request)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
