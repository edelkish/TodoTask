using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TodoApi.Dtos;

namespace TodoApi.Tests;

internal static class TestHelpers
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FutureDate() => DateTime.UtcNow.AddDays(30).ToString("O");

    /// <summary>Crea una tarea válida y devuelve la respuesta ya deserializada.</summary>
    public static async Task<TaskResponse> CreateAsync(
        this HttpClient client, string title = "Tarea de prueba", string assignee = "ana",
        string? status = null, string? dueDate = null)
    {
        var response = await client.PostAsJsonAsync("/api/tasks",
            new { title, assignee, status, dueDate }, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
    }
}
