using System.ComponentModel.DataAnnotations;

namespace TeamTaskBoard.Api.Dtos;

public sealed record SaveTask(
    [param: Required, StringLength(200)] string Title,
    [param: StringLength(4000)] string? Description,
    [param: Required, RegularExpression("To Do|In Progress|Done")] string Status = "To Do",
    [param: Required, RegularExpression("Low|Medium|High")] string Priority = "Medium",
    DateOnly? DueDate = null,
    [param: Range(1, int.MaxValue)] int? AssigneeId = null)
{
    public bool HasValidDueDate(DateOnly? existingDueDate, DateOnly today) =>
        DueDate == existingDueDate || DueDate is null || DueDate >= today;
}

public sealed class TaskFilters
{
    [RegularExpression("To Do|In Progress|Done")] public string? Status { get; set; }
    [RegularExpression("Low|Medium|High")] public string? Priority { get; set; }
    [StringLength(200)] public string? Search { get; set; }
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 30;
}

public sealed record TaskDto(int Id, int ProjectId, string Title, string? Description, string Status,
    string Priority, DateOnly? DueDate, int? AssigneeId, DateTime? CreatedAt, DateTime? UpdatedAt);

public sealed record TaskPage(IReadOnlyList<TaskDto> Items, int Page, int PageSize, int TotalCount);
public sealed record OverdueCount(int UserId, string Name, long Count);
