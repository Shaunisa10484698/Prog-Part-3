
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDayAPI.Data;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(
        ApplicationDbContext db,
        IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // REGISTER
    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        if (string.IsNullOrWhiteSpace(user.FullName) ||
            string.IsNullOrWhiteSpace(user.Email) ||
            string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return BadRequest("All required fields must be completed");
        }

        if (await _db.Users.AnyAsync(u => u.Email == user.Email))
            return BadRequest("Email already exists");

        user.Role = "Participant";
        user.PasswordHash = Hash(user.PasswordHash);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Registered successfully" });
    }

    // LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest("Email and password are required");
        }

        var hashedPassword = Hash(dto.Password);

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == dto.Email &&
            u.PasswordHash == hashedPassword);

        if (user == null)
            return Unauthorized("Invalid email or password");

        var token = GenerateToken(user);

        return Ok(new
        {
            token = token,
            id = user.Id,
            fullName = user.FullName,
            email = user.Email,
            role = user.Role
        });
    }

    // UPDATE PROFILE

    [Authorize]
    [HttpGet("Profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized("Invalid user token");

        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return NotFound("User not found");

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.ProfilePicture
        });
    }


    // PASSWORD HASHING
    private string Hash(string password)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(password));

        return Convert.ToHexString(bytes);
    }

    // GENERATE JWT TOKEN
    private string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var creds = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    // LOGIN DTO
    public class LoginDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}

