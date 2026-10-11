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

builder.Services.AddControllers(); // Needed for AccountController & CultureController
builder.Services.AddLocalization();

var supportedCultures = new[] { "vi-VN", "vi", "en-US", "en" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("vi-VN")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

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
builder.Services.AddTransient<BlazorAuthorizationMessageHandler>();

builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri("http://localhost:5011/"))
    .AddHttpMessageHandler<BlazorAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client => client.BaseAddress = new Uri("http://localhost:5011/"));

builder.Services.AddScoped<DashboardApiClient>();
builder.Services.AddScoped<MenuApiClient>();
builder.Services.AddScoped<TableApiClient>();
builder.Services.AddHttpClient<AdminApiClient>(client => client.BaseAddress = new Uri("http://localhost:5011/"))
    .AddHttpMessageHandler<BlazorAuthorizationMessageHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("vi-VN")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers(); // Map MVC Controllers
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
