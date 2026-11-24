using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Dtos;

namespace TeamTaskBoard.Api.Services;

public sealed class TaskService(TaskBoardDbContext database)
{
    private async Task<int?> OwnerId(string uid, CancellationToken cancellation)
    {
        var user = await database.Users.AsNoTracking().SingleOrDefaultAsync(row => row.FirebaseUid == uid, cancellation);
        return user?.FirebaseUid == uid ? user.Id : null;
    }

    private IQueryable<BoardTask> OwnedTasks(int? ownerId) =>
        from task in database.Tasks
        join project in database.Projects on task.ProjectId equals project.Id
        where project.OwnerId == ownerId
        select task;

    private static TaskDto Map(BoardTask task) => new(task.Id, task.ProjectId, task.Title, task.Description,
        task.Status, task.Priority, task.DueDate, task.AssigneeId, task.CreatedAt, task.UpdatedAt);

    public async Task<TaskPage?> List(string uid, int projectId, TaskFilters filters, CancellationToken cancellation)
    {
        var owner = await OwnerId(uid, cancellation);
        if (!await database.Projects.AnyAsync(project => project.Id == projectId && project.OwnerId == owner, cancellation))
            return null;

        var query = OwnedTasks(owner).AsNoTracking().Where(task => task.ProjectId == projectId);
        if (filters.Status is not null)
            query = query.Where(task => task.Status == filters.Status);
        if (filters.Priority is not null)
            query = query.Where(task => task.Priority == filters.Priority);
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(task => task.Title.Contains(search));
        }

        var total = await query.CountAsync(cancellation);
        var items = await query.OrderBy(task => task.Id).Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize).ToListAsync(cancellation);
        return new TaskPage(items.Select(Map).ToArray(), filters.Page, filters.PageSize, total);
    }

    public async Task<TaskDto?> Get(string uid, int id, CancellationToken cancellation)
    {
        var owner = await OwnerId(uid, cancellation);
        var task = await OwnedTasks(owner).AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, cancellation);
        return task is null ? null : Map(task);
    }

    public async Task<(TaskDto? Task, string? Error)> Save(string uid, int id, bool create, SaveTask request,
        CancellationToken cancellation)
    {
        var owner = await OwnerId(uid, cancellation);
        BoardTask? task;
        if (create)
        {
            if (!await database.Projects.AnyAsync(project => project.Id == id && project.OwnerId == owner, cancellation))
                return (null, null);
            task = new BoardTask { ProjectId = id, Title = request.Title };
        }
        else
        {
            task = await OwnedTasks(owner).SingleOrDefaultAsync(row => row.Id == id, cancellation);
            if (task is null)
                return (null, null);
        }

        if (!request.HasValidDueDate(task.DueDate, DateOnly.FromDateTime(DateTime.UtcNow)))
            return (null, "The due date cannot be in the past (UTC).");
        if (request.AssigneeId is int assignee &&
            !await database.Users.AnyAsync(user => user.Id == assignee, cancellation))
            return (null, "The assignee does not exist.");

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.AssigneeId = request.AssigneeId;
        if (create)
            database.Tasks.Add(task);
        await database.SaveChangesAsync(cancellation);
        return (Map(task), null);
    }

    public async Task<bool> Delete(string uid, int id, CancellationToken cancellation)
    {
        var owner = await OwnerId(uid, cancellation);
        var task = await OwnedTasks(owner).SingleOrDefaultAsync(row => row.Id == id, cancellation);
        if (task is null)
            return false;
        database.Tasks.Remove(task);
        await database.SaveChangesAsync(cancellation);
        return true;
    }

    public async Task<IReadOnlyList<OverdueCount>> Overdue(string uid, CancellationToken cancellation)
    {
        var owner = await OwnerId(uid, cancellation);
        if (owner is null)
            return [];

        return await database.Database.SqlQuery<OverdueCount>($"""
            SELECT assignee.id AS UserId, assignee.name AS Name, COUNT(*) AS `Count`
            FROM tasks AS task
            JOIN projects AS project ON project.id = task.project_id
            JOIN users AS assignee ON assignee.id = task.assignee_id
            WHERE project.owner_id = {owner.Value} AND task.status <> 'Done'
                AND task.due_date < UTC_DATE()
            GROUP BY assignee.id, assignee.name
            ORDER BY COUNT(*) DESC, assignee.id
            """).ToListAsync(cancellation);
    }
}
