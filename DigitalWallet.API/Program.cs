using System.Text;
using DigitalWallet.API.Middleware;
using DigitalWallet.Application;
using DigitalWallet.Application.Settings;
using DigitalWallet.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(
    (context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
);

builder.WebHost.UseUrls("https://localhost:7123", "http://localhost:5123");

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

JwtSettings jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
            ),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:8081", "http://localhost:8080")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    );
});

// -----------------------------------------------
// BUILD THE APP
// -----------------------------------------------
var app = builder.Build();

// -----------------------------------------------
// MIDDLEWARE PIPELINE — order matters
// -----------------------------------------------

// Request logging wraps the exception handler to capture the final response status.

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { name = "Digital Wallet API", status = "running" }));

app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var url in app.Urls)
        app.Logger.LogInformation("API is listening on {Url}", url);
});

app.Run();
