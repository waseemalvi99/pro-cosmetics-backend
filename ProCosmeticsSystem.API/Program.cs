using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProCosmeticsSystem.API;
using ProCosmeticsSystem.API.Endpoints;
using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application;
using ProCosmeticsSystem.Infrastructure;
using ProCosmeticsSystem.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Layer DI
builder.Services.AddApi(builder.Environment);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// OpenAPI
builder.Services.AddOpenApi();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!)),
        ClockSkew = TimeSpan.Zero
    };

    // SignalR token support
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed admin user & permissions on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ProCosmeticsSystem.Infrastructure.Services.DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowFrontend");
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapAuthEndpoints();
app.MapUserManagementEndpoints();
app.MapRoleEndpoints();
app.MapCustomerEndpoints();
app.MapProductEndpoints();
app.MapProductImageEndpoints();
app.MapCategoryEndpoints();
app.MapInventoryEndpoints();
app.MapSupplierEndpoints();
app.MapPurchaseOrderEndpoints();
app.MapSalesmanEndpoints();
app.MapSaleEndpoints();
app.MapDeliveryEndpoints();
app.MapDeliveryManEndpoints();
app.MapNotificationEndpoints();
app.MapReportEndpoints();

// SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
