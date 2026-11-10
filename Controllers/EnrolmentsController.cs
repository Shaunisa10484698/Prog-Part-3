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
public class EnrolmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public EnrolmentsController(ApplicationDbContext db) => _db = db;

    // Participant enrols
    [Authorize(Roles = "Participant")]
    [HttpPost]
    public async Task<ActionResult<Enrolment>> Enrol(Enrolment enrol)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        enrol.ParticipantId = userId;
        enrol.EnrolledDate = DateTime.Now;

        // Prevent double enrolment
        if (await _db.Enrolments.AnyAsync(e => e.EventId == enrol.EventId && e.ParticipantId == userId))
            return BadRequest("Already enrolled in this event");

        _db.Enrolments.Add(enrol);
        await _db.SaveChangesAsync();
        return Ok(enrol);
    }

    // My enrolments - Participant
    [HttpGet("my")]
    public async Task<ActionResult<List<Enrolment>>> MyEnrolments()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Enrolments.Where(e => e.ParticipantId == userId).ToListAsync();
    }

    // All enrolments for an event - Organiser
    [Authorize(Roles = "Organiser")]
    [HttpGet("byEvent/{eventId}")]
    public async Task<ActionResult<List<Enrolment>>> ByEvent(int eventId)
        => await _db.Enrolments.Where(e => e.EventId == eventId).ToListAsync();


    [Authorize(Roles = "Participant")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var enrolment = await _db.Enrolments
            .FirstOrDefaultAsync(e =>
                e.Id == id && e.ParticipantId == userId);

        if (enrolment == null)
            return NotFound("Enrolment not found");

        _db.Enrolments.Remove(enrolment);

        await _db.SaveChangesAsync();

        return Ok("Enrolment cancelled successfully");
    }

}