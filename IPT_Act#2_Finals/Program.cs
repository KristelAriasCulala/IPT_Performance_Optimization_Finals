using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;

var builder = WebApplication.CreateBuilder(args);

// Enable MiniProfiler
builder.Services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler"; // Profiler URL
    options.ColorScheme = StackExchange.Profiling.ColorScheme.Auto;
    options.PopupRenderPosition = StackExchange.Profiling.RenderPosition.BottomLeft;
    options.PopupShowTimeWithChildren = true;
    options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();
    options.TrackConnectionOpenClose = true;
});

// Add caching services
builder.Services.AddMemoryCache();

// Remove session support to optimize performance
// builder.Services.AddSession();  // ❌ Remove this line if session is not needed

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Use MiniProfiler Middleware
app.UseMiniProfiler();

// Use caching (if needed)
app.UseResponseCaching();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Remove session middleware if not needed
// app.UseSession();  // ❌ Remove this line

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Login}/{id?}");

app.Run();