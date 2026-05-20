using DP.AppWMS.ApiService.Filters;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.DTOs;
using WMS.Application.Inbound.Services;

namespace DP.AppWMS.ApiService.Controllers;

public class InboundController(IInboundService svc) : BaseController
{
    private readonly IInboundService _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InboundOrderDto>>>> GetList(
        CancellationToken ct)
    {
        var result = await _svc.GetListAsync(ct);
        return Ok(ApiResponse<List<InboundOrderDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InboundOrderDto>>> GetById(
        Guid id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return Ok(ApiResponse<InboundOrderDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "admin,manager,planner")]
    public async Task<ActionResult<ApiResponse<InboundOrderDto>>> Create(
        [FromBody] CreateInboundRequest request,
        [FromServices] IValidator<CreateInboundRequest> validator,
        CancellationToken ct)
    {
        await ValidationFilter.ValidateAsync(validator, request);
        var result = await _svc.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result?.Id },
            ApiResponse<InboundOrderDto>.Ok(result));
    }

    [HttpPut("{id:guid}/receive")]
    [Authorize(Roles = "admin,manager,keeper")]
    public async Task<ActionResult<ApiResponse>> Receive(
        Guid id, [FromBody] ReceiveInboundRequest request, CancellationToken ct)
    {
        await _svc.ReceiveAsync(id, request, ct);
        return Ok(ApiResponse.Ok(null, "Đã ghi nhận nhận hàng"));
    }

    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "admin,manager")]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id, CancellationToken ct)
    {
        await _svc.CancelAsync(id, ct);
        return Ok(ApiResponse.Ok(null, "Đã hủy đơn nhập"));
    }
}
