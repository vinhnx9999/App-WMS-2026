using DP.AppWMS.ApiService.Filters;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Outbound.DTOs;
using WMS.Application.Outbound.Services;

namespace DP.AppWMS.ApiService.Controllers;

public class OutboundController(IOutboundService svc) : BaseController
{
    private readonly IOutboundService _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OutboundOrderDto>>>> GetList(
        CancellationToken ct)
    {
        var result = await _svc.GetListAsync(ct);
        return Ok(ApiResponse<List<OutboundOrderDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OutboundOrderDto>>> GetById(
        Guid id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return Ok(ApiResponse<OutboundOrderDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "admin,manager,planner")]
    public async Task<ActionResult<ApiResponse<OutboundOrderDto>>> Create(
        [FromBody] CreateOutboundRequest request,
        [FromServices] IValidator<CreateOutboundRequest> validator,
        CancellationToken ct)
    {
        await ValidationFilter.ValidateAsync(validator, request);
        var result = await _svc.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<OutboundOrderDto>.Ok(result));
    }

    [HttpPut("{id:guid}/ship")]
    [Authorize(Roles = "admin,manager,keeper")]
    public async Task<ActionResult<ApiResponse>> Ship(
        Guid id, [FromBody] ShipOutboundRequest request, CancellationToken ct)
    {
        await _svc.ShipAsync(id, request, ct);
        return Ok(ApiResponse.Ok(null, "Đã xác nhận xuất kho"));
    }

    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "admin,manager")]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id, CancellationToken ct)
    {
        await _svc.CancelAsync(id, ct);
        return Ok(ApiResponse.Ok(null, "Đã hủy đơn xuất"));
    }
}
