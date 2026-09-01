using DayNex.IdentityService.Application;
using DayNex.IdentityService.Infrastructure;
using DayNex.Shared.Http.Extensions;
using DayNex.Shared.Http.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Layer composition (leak-proof: API only wires interfaces, never implements logic) ----
builder.Services.AddIdentityServiceApplication();
builder.Services.AddIdentityServiceInfrastructure(builder.Configuration);

// ---- Shared auth (identical pattern every DayNex microservice will use) ----
builder.Services.AddDayNexAuthentication(builder.Configuration);
builder.Services.AddDayNexAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ---- Swagger with JWT bearer support, so Postman/Swagger UI can pass a token ----
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "DayNex Identity Service", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT access token issued by Entra External ID."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DayNexDefault", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Applies pending EF Core migrations and seeds the single SuperAdmin account from config.
await DayNex.IdentityService.Infrastructure.Persistence.IdentityDbInitializer.InitializeAsync(app.Services);

app.UseDayNexExceptionHandling(); // shared middleware — first in the pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DayNex Identity Service v1"));
}

app.UseHttpsRedirection();
app.UseCors("DayNexDefault");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
