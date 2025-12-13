using LeakDetectionDashboard.Background;
using LeakDetectionDashboard.Components;
using LeakDetectionDashboard.Data;
using LeakDetectionDashboard.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor Components (Blazor) with interactive server mode
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// DbContext (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient for Fake IoT backend
builder.Services.AddHttpClient<FakeIotClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    // 👇 default to your FastAPI server URL
    var baseUrl = config["FakeIot:BaseUrl"] ?? "http://localhost:8000/";
    client.BaseAddress = new Uri(baseUrl);
});

// Domain services
builder.Services.AddScoped<LeakDetectionService>();
builder.Services.AddSingleton<LeakDetectionModelService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<LoggingService>();

// Background polling service
builder.Services.AddHostedService<SensorPollingService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// ❌ we don’t have HTTPS configured → avoid redirection warning
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Map the root Razor component (App) for interactive server render mode
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
