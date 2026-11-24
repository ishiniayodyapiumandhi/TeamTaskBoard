using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Dtos;

namespace TeamTaskBoard.Api.Services;

public sealed class ProjectService(TaskBoardDbContext database)
{
    public async Task<IReadOnlyList<ProjectDto>> List(string uid, CancellationToken cancellation)
    {
        var owner = await FindOwner(uid, cancellation);
        if (owner is null)
            return [];

        return await database.Projects.AsNoTracking().Where(project => project.OwnerId == owner.Id)
            .OrderByDescending(project => project.Id)
            .Select(project => new ProjectDto(project.Id, project.Name, project.Description, project.CreatedAt))
            .ToListAsync(cancellation);
    }

    public async Task<ProjectDto?> Get(string uid, int id, CancellationToken cancellation)
    {
        var owner = await FindOwner(uid, cancellation);
        if (owner is null)
            return null;

        return await database.Projects.AsNoTracking()
            .Where(project => project.OwnerId == owner.Id && project.Id == id)
            .Select(project => new ProjectDto(project.Id, project.Name, project.Description, project.CreatedAt))
            .SingleOrDefaultAsync(cancellation);
    }

    public async Task<ProjectDto?> Create(string uid, string email, CreateProjectRequest request,
        CancellationToken cancellation)
    {
        var owner = await FindOwner(uid, cancellation);
        if (owner is null)
        {
            var name = email[..Math.Min(email.IndexOf('@'), 100)];
            owner = new BoardUser { FirebaseUid = uid, Email = email, Name = name };
            database.Users.Add(owner);
            try
            {
                await database.SaveChangesAsync(cancellation);
            }
            catch (DbUpdateException error) when (error.InnerException is MySqlException { Number: 1062 })
            {
                database.Entry(owner).State = EntityState.Detached;
                owner = await FindOwner(uid, cancellation);
                if (owner is null)
                    return null;
            }
        }

        var project = new Project { OwnerId = owner.Id, Name = request.Name.Trim(), Description = request.Description?.Trim() };
        database.Projects.Add(project);
        await database.SaveChangesAsync(cancellation);
        return new ProjectDto(project.Id, project.Name, project.Description, project.CreatedAt);
    }

    private async Task<BoardUser?> FindOwner(string uid, CancellationToken cancellation)
    {
        var owner = await database.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.FirebaseUid == uid, cancellation);
        return owner?.FirebaseUid == uid ? owner : null;
    }
}
