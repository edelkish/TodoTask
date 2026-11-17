using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;

namespace TodoApi.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks");

        group.MapPost("/", CreateTask).WithName("CreateTask");
        group.MapGet("/{id:int}", GetTaskById).WithName("GetTaskById");

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
}
