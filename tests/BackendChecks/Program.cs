using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Controllers;
using TeamTaskBoard.Api.Dtos;

static void Check(bool passed, string description)
{
    if (!passed)
        throw new InvalidOperationException(description);
    Console.WriteLine($"PASS: {description}");
}

using var services = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
var validator = services.GetRequiredService<IObjectModelValidator>();

ModelStateDictionary Validate(object model)
{
    var context = new ActionContext(new DefaultHttpContext { RequestServices = services },
        new RouteData(), new ActionDescriptor());
    validator.Validate(context, null, string.Empty, model);
    return context.ModelState;
}

bool Valid(object model) => Validate(model).IsValid;

Check(Valid(new CreateProjectRequest("Demo", null)), "Project description is optional");
Check(Valid(new CreateProjectRequest("Demo", new string('x', 4000))), "Project accepts a 4000-character description");
var invalidProject = Validate(new CreateProjectRequest("Demo", new string('x', 4001)));
Check(!invalidProject.IsValid && invalidProject["Description"]?.Errors.Count == 1,
    "MVC rejects an oversized project description on the Description field");
Check(!Valid(new CreateProjectRequest(" ", null)), "MVC rejects a blank project name");
using var mappingContext = new TaskBoardDbContext(new DbContextOptionsBuilder<TaskBoardDbContext>()
    .UseMySQL("Server=localhost;Database=mapping_only").Options);
var dueDateMapping = mappingContext.Model.FindEntityType(typeof(BoardTask))!
    .FindProperty(nameof(BoardTask.DueDate))!.GetRelationalTypeMapping();
Check(dueDateMapping.StoreType == "date" && dueDateMapping.Converter?.ProviderClrType == typeof(DateTime),
    "MySQL DATE uses DateTime as its provider type");

var request = new SaveTask("Prepare demo", null);
Check(Valid(request), "Task defaults are valid");
Check(!Valid(request with { Title = " " }), "Blank title is rejected");
Check(!Valid(request with { Status = "Blocked" }), "Unknown status is rejected");
Check(!Valid(request with { Priority = "Urgent" }), "Unknown priority is rejected");
Check(!Valid(request with { AssigneeId = 0 }), "Invalid assignee ID is rejected");
Check(!Valid(request with { Description = new string('x', 4001) }), "Oversized task description is rejected");
Check(Valid(new TaskFilters()), "Default paging is valid");
Check(!Valid(new TaskFilters { Page = 0 }), "Page zero is rejected");
Check(!Valid(new TaskFilters { PageSize = 101 }), "Oversized page is rejected");

var today = new DateOnly(2026, 11, 19);
var yesterday = today.AddDays(-1);
Check(!(request with { DueDate = yesterday }).HasValidDueDate(null, today), "New overdue date is rejected");
Check((request with { DueDate = yesterday }).HasValidDueDate(yesterday, today), "Unchanged overdue date is allowed");
Check((request with { DueDate = today }).HasValidDueDate(null, today), "Today's due date is allowed");

foreach (var controller in new[] { typeof(ProjectsController), typeof(TasksController), typeof(CommentAccessController) })
{
    Check(controller.IsDefined(typeof(AuthorizeAttribute), inherit: true), $"{controller.Name} requires authorization");
    Check(!controller.IsDefined(typeof(AllowAnonymousAttribute), inherit: true) &&
        controller.GetMethods().Where(method => method.IsDefined(typeof(HttpMethodAttribute), inherit: true))
            .All(method => !method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)),
        $"{controller.Name} has no anonymous route overrides");
}

if (args.Contains("--mysql"))
    await MySqlTaskChecks.Run();
