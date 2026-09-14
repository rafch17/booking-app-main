using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using booking.core.Constants;
using booking.core.DTOs;
using booking.core.Interfaces;

namespace booking.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServicesService _svc;
    public ServicesController(IServicesService svc) => _svc = svc;

    [HttpGet]
    [Authorize(Policy = "Service.Read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var (ok, data, code) = await _svc.GetAllAsync(ct);
        return Ok(new { code, data });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Service.Read")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var (ok, data, code) = await _svc.GetByIdAsync(id, ct);
        if (!ok || data is null) return NotFound(new { code = Codes.NotFound, message = "Service not found." });
        return Ok(new { code, data });
    }

    [HttpPost]
    [Authorize(Policy = "Service.Create")]
    public async Task<IActionResult> Create([FromBody] ServiceUpsertDto body, CancellationToken ct)
    {
        var (ok, id, code, message) = await _svc.CreateAsync(body, ct);
        if (!ok) return BadRequest(new { code, message });
        return CreatedAtAction(nameof(GetById), new { id }, new { code = Codes.Ok, data = new { id } });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Service.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] ServiceUpsertDto body, CancellationToken ct)
    {
        var (ok, code, message) = await _svc.UpdateAsync(id, body, ct);
        if (!ok && code == Codes.NotFound) return NotFound(new { code, message });
        if (!ok) return BadRequest(new { code, message });
        return Ok(new { code = Codes.Ok });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Service.Delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var (ok, code, message) = await _svc.DeleteAsync(id, ct);
        if (!ok && code == Codes.NotFound) return NotFound(new { code, message });
        if (!ok) return BadRequest(new { code, message });
        return Ok(new { code = Codes.Ok });
    }
}
