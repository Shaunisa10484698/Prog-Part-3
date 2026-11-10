using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class CategoriesController : Controller
{
    private readonly ApiClient _api;
    public CategoriesController(ApiClient api) => _api = api;
    private string? Token => HttpContext.Session.GetString("Token");

    [HttpGet]
    public IActionResult Create(int eventId) { ViewBag.EventId = eventId; return View(); }

    [HttpPost]
    public async Task<IActionResult> Create(Category cat)
    {
        await _api.PostAsync("Categories", cat, Token);
        return RedirectToAction("Details", "Events", new { id = cat.EventId });
    }
}