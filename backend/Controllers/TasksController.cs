using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.Dtos;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Authorize]
public sealed class TasksController(TaskService tasks) : ControllerBase
{
    private string Uid => User.FindFirst("sub")!.Value;

    [HttpGet("api/projects/{id:int}/tasks")]
    public async Task<ActionResult<TaskPage>> List(int id, [FromQuery] TaskFilters filters, CancellationToken cancellation)
    {
        var page = await tasks.List(Uid, id, filters, cancellation);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpGet("api/tasks/{id:int}")]
    public async Task<ActionResult<TaskDto>> Get(int id, CancellationToken cancellation)
    {
        var task = await tasks.Get(Uid, id, cancellation);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost("api/projects/{id:int}/tasks")]
    public Task<ActionResult<TaskDto>> Create(int id, SaveTask request, CancellationToken cancellation) =>
        Save(id, true, request, cancellation);

    [HttpPut("api/tasks/{id:int}")]
    public Task<ActionResult<TaskDto>> Update(int id, SaveTask request, CancellationToken cancellation) =>
        Save(id, false, request, cancellation);

    [HttpDelete("api/tasks/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellation) =>
        await tasks.Delete(Uid, id, cancellation) ? NoContent() : NotFound();

    [HttpGet("api/reports/overdue-by-user")]
    public async Task<ActionResult<IReadOnlyList<OverdueCount>>> Overdue(CancellationToken cancellation) =>
        Ok(await tasks.Overdue(Uid, cancellation));

    private async Task<ActionResult<TaskDto>> Save(int id, bool create, SaveTask request, CancellationToken cancellation)
    {
        var (task, error) = await tasks.Save(Uid, id, create, request, cancellation);
        if (error is not null)
            return Problem(statusCode: 400, title: error);
        if (task is null)
            return NotFound();
        return create ? CreatedAtAction(nameof(Get), new { id = task.Id }, task) : Ok(task);
    }
}
