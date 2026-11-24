using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Dtos;
using TeamTaskBoard.Api.Services;

internal static class MySqlTaskChecks
{
    public static async Task Run()
    {
        var connection = Environment.GetEnvironmentVariable("TaskBoardTest__ConnectionString");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set TaskBoardTest__ConnectionString to a separate MySQL test database with the assignment schema.");
        if (!string.Equals(new MySqlConnectionStringBuilder(connection).Database,
                "team_task_board_test", StringComparison.Ordinal))
            throw new InvalidOperationException("The integration test only accepts Database=team_task_board_test.");

        await using var database = new TaskBoardDbContext(new DbContextOptionsBuilder<TaskBoardDbContext>()
            .UseMySQL(connection).Options);
        await using var transaction = await database.Database.BeginTransactionAsync();
        try
        {
            var uid = Guid.NewGuid().ToString("N");
            var project = await new ProjectService(database).Create(uid, uid + "@example.invalid",
                new CreateProjectRequest("Date mapping regression", null), default)
                ?? throw new InvalidOperationException("Could not create the test project.");
            var tasks = new TaskService(database);
            var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
            foreach (DateOnly? date in new DateOnly?[] { dueDate, null })
            {
                var (created, error) = await tasks.Save(uid, project.Id, true,
                    new SaveTask("Date round trip", null, DueDate: date), default);
                if (created is null || error is not null)
                    throw new InvalidOperationException("Could not create the test task.");

                database.ChangeTracker.Clear();
                var page = await tasks.List(uid, project.Id, new TaskFilters(), default);
                var result = page?.Items.Single(task => task.Id == created.Id);
                if (result is null || result.DueDate != date)
                    throw new InvalidOperationException("Task listing did not round-trip the expected due date.");
            }
            var ownerId = await database.Users.Where(user => user.FirebaseUid == uid)
                .Select(user => user.Id).SingleAsync();
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            database.Tasks.AddRange(
                new BoardTask { ProjectId = project.Id, Title = "Overdue", AssigneeId = ownerId, DueDate = yesterday },
                new BoardTask { ProjectId = project.Id, Title = "Completed", AssigneeId = ownerId, DueDate = yesterday, Status = "Done" },
                new BoardTask { ProjectId = project.Id, Title = "Future", AssigneeId = ownerId, DueDate = dueDate },
                new BoardTask { ProjectId = project.Id, Title = "No due date", AssigneeId = ownerId },
                new BoardTask { ProjectId = project.Id, Title = "Unassigned", DueDate = yesterday });
            await database.SaveChangesAsync();
            var report = await tasks.Overdue(uid, default);
            if (report.Count != 1 || report[0].UserId != ownerId || report[0].Count != 1)
                throw new InvalidOperationException("The overdue report did not return the expected assignee and count.");
        }
        finally
        {
            await transaction.RollbackAsync();
        }
        Console.WriteLine("PASS: MySQL task creation and listing round-trip a date and a null due date.");
        Console.WriteLine("PASS: MySQL overdue report counts the assigned overdue task and excludes completed, future, unassigned and undated tasks.");
    }
}
