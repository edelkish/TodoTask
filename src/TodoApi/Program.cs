using Microsoft.EntityFrameworkCore;
using TodoApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.Run();

// Necesario para que WebApplicationFactory<Program> pueda referenciar el punto de entrada en los tests.
public partial class Program;
