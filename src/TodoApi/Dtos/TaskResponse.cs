using TodoApi.Models;

namespace TodoApi.Dtos;

public record TaskResponse(
    int Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    string Assignee,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);
