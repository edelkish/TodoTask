using System.ComponentModel.DataAnnotations;
using TodoApi.Models;

namespace TodoApi.Dtos;

/// <summary>Reemplazo completo de la tarea (PUT). Permite fechas límite pasadas.</summary>
public class UpdateTaskRequest
{
    [Required(AllowEmptyStrings = false), MaxLength(200)]
    public string? Title { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    public TaskItemStatus Status { get; init; }

    [Required(AllowEmptyStrings = false), MaxLength(100)]
    public string? Assignee { get; init; }

    public DateTime? DueDate { get; init; }
}
