using DP.AppWMS.ApiService.Filters;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Warehouse.Zones.DTOs;
using WMS.Application.Warehouse.Zones.Services;

namespace DP.AppWMS.ApiService.Controllers;

public class ZonesController(IZoneService svc) : BaseController
{
    private readonly IZoneService _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ZoneDto>>>> GetAll(CancellationToken ct)
    {
        var result = await _svc.GetAllAsync(ct);
        return Ok(ApiResponse<List<ZoneDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ZoneDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return Ok(ApiResponse<ZoneDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<ZoneDto>>> Create(
        [FromBody] CreateZoneRequest request,
        [FromServices] IValidator<CreateZoneRequest> validator,
        CancellationToken ct)
    {
        await ValidationFilter.ValidateAsync(validator, request);
        var result = await _svc.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<ZoneDto>.Ok(result));
    }
}
