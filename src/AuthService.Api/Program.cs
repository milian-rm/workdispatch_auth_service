using AuthService.Api.Extensions;
using AuthService.Api.Middlewares;
using AuthService.Api.ModelBinders;
using AuthService.Persistence.Data;
using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;
using Serilog;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);


// CONFIGURACIÓN
// FIX: Bypass SSL (Cloudinary, etc.)
System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;


// Configura Serilog como el motor de registro (logging) principal de tu aplicación
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));


// Integra la configuración de FileDataModelBinderProvider.cs
builder.Services.AddControllers(options =>
{
    // Agregar el model binder para IFileData
    options.ModelBinderProviders.Insert(0, new FileDataModelBinderProvider());
})
.AddJsonOptions(o =>
{
    // Estandarizar las respuestas en camelCase para coincidir con auth-node
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});


// CONFIGURACIÓN DE SERVICIOS POR MEDIO DE MÉTODOS DE EXTENSIÓN
builder.Services.AddApiDocumentation();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration); // Esto habilita el esquema JWT
builder.Services.AddRateLimitingPolicies();


// INTEGRAR SERVICIOS DE SEGURIDAD
builder.Services.AddSecurityPolicies(builder.Configuration);
builder.Services.AddSecurityOptions();


builder.Services.AddEndpointsApiExplorer();

// CONFIGURACIÓN DE SWAGGER (Documentación + Seguridad JWT)
builder.Services.AddSwaggerGen(options =>
{
    // 1. Configuración básica de la Doc
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "AuthService API", 
        Version = "v1",
        Description = "API de autenticación para el proyecto WorkDispatch"
    });

    // 2. Incluir comentarios XML (Para que el cliente vea tus <summary>)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // 3. Configurar el esquema de seguridad (El botón "Authorize")
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT. Ingresa 'Bearer' [espacio] y tu token.\r\n\r\n Ejemplo: \"Bearer eyJhbGci...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});



var app = builder.Build();


// CONFIGURACIÓN DE HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add Serilog request logging
app.UseSerilogRequestLogging();

// Add Security Headers using NetEscapades package
app.UseSecurityHeaders(policies => policies
    .AddDefaultSecurityHeaders()
    .RemoveServerHeader()
    .AddFrameOptionsDeny()
    .AddXssProtectionBlock()
    .AddContentTypeOptionsNoSniff()
    .AddReferrerPolicyStrictOriginWhenCrossOrigin()
    .AddContentSecurityPolicy(builder =>
    {
        builder.AddDefaultSrc().Self();
        builder.AddScriptSrc().Self().UnsafeInline();
        builder.AddStyleSrc().Self().UnsafeInline();
        builder.AddImgSrc().Self().Data();
        builder.AddFontSrc().Self().Data();
        builder.AddConnectSrc().Self().From("https://localhost:5173");
        builder.AddFrameAncestors().None();
        builder.AddBaseUri().Self();
        builder.AddFormAction().Self();
    })
    .AddCustomHeader("Permissions-Policy", "geolocation=(), microphone=(), camera=()")
    .AddCustomHeader("Cache-Control", "no-store, no-cache, must-revalidate, private")
);

// Global exception handling
app.UseMiddleware<GlobalExceptionMiddleware>();


// Core middlewares
//app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy"); // Asegúrate de que coincida con lo configurado en SecurityPolicies
app.UseRateLimiter();

// EL ORDEN ES CRÍTICO AQUÍ:
app.UseAuthentication(); // Primero identifica quién es (JWT)
app.UseAuthorization();  // Luego verifica si tiene permiso ([Authorize])

app.MapControllers();


// Health check endpoints
app.MapHealthChecks("/health");

app.MapGet("/api/v1/health", () =>
{
    var response = new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    };
    return Results.Ok(response);
});



// Startup log
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var addresses = (IEnumerable<string>?)addressesFeature?.Addresses ?? app.Urls;

        if (addresses != null && addresses.Any())
        {
            foreach (var addr in addresses)
            {
                var health = $"{addr.TrimEnd('/')}/api/v1/health";
                startupLogger.LogInformation("AuthService API is running at {Url}. Health endpoint: {HealthUrl}", addr, health);
            }
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Failed to determine listening addresses");
    }
});


// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking database connection and running migrations...");
        await context.Database.MigrateAsync();

        logger.LogInformation("Database ready. Running seed data...");
        await DataSeeder.SeedAsync(context);

        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}


app.Run();