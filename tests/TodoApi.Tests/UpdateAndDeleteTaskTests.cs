using System.Net;
using System.Net.Http.Json;
using TodoApi.Dtos;
using TodoApi.Models;
using static TodoApi.Tests.TestHelpers;

namespace TodoApi.Tests;

public class UpdateAndDeleteTaskTests(TodoApiFactory factory) : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Editar_tarea_actualiza_los_campos_y_devuelve_200()
    {
        var original = await _client.CreateAsync("Original", "ana");

        var response = await _client.PutAsJsonAsync($"/api/tasks/{original.Id}",
            new { title = "Editada", description = "nueva", status = "Done", assignee = "luis", dueDate = FutureDate() }, Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
        Assert.Equal("Editada", updated.Title);
        Assert.Equal(TaskItemStatus.Done, updated.Status);
        Assert.Equal("luis", updated.Assignee);
        Assert.Equal(original.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt >= original.UpdatedAt);

        // El cambio queda persistido.
        var fetched = await _client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{original.Id}", Json);
        Assert.Equal("Editada", fetched!.Title);
    }

    [Fact]
    public async Task Editar_con_datos_invalidos_devuelve_400_y_no_modifica_la_tarea()
    {
        var original = await _client.CreateAsync("Intacta", "ana");

        var response = await _client.PutAsJsonAsync($"/api/tasks/{original.Id}",
            new { title = "", assignee = "ana", status = "Done" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{original.Id}", Json);
        Assert.Equal("Intacta", fetched!.Title);
    }

    [Fact]
    public async Task Editar_tarea_inexistente_devuelve_404()
    {
        var response = await _client.PutAsJsonAsync("/api/tasks/987654",
            new { title = "x", assignee = "ana", status = "Pending" }, Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Borrar_tarea_devuelve_204_y_despues_404()
    {
        var task = await _client.CreateAsync("Para borrar", "ana");

        var first = await _client.DeleteAsync($"/api/tasks/{task.Id}");
        var second = await _client.DeleteAsync($"/api/tasks/{task.Id}");
        var get = await _client.GetAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
