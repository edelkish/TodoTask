using TodoApi.Models;

namespace TodoApi.Dtos;

public static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem t) =>
        new(t.Id, t.Title, t.Description, t.Status, t.Assignee, t.DueDate, t.CreatedAt, t.UpdatedAt);

    public static TaskItem ToEntity(this CreateTaskRequest r, DateTime now) => new()
    {
        Title = r.Title!.Trim(),
        Description = r.Description?.Trim(),
        Status = r.Status,
        Assignee = r.Assignee!.Trim(),
        DueDate = r.DueDate?.ToUniversalTime(),
        CreatedAt = now,
        UpdatedAt = now
    };

    public static void Apply(this UpdateTaskRequest r, TaskItem t, DateTime now)
    {
        t.Title = r.Title!.Trim();
        t.Description = r.Description?.Trim();
        t.Status = r.Status;
        t.Assignee = r.Assignee!.Trim();
        t.DueDate = r.DueDate?.ToUniversalTime();
        t.UpdatedAt = now;
    }
}
