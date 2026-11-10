
using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class EventsController : Controller
{
    private readonly ApiClient _api;
    private readonly IWebHostEnvironment _env;

    public EventsController(ApiClient api, IWebHostEnvironment env)
    {
        _api = api;
        _env = env;
    }

    private string? Token() =>
        HttpContext.Session.GetString("Token");

    private string? Role() =>
        HttpContext.Session.GetString("Role");

    private string? UserId() =>
        HttpContext.Session.GetString("UserId");

    private bool IsOrganiser() =>
        !string.IsNullOrEmpty(Token()) &&
        Role() == "Organiser";

    private bool IsLoggedIn() =>
        !string.IsNullOrEmpty(Token());

    // CHECK EVENT OWNERSHIP
    private async Task<bool> OwnsEvent(int eventId)
    {
        var userId = UserId();

        if (!IsOrganiser() || string.IsNullOrEmpty(userId))
            return false;

        var ev = await _api.GetAsync<Event>(
            $"Events/{eventId}", Token());

        return ev != null &&
               ev.OrganiserId.ToString() == userId;
    }

    // PUBLIC EVENT LIST
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? type,
        DateTime? date)
    {
        var events =
            await _api.GetAsync<List<Event>>("Events") ?? new();

        if (!string.IsNullOrEmpty(search))
        {
            events = events.Where(e =>
                e.Name.Contains(search,
                    StringComparison.OrdinalIgnoreCase) ||
                (e.Location ?? "").Contains(search,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrEmpty(type))
            events = events.Where(e => e.EventType == type).ToList();

        if (date.HasValue)
            events = events.Where(e =>
                e.Date.Date == date.Value.Date).ToList();

        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");

        ViewBag.Types = events.Select(e => e.EventType)
            .Distinct()
            .ToList();

        return View(events);
    }

    // ORGANISER DASHBOARD
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        if (!IsOrganiser())
            return RedirectToAction("Login", "Account");

        var events =
            await _api.GetAsync<List<Event>>("Events", Token()) ?? new();

        var myId = UserId();

        var myEvents = events
            .Where(e => e.OrganiserId.ToString() == myId)
            .ToList();

        var counts = new Dictionary<int, int>();

        foreach (var e in myEvents)
        {
            var enrols =
                await _api.GetAsync<List<Enrolment>>(
                    $"Enrolments/ByEvent/{e.Id}", Token()) ?? new();

            counts[e.Id] = enrols.Count;
        }

        ViewBag.MyEvents = myEvents;
        ViewBag.Counts = counts;
        ViewBag.TotalEvents = myEvents.Count;
        ViewBag.TotalEnrolments = counts.Values.Sum();

        ViewBag.Upcoming = myEvents
            .Where(e => e.Date >= DateTime.Today)
            .OrderBy(e => e.Date)
            .ToList();

        return View(myEvents);
    }

    // PUBLIC EVENT DETAILS
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<Event>(
            $"Events/{id}", Token());

        if (ev == null)
            return NotFound();

        ViewBag.Categories =
            await _api.GetAsync<List<Category>>(
                $"Categories/ByEvent/{id}", Token()) ?? new();

        ViewBag.Results =
            await _api.GetAsync<List<Result>>(
                $"Results/ByEvent/{id}", Token()) ?? new();

        return View(ev);
    }

    // CREATE EVENT GET
    [HttpGet]
    public IActionResult Create()
    {
        if (!IsOrganiser())
            return RedirectToAction("Login", "Account");

        return View();
    }

    // CREATE EVENT POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Event ev, IFormFile? banner)
    {
        if (!IsOrganiser())
            return RedirectToAction("Login", "Account");

        if (banner != null && banner.Length > 0)
        {
            var fn = Guid.NewGuid() +
                     Path.GetExtension(banner.FileName);

            var folder = Path.Combine(
                _env.WebRootPath, "images");

            Directory.CreateDirectory(folder);

            using var stream = System.IO.File.Create(
                Path.Combine(folder, fn));

            await banner.CopyToAsync(stream);

            ev.BannerImage = "/images/" + fn;
        }

        var res = await _api.PostAsync(
            "Events", ev, Token());

        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index");

        ViewBag.Error =
            "Failed to create event. Check your organiser permissions.";

        return View(ev);
    }

    // EDIT EVENT GET
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsOrganiser())
            return RedirectToAction("Login", "Account");

        if (!await OwnsEvent(id))
            return RedirectToAction("Index");

        var ev = await _api.GetAsync<Event>(
            $"Events/{id}", Token());

        if (ev == null)
            return NotFound();

        return View(ev);
    }

    // EDIT EVENT POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Event ev, IFormFile? banner)
    {
        if (!IsOrganiser())
            return RedirectToAction("Login", "Account");

        if (!await OwnsEvent(ev.Id))
            return RedirectToAction("Index");

        if (banner != null && banner.Length > 0)
        {
            var fn = Guid.NewGuid() +
                     Path.GetExtension(banner.FileName);

            var folder = Path.Combine(
                _env.WebRootPath, "images");

            Directory.CreateDirectory(folder);

            using var stream = System.IO.File.Create(
                Path.Combine(folder, fn));

            await banner.CopyToAsync(stream);

            ev.BannerImage = "/images/" + fn;
        }

        var res = await _api.PutAsync(
            $"Events/{ev.Id}", ev, Token());

        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index");

        ViewBag.Error = "Event update failed.";

        return View(ev);
    }

    // DELETE EVENT POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsOrganiser())
            return RedirectToAction("Login", "Account");

        if (!await OwnsEvent(id))
            return RedirectToAction("Index");

        await _api.DeleteAsync(
            $"Events/{id}", Token());

        return RedirectToAction("Index");
    }
}

