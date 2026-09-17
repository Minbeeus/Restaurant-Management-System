using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.BackgroundServices;
using Restaurant.Infrastructure.Hubs;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Persistence.Interceptors;
using Restaurant.Infrastructure.Services;
using Restaurant.WebAPI.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext & Interceptors
builder.Services.AddSingleton<AuditableEntityInterceptor>();
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase") || string.IsNullOrEmpty(connStr))
    {
        options.UseInMemoryDatabase("RestaurantDb").AddInterceptors(interceptor);
    }
    else
    {
        options.UseSqlServer(connStr).AddInterceptors(interceptor);
    }
});

// 2. Register Application & Infrastructure Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<IModifierService, ModifierService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IKitchenService, KitchenService>();
builder.Services.AddScoped<IQrOrderService, QrOrderService>();
builder.Services.AddScoped<IPosOrderService, PosOrderService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICancelOrderService, CancelOrderService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IRestaurantNotificationService, RestaurantNotificationService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Restaurant.Infrastructure.Services.InventoryService).Assembly));
builder.Services.AddHostedService<KitchenSlaMonitoringWorker>();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache(); // Fallback for IDistributedCache when Redis is commented out

// var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = redisConnectionString;
// });
// builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString);
builder.Services.AddSignalR();

// 3. JWT Authentication & Authorization Policies
var secretKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey) && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("FATAL: 'JwtSettings:SecretKey' is missing in Production environment configuration!");
}
secretKey ??= "RestaurantManagementSecretKeySuperProtection123!";
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("CashierAccess", policy => policy.RequireRole("Admin", "Cashier"));
    options.AddPolicy("ChefAccess", policy => policy.RequireRole("Chef"));
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

var supportedCultures = new[] { "vi", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("vi")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RestaurantHub>("/hubs/restaurant");

// Auto seed Roles data on startup if DB exists
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (dbContext.Database.CanConnect())
    {
        await DbInitializer.SeedAsync(dbContext);
    }
}

app.Run();

public partial class Program { }

