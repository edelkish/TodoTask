using System.ComponentModel.DataAnnotations;
using TodoApi.Models;

namespace TodoApi.Dtos;

public class CreateTaskRequest : IValidatableObject
{
    [Required(AllowEmptyStrings = false), MaxLength(200)]
    public string? Title { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    /// <summary>Si no se indica, la tarea se crea como Pending.</summary>
    public TaskItemStatus Status { get; init; } = TaskItemStatus.Pending;

    [Required(AllowEmptyStrings = false), MaxLength(100)]
    public string? Assignee { get; init; }

    public DateTime? DueDate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueDate is { } due && due.ToUniversalTime() < DateTime.UtcNow)
        {
            yield return new ValidationResult(
                "La fecha límite no puede estar en el pasado.", [nameof(DueDate)]);
        }
    }
}
