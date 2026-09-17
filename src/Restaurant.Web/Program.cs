using Microsoft.AspNetCore.Authentication.Cookies;
using Restaurant.Web.Components;
using Restaurant.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddProvider(new FileLoggerProvider());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });

builder.Services.AddControllers(); // Needed for AccountController
builder.Services.AddLocalization();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
        options.Cookie.Name = "RestaurantAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// HttpClients
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri("http://localhost:5011/"));
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client => client.BaseAddress = new Uri("http://localhost:5011/"));

builder.Services.AddScoped<DashboardApiClient>();
builder.Services.AddScoped<MenuApiClient>();
builder.Services.AddScoped<TableApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers(); // Map MVC Controllers
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
