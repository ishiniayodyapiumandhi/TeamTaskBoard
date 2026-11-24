using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.Dtos;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(ProjectService projects) : ControllerBase
{
    private string Uid => User.FindFirst("sub")!.Value;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> List(CancellationToken cancellation) =>
        Ok(await projects.List(Uid, cancellation));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> Get(int id, CancellationToken cancellation)
    {
        var project = await projects.Get(Uid, id, cancellation);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken cancellation)
    {
        var email = User.FindFirst("email")?.Value;
        if (email is null || email.Length > 255 || email.IndexOf('@') <= 0)
            return Problem(statusCode: 400, title: "A Firebase email address is required.");

        var project = await projects.Create(Uid, email, request, cancellation);
        return project is null
            ? Problem(statusCode: 409, title: "This Firebase account conflicts with an existing database user.")
            : CreatedAtAction(nameof(Get), new { id = project.Id }, project);
    }
}
