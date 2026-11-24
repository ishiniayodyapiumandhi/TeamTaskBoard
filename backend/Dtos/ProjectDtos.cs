using System.ComponentModel.DataAnnotations;

namespace TeamTaskBoard.Api.Dtos;

public sealed record CreateProjectRequest(
    [param: Required, StringLength(150)] string Name,
    [param: StringLength(4000)] string? Description);

public sealed record ProjectDto(int Id, string Name, string? Description, DateTime? CreatedAt);
