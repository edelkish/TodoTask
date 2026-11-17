using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks");

        group.MapGet("/", ListTasks).WithName("ListTasks");
        group.MapPost("/", CreateTask).WithName("CreateTask");
        group.MapGet("/{id:int}", GetTaskById).WithName("GetTaskById");
        group.MapPut("/{id:int}", UpdateTask).WithName("UpdateTask");
        group.MapDelete("/{id:int}", DeleteTask).WithName("DeleteTask");

        return app;
    }

    private static async Task<CreatedAtRoute<TaskResponse>> CreateTask(
        CreateTaskRequest request, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var task = request.ToEntity(clock.GetUtcNow().UtcDateTime);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);

        return TypedResults.CreatedAtRoute(task.ToResponse(), "GetTaskById", new { id = task.Id });
    }

    private static async Task<Results<Ok<TaskResponse>, NotFound>> GetTaskById(
        int id, AppDbContext db, CancellationToken ct)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return task is null ? TypedResults.NotFound() : TypedResults.Ok(task.ToResponse());
    }

    private const int MaxPageSize = 100;

    /// <summary>
    /// Lista tareas con filtros opcionales por estado y responsable (sin distinguir mayúsculas) y paginación.
    /// Orden estable: fecha límite ascendente (las tareas sin fecha al final) y después Id.
    /// </summary>
    private static async Task<Results<Ok<PagedResult<TaskResponse>>, ValidationProblem>> ListTasks(
        AppDbContext db, CancellationToken ct,
        string? status = null, string? assignee = null, int page = 1, int pageSize = 20)
    {
        var errors = new Dictionary<string, string[]>();
        // El estado se interpreta a mano: el binding automático de enums ignora en silencio los valores
        // desconocidos cuando no está en Development, devolviendo todas las tareas en vez de un 400.
        TaskItemStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (int.TryParse(status, out _) || !Enum.TryParse<TaskItemStatus>(status.Trim(), ignoreCase: true, out var parsed))
                errors["status"] = [$"status debe ser uno de: {string.Join(", ", Enum.GetNames<TaskItemStatus>())}."];
            else
                statusFilter = parsed;
        }
        if (page < 1) errors["page"] = ["page debe ser mayor o igual que 1."];
        if (pageSize is < 1 or > MaxPageSize) errors["pageSize"] = [$"pageSize debe estar entre 1 y {MaxPageSize}."];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var query = db.Tasks.AsNoTracking();
        if (statusFilter is { } s) query = query.Where(t => t.Status == s);
        if (!string.IsNullOrWhiteSpace(assignee))
        {
            var who = assignee.Trim().ToLower();
            query = query.Where(t => t.Assignee.ToLower() == who);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<TaskResponse>(
            items.Select(t => t.ToResponse()).ToList(), page, pageSize, total));
    }

    /// <summary>Reemplaza todos los campos editables de la tarea. 404 si no existe.</summary>
    private static async Task<Results<Ok<TaskResponse>, NotFound>> UpdateTask(
        int id, UpdateTaskRequest request, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null) return TypedResults.NotFound();

        request.Apply(task, clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(task.ToResponse());
    }

    /// <summary>Elimina la tarea. 204 si se borró, 404 si no existe.</summary>
    private static async Task<Results<NoContent, NotFound>> DeleteTask(
        int id, AppDbContext db, CancellationToken ct)
    {
        var deleted = await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }
}
