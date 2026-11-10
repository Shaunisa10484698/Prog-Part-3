using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers;

public class EnrolmentsController : Controller
{
    private readonly ApiClient _api;
    public EnrolmentsController(ApiClient api) => _api = api;
    private string? Token => HttpContext.Session.GetString("Token");
    private string? Role => HttpContext.Session.GetString("Role");

    [HttpGet]
    public async Task<IActionResult> Enrol(int eventId)
    {
        if (Role != "Participant") return RedirectToAction("Login", "Account");
        var categories = await _api.GetAsync<List<Category>>($"Categories/byEvent/{eventId}") ?? new List<Category>();
        ViewBag.EventId = eventId;
        return View(categories);
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var enrol = new Enrolment { EventId = eventId, CategoryId = categoryId, Status = "Confirmed", EnrolledDate = DateTime.Now };
        var res = await _api.PostAsync("Enrolments", enrol, Token);
        if (!res.IsSuccessStatusCode) ViewBag.Error = "Already enrolled or error";
        else return RedirectToAction("My");
        var categories = await _api.GetAsync<List<Category>>($"Categories/byEvent/{eventId}") ?? new List<Category>();
        ViewBag.EventId = eventId;
        return View(categories);
    }

    public async Task<IActionResult> My()
    {
        var list = await _api.GetAsync<List<Enrolment>>("Enrolments/my", Token) ?? new List<Enrolment>();
        return View(list);
    }

    public async Task<IActionResult> ByEvent(int eventId)
    {
        var list = await _api.GetAsync<List<Enrolment>>($"Enrolments/byEvent/{eventId}", Token) ?? new List<Enrolment>();
        ViewBag.EventId = eventId;
        return View(list);
    }

    public async Task<IActionResult> Cancel(int id)
    {
        await _api.DeleteAsync($"Enrolments/{id}", Token);
        return RedirectToAction("My");
    }
}