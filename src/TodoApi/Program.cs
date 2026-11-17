using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Endpoints;
using Scalar.AspNetCore;
using TodoApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Los estados viajan como texto ("Pending", "InProgress", "Done"); un valor desconocido produce un 400.
builder.Services.Configure<JsonOptions>(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// Validación automática (DataAnnotations / IValidatableObject) en Minimal APIs -> 400 con ProblemDetails.
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// Aplica las migraciones pendientes al arrancar en desarrollo (crea todo.db si no existe).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("TodoTask API"));
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapTaskEndpoints();

app.Run();

// Necesario para que WebApplicationFactory<Program> pueda referenciar el punto de entrada en los tests.
public partial class Program;
