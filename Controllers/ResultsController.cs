using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class ResultsController : Controller
{
    private readonly ApiClient _api;
    public ResultsController(ApiClient api) => _api = api;
    private string? Token => HttpContext.Session.GetString("Token");

    [HttpGet]
    public IActionResult Create(int eventId) { ViewBag.EventId = eventId; return View(); }

    [HttpPost]
    public async Task<IActionResult> Create(Result result)
    {
        var res = await _api.PostAsync("Results", result, Token);
        if (!res.IsSuccessStatusCode) { ViewBag.Error = "Failed - check Participant ID"; ViewBag.EventId = result.EventId; return View(result); }
        return RedirectToAction("Details", "Events", new { id = result.EventId });
    }

    public async Task<IActionResult> My()
    {
        var list = await _api.GetAsync<List<Result>>("Results/my", Token) ?? new List<Result>();
        return View(list);
    }
}