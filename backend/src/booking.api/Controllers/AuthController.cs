using Microsoft.AspNetCore.Mvc;
using booking.core.Constants;
using booking.core.DTOs;
using booking.core.Interfaces;

namespace booking.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto, CancellationToken ct)
    {
        var (ok, data, code) = await _auth.LoginAsync(dto.Email, dto.Password, ct);
        if (!ok)
        {
            return code switch
            {
                Codes.UserNotFound or Codes.InvalidCredentials
                    => Unauthorized(new { code, message = "Invalid credentials." }),
                Codes.EmailNotConfirmed
                    => Unauthorized(new { code, message = "Email not confirmed." }),
                _ => Unauthorized(new { code = Codes.Unauthorized, message = "Unauthorized." })
            };
        }
        return Ok(new { code = Codes.Ok, data });
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest dto, CancellationToken ct)
    {
        var (ok, data, code, message) = await _auth.RegisterAsync(dto.Email, dto.Password, dto.UserName, ct);
        if (!ok)
        {
            return BadRequest(new { code, message });
        }
        return Ok(new { code = Codes.Ok, data });
    }
}