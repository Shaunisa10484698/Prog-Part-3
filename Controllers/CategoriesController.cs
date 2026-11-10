
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using System.Security.Claims;

namespace RaceDayAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/Categories
    [HttpGet]
    public async Task<ActionResult<List<Category>>> GetAll()
    {
        return await _db.Categories.ToListAsync();
    }

    // GET: api/Categories/byEvent/5
    [HttpGet("byEvent/{eventId}")]
    public async Task<ActionResult<List<Category>>> GetByEvent(int eventId)
    {
        return await _db.Categories
            .Where(c => c.EventId == eventId)
            .ToListAsync();
    }

    // POST: api/Categories
    [Authorize(Roles = "Organiser")]
    [HttpPost]
    public async Task<ActionResult<Category>> Create(Category cat)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var existingEvent = await _db.Events
            .FindAsync(cat.EventId);

        if (existingEvent == null)
            return NotFound("Event not found");

        if (existingEvent.OrganiserId != userId)
            return Forbid();

        _db.Categories.Add(cat);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetByEvent),
            new { eventId = cat.EventId },
            cat
        );
    }

    // DELETE: api/Categories/5
    [Authorize(Roles = "Organiser")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var category = await _db.Categories
            .FindAsync(id);

        if (category == null)
            return NotFound("Category not found");

        var existingEvent = await _db.Events
            .FindAsync(category.EventId);

        if (existingEvent == null)
            return NotFound("Event not found");

        if (existingEvent.OrganiserId != userId)
            return Forbid();

        _db.Categories.Remove(category);

        await _db.SaveChangesAsync();

        return Ok("Category deleted successfully");
    }
}
