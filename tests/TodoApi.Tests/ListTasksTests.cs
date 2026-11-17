using System.Net;
using System.Net.Http.Json;
using TodoApi.Dtos;
using TodoApi.Models;
using static TodoApi.Tests.TestHelpers;

namespace TodoApi.Tests;

public class ListTasksTests(TodoApiFactory factory) : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // Cada test usa un responsable único para no depender de los datos de otros tests.
    private static string UniqueAssignee() => $"user-{Guid.NewGuid():N}";

    private Task<PagedResult<TaskResponse>> ListAsync(string query) =>
        _client.GetFromJsonAsync<PagedResult<TaskResponse>>($"/api/tasks{query}", Json)!;

    [Fact]
    public async Task Filtra_por_responsable_sin_distinguir_mayusculas()
    {
        var ana = UniqueAssignee();
        var luis = UniqueAssignee();
        await _client.CreateAsync("A1", ana);
        await _client.CreateAsync("A2", ana);
        await _client.CreateAsync("L1", luis);

        var result = await ListAsync($"?assignee={ana.ToUpperInvariant()}");

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.Equal(ana, t.Assignee));
    }

    [Fact]
    public async Task Filtra_por_estado_y_combina_con_responsable()
    {
        var who = UniqueAssignee();
        await _client.CreateAsync("pendiente", who);
        await _client.CreateAsync("en curso", who, status: "InProgress");
        await _client.CreateAsync("hecha", who, status: "Done");

        var done = await ListAsync($"?assignee={who}&status=Done");
        var all = await ListAsync($"?assignee={who}");

        Assert.Equal(3, all.TotalCount);
        var single = Assert.Single(done.Items);
        Assert.Equal(TaskItemStatus.Done, single.Status);
    }

    [Fact]
    public async Task Pagina_los_resultados_con_totales_correctos()
    {
        var who = UniqueAssignee();
        for (var i = 1; i <= 5; i++) await _client.CreateAsync($"T{i}", who);

        var page1 = await ListAsync($"?assignee={who}&page=1&pageSize=2");
        var page3 = await ListAsync($"?assignee={who}&page=3&pageSize=2");
        var page4 = await ListAsync($"?assignee={who}&page=4&pageSize=2");

        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Single(page3.Items);
        Assert.Empty(page4.Items);
        Assert.Empty(page1.Items.Select(t => t.Id).Intersect(page3.Items.Select(t => t.Id)));
    }

    [Fact]
    public async Task Ordena_por_fecha_limite_dejando_las_tareas_sin_fecha_al_final()
    {
        var who = UniqueAssignee();
        var sinFecha = await _client.CreateAsync("sin fecha", who);
        var tarde = await _client.CreateAsync("tarde", who, dueDate: DateTime.UtcNow.AddDays(20).ToString("O"));
        var pronto = await _client.CreateAsync("pronto", who, dueDate: DateTime.UtcNow.AddDays(5).ToString("O"));

        var result = await ListAsync($"?assignee={who}");

        Assert.Equal([pronto.Id, tarde.Id, sinFecha.Id], result.Items.Select(t => t.Id));
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?status=Cancelada")]
    public async Task Parametros_invalidos_devuelven_400(string query)
    {
        var response = await _client.GetAsync($"/api/tasks{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
