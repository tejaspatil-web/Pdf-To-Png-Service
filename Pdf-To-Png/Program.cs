using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Pdf_To_Png.Services;
using Microsoft.IdentityModel.Logging;

IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on the specified port
var port = Environment.GetEnvironmentVariable("PORT");

if (string.IsNullOrEmpty(port))
{
    throw new Exception("PORT environment variable is not set!");
}

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


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
if (app.Environment.IsDevelopment())
{
  app.UseHttpsRedirection();
}

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

    var validKey = builder.Configuration["ServiceSettings:ServiceKey"];

    if (string.IsNullOrEmpty(validKey))
    {
        Console.WriteLine("Service key NOT configured!");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Server misconfiguration");
        return;
    }

    if (!context.Request.Headers.TryGetValue("X-SERVICE-KEY", out var key))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Missing Service Key");
        return;
    }

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

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine("CRASH: " + ex.ToString());
        throw;
    }
});

app.Run();