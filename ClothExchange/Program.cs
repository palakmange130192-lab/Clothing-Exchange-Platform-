using ClothExchange.Data;
using ClothExchange.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Services (Dependency Injection container)
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();   // session needs a place to store data
builder.Services.AddSession();                  // enables Session (used for login)
builder.Services.AddScoped<DbHelper>();         // our class that gives database connections

var app = builder.Build();

// Request pipeline (order matters, each one is a middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();                            // css, images, js
app.UseSession();                                // must come before the controllers
app.UseMiddleware<RequestLoggerMiddleware>();    // our own middleware
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
