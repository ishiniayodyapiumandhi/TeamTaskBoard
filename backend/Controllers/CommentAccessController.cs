using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.Services;
using System.Globalization;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks/{id:int}/comment-access")]
public sealed class CommentAccessController(TaskService tasks) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Grant(int id, [FromServices] IServiceProvider services, CancellationToken cancellation)
    {
        var uid = User.FindFirst("sub")!.Value;
        if (await tasks.Get(uid, id, cancellation) is null)
            return NotFound();

        var firestore = services.GetRequiredService<FirestoreDb>();
        var document = firestore.Collection("taskAccess").Document(id.ToString(CultureInfo.InvariantCulture));
        await document.SetAsync(new Dictionary<string, object>
        {
            ["ownerUid"] = uid,
            ["expiresAt"] = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(2))
        }, cancellationToken: cancellation);
        return NoContent();
    }
}
