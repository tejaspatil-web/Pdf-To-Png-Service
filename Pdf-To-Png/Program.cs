using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Pdf_To_Png.Services;
using Microsoft.IdentityModel.Logging;

IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on the specified port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// CORS
var allowedOrigins = new[]
{
    "https://chatfusionx.web.app",
    "http://localhost:4200"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMultipleOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JWT AUTHENTICATION 
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key is missing in configuration!");
}

// JWT Authentication Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        RequireSignedTokens = true,
        RequireExpirationTime = true,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        ),

        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },

        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

builder.Services.AddAuthorization();

// SERVICES
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IPdfToPngService, PdfToPngService>();

var app = builder.Build();


// MIDDLEWARE PIPELINE CONFIGURATION
//app.UseHttpsRedirection();

app.UseCors("AllowMultipleOrigins");

// JWT
app.UseAuthentication();
app.UseAuthorization();

// SERVICE KEY MIDDLEWARE
app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    if (path.StartsWithSegments("/swagger") ||
        path.StartsWithSegments("/health") ||
        path == "/")
    {
        await next();
        return;
    }

    if (!context.Request.Headers.TryGetValue("X-SERVICE-KEY", out var key))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Missing Service Key");
        return;
    }

    var validKey = builder.Configuration["ServiceSettings:ServiceKey"];

    if (!string.Equals(validKey, key))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Invalid Service Key");
        return;
    }

    await next();
});

// SWAGGER
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ENDPOINTS
app.MapControllers();

app.MapGet("/", () => "PDF to PNG Service Running");

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "OK",
        time = DateTime.UtcNow
    });
});

app.Run();