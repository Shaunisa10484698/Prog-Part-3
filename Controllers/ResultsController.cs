using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using System.Security.Claims;

namespace RaceDayAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ResultsController(ApplicationDbContext db) => _db = db;

    // Organiser adds result

    [Authorize(Roles = "Organiser")]
    [HttpPost]
    public async Task<ActionResult<Result>> AddResult(Result result)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        // Check whether the event exists
        var existingEvent = await _db.Events
            .FindAsync(result.EventId);

        if (existingEvent == null)
            return NotFound("Event not found");

        // Check event ownership
        if (existingEvent.OrganiserId != userId)
            return Forbid();

        // Check whether participant exists
        var participant = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == result.ParticipantId &&
                u.Role == "Participant");

        if (participant == null)
            return NotFound("Participant not found");

        // Check participant enrolment
        var enrolment = await _db.Enrolments
            .FirstOrDefaultAsync(e =>
                e.EventId == result.EventId &&
                e.ParticipantId == result.ParticipantId);

        if (enrolment == null)
            return BadRequest(
                "Participant is not enrolled in this event"
            );

        // Validate result details
        if (result.Position <= 0)
            return BadRequest("Invalid finishing position");

        if (!TimeSpan.TryParse(result.FinishTime, out _))
            return BadRequest("Invalid finish time");

        // Prevent duplicate results
        var duplicate = await _db.Results.AnyAsync(r =>
            r.EventId == result.EventId &&
            r.ParticipantId == result.ParticipantId);

        if (duplicate)
            return BadRequest(
                "Result already exists for this participant"
            );

        // Save result
        result.Id = 0;

        _db.Results.Add(result);

        await _db.SaveChangesAsync();

        return Ok(result);
    }

    // Participant sees own results
    [HttpGet("my")]
    public async Task<ActionResult<List<Result>>> MyResults()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Results.Where(r => r.ParticipantId == userId).ToListAsync();
    }

    // Anyone logged in can see results for an event - leaderboard
    [HttpGet("byEvent/{eventId}")]
    public async Task<ActionResult<List<Result>>> ByEvent(int eventId)
        => await _db.Results.Where(r => r.EventId == eventId).OrderBy(r => r.Position).ToListAsync();
}
