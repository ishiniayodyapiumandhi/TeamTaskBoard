using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TeamTaskBoard.Api.Data;

public sealed class TaskBoardDbContext(DbContextOptions<TaskBoardDbContext> options) : DbContext(options)
{
    public DbSet<BoardUser> Users => Set<BoardUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<BoardTask> Tasks => Set<BoardTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<BoardUser>();
        users.HasIndex(user => user.FirebaseUid).IsUnique();
        users.HasIndex(user => user.Email).IsUnique();
        users.Property(user => user.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        var projects = modelBuilder.Entity<Project>();
        projects.Property(project => project.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        projects.HasOne<BoardUser>().WithMany().HasForeignKey(project => project.OwnerId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_project_owner");

        var tasks = modelBuilder.Entity<BoardTask>();
        tasks.Property(task => task.DueDate).HasConversion(
            date => date!.Value.ToDateTime(TimeOnly.MinValue),
            date => DateOnly.FromDateTime(date));
        tasks.Property(task => task.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        tasks.Property(task => task.UpdatedAt).ValueGeneratedOnAddOrUpdate();
        tasks.HasOne<Project>().WithMany().HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_task_project");
        tasks.HasOne<BoardUser>().WithMany().HasForeignKey(task => task.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull).HasConstraintName("fk_task_assignee");
        tasks.HasIndex(task => new { task.ProjectId, task.Priority })
            .HasDatabaseName("idx_task_project_priority");
    }
}

[Table("users")]
public sealed class BoardUser
{
    [Column("id")] public int Id { get; set; }
    [Column("firebase_uid"), MaxLength(128)] public required string FirebaseUid { get; set; }
    [Column("name"), MaxLength(100)] public required string Name { get; set; }
    [Column("email"), MaxLength(255)] public required string Email { get; set; }
    [Column("created_at", TypeName = "timestamp")] public DateTime? CreatedAt { get; set; }
}

[Table("projects")]
public sealed class Project
{
    [Column("id")] public int Id { get; set; }
    [Column("owner_id")] public int OwnerId { get; set; }
    [Column("name"), MaxLength(150)] public required string Name { get; set; }
    [Column("description", TypeName = "text")] public string? Description { get; set; }
    [Column("created_at", TypeName = "timestamp")] public DateTime? CreatedAt { get; set; }
}

[Table("tasks")]
public sealed class BoardTask
{
    [Column("id")] public int Id { get; set; }
    [Column("project_id")] public int ProjectId { get; set; }
    [Column("assignee_id")] public int? AssigneeId { get; set; }
    [Column("title"), MaxLength(200)] public required string Title { get; set; }
    [Column("description", TypeName = "text")] public string? Description { get; set; }
    [Column("status")] public string Status { get; set; } = "To Do";
    [Column("priority")] public string Priority { get; set; } = "Medium";
    [Column("due_date", TypeName = "date")] public DateOnly? DueDate { get; set; }
    [Column("created_at", TypeName = "timestamp")] public DateTime? CreatedAt { get; set; }
    [Column("updated_at", TypeName = "timestamp")] public DateTime? UpdatedAt { get; set; }
}
