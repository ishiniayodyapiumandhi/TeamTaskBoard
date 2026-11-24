using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TaskBoardDbContext>((services, options) =>
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("TaskBoard");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Set ConnectionStrings__TaskBoard to use MySQL.");
    options.UseMySQL(connection);
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173";
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddSingleton(services => FirestoreDb.Create(
    services.GetRequiredService<IConfiguration>()["Firebase:ProjectId"]
    ?? throw new InvalidOperationException("Set Firebase__ProjectId before enabling comments.")));
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var projectId = builder.Configuration["Firebase:ProjectId"];
    if (string.IsNullOrWhiteSpace(projectId))
        throw new InvalidOperationException("Set Firebase__ProjectId to your Firebase project ID.");

    options.Authority = $"https://securetoken.google.com/{projectId}";
    options.Audience = projectId;
    options.MapInboundClaims = false;
    options.IncludeErrorDetails = false;
    options.TokenValidationParameters.ValidIssuer = options.Authority;
    options.TokenValidationParameters.ValidAlgorithms = ["RS256"];
    options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var subject = context.Principal?.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject) || subject.Length > 128)
                context.Fail("Invalid Firebase subject.");
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var claim in new[] { "iat", "auth_time" })
            {
                if (!long.TryParse(context.Principal?.FindFirst(claim)?.Value, out var timestamp)
                    || timestamp <= 0 || timestamp > now)
                    context.Fail("Invalid Firebase token timestamp.");
            }
            return Task.CompletedTask;
        }
    };
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
