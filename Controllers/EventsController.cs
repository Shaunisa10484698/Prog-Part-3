using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using System.Security.Claims;

namespace RaceDayAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public EventsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Event>>> GetEvents()
        => await _db.Events.ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEvent(int id)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null) return NotFound();
        return e;
    }

    [Authorize(Roles = "Organiser")]
    [HttpPost]
    public async Task<ActionResult<Event>> CreateEvent(Event ev)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ev.OrganiserId = int.Parse(userId!);
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEvent), new { id = ev.Id }, ev);
    }


    [Authorize(Roles = "Organiser")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, Event ev)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var existingEvent = await _db.Events.FindAsync(id);

        if (existingEvent == null)
            return NotFound("Event not found");

        if (existingEvent.OrganiserId != userId)
            return Forbid();

        existingEvent.Name = ev.Name;
        existingEvent.Description = ev.Description;
        existingEvent.Date = ev.Date;
        existingEvent.Location = ev.Location;
        existingEvent.Distance = ev.Distance;
        existingEvent.EventType = ev.EventType;
        existingEvent.BannerImage = ev.BannerImage;

        await _db.SaveChangesAsync();

        return Ok("Event updated successfully");
    }



    [Authorize(Roles = "Organiser")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var existingEvent = await _db.Events.FindAsync(id);

        if (existingEvent == null)
            return NotFound("Event not found");

        if (existingEvent.OrganiserId != userId)
            return Forbid();

        _db.Events.Remove(existingEvent);

        await _db.SaveChangesAsync();

        return Ok("Event deleted successfully");
    }

}