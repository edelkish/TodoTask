using System.Net;
using System.Net.Http.Json;
using TodoApi.Dtos;
using TodoApi.Models;
using static TodoApi.Tests.TestHelpers;

namespace TodoApi.Tests;

public class CreateTaskTests(TodoApiFactory factory) : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Crear_tarea_valida_devuelve_201_con_Location_y_estado_Pending_por_defecto()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks",
            new { title = "Escribir README", description = "Con decisiones", assignee = "ana", dueDate = FutureDate() }, Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskResponse>(Json);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal(TaskItemStatus.Pending, created.Status);
        Assert.Equal("Escribir README", created.Title);
        Assert.EndsWith($"/api/tasks/{created.Id}", response.Headers.Location!.ToString());

        // La tarea creada se puede recuperar desde la URL de Location.
        var fetched = await _client.GetFromJsonAsync<TaskResponse>(response.Headers.Location, Json);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Theory]
    [InlineData("", "ana")]                         // título vacío
    [InlineData("   ", "ana")]                      // título solo espacios
    [InlineData("Titulo", "")]                      // responsable vacío
    public async Task Crear_con_campos_obligatorios_vacios_devuelve_400(string title, string assignee)
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { title, assignee }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Crear_con_titulo_demasiado_largo_devuelve_400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks",
            new { title = new string('x', 201), assignee = "ana" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Crear_con_fecha_limite_pasada_devuelve_400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks",
            new { title = "Vencida", assignee = "ana", dueDate = "2000-01-01T00:00:00Z" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Crear_con_estado_desconocido_devuelve_400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks",
            new { title = "x", assignee = "ana", status = "Cancelada" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Obtener_tarea_inexistente_devuelve_404()
    {
        var response = await _client.GetAsync("/api/tasks/987654");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
