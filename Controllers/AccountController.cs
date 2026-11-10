
using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;
using RaceDayWeb.ViewModels;
using System.Text.Json;

namespace RaceDayWeb.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;
    private readonly IWebHostEnvironment _env;

    public AccountController(ApiClient api, IWebHostEnvironment env)
    {
        _api = api;
        _env = env;
    }

    private string? Token() =>
        HttpContext.Session.GetString("Token");

    // LOGIN GET
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // LOGIN POST
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var res = await _api.PostAsync(
            "Auth/login",
            new
            {
                Email = model.Email,
                Password = model.Password
            });

        if (!res.IsSuccessStatusCode)
        {
            var error = await res.Content.ReadAsStringAsync();
            ViewBag.Error = "Login failed: " + error;
            return View(model);
        }

        var json = await res.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? Get(string name)
        {
            foreach (var p in root.EnumerateObject())
            {
                if (p.Name.Equals(
                    name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return p.Value.ValueKind == JsonValueKind.String
                        ? p.Value.GetString()
                        : p.Value.ToString();
                }
            }

            return null;
        }

        string? token = Get("token");
        string? role = Get("role");
        string? id = Get("id");
        string? fullName = Get("fullName");

        if (root.TryGetProperty("user", out var userEl) ||
            root.TryGetProperty("User", out userEl))
        {
            foreach (var p in userEl.EnumerateObject())
            {
                if (p.Name.Equals(
                    "role",
                    StringComparison.OrdinalIgnoreCase))
                    role = p.Value.GetString();

                if (p.Name.Equals(
                    "id",
                    StringComparison.OrdinalIgnoreCase))
                    id = p.Value.ToString();

                if (p.Name.Equals(
                    "fullName",
                    StringComparison.OrdinalIgnoreCase))
                    fullName = p.Value.GetString();
            }
        }

        if (string.IsNullOrEmpty(token))
        {
            ViewBag.Error =
                "Login succeeded but no token was returned.";
            return View(model);
        }

        HttpContext.Session.SetString("Token", token);
        HttpContext.Session.SetString("Role", role ?? "");
        HttpContext.Session.SetString("UserId", id ?? "");
        HttpContext.Session.SetString("FullName", fullName ?? "");

        if (role == "Organiser")
            return RedirectToAction("Index", "Events");

        return RedirectToAction("Index", "Home");
    }

    // REGISTER GET
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // REGISTER POST
    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterViewModel model)
    {
        var user = new User
        {
            FullName = model.FullName,
            Email = model.Email,
            PasswordHash = model.Password,
            Role = "Participant"
        };

        var res = await _api.PostAsync(
            "Auth/register", user);

        if (!res.IsSuccessStatusCode)
        {
            ViewBag.Error =
                await res.Content.ReadAsStringAsync();

            return View(model);
        }

        return RedirectToAction("Login");
    }

    // LOGOUT
    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // PROFILE GET
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var token = Token();

        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        try
        {
            var user = await _api.GetAsync<User>(
                "Auth/Profile", token);

            if (user == null)
            {
                ViewBag.Error =
                    "Could not load your profile.";

                return View();
            }

            return View(user);
        }
        catch (HttpRequestException ex)
        {
            ViewBag.Error =
                "Unable to load profile: " + ex.Message;

            return View();
        }
    }

    // PROFILE POST
    [HttpPost]
    public async Task<IActionResult> Profile(
        User u, IFormFile? pic)
    {
        var token = Token();

        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        if (pic != null && pic.Length > 0)
        {
            var fn = Guid.NewGuid() +
                     Path.GetExtension(pic.FileName);

            var folder = Path.Combine(
                _env.WebRootPath, "images");

            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, fn);

            using var stream =
                System.IO.File.Create(path);

            await pic.CopyToAsync(stream);

            u.ProfilePicture = "/images/" + fn;
        }

        var response = await _api.PutAsync(
            "Auth/Profile", u, token);

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error =
                await response.Content.ReadAsStringAsync();

            return View(u);
        }

        HttpContext.Session.SetString(
            "FullName", u.FullName ?? "");

        return RedirectToAction("Profile");
    }
}
