var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();

// Necesario para que WebApplicationFactory<Program> pueda referenciar el punto de entrada en los tests.
public partial class Program;
