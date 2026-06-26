using DP.AppWMS.ApiService.Filters;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Inventory.DTOs;
using WMS.Application.Inventory.Services;
using WMS.Domain.Enums;

namespace DP.AppWMS.ApiService.Controllers;

public class InventoryController(IInventoryService svc) : BaseController
{
    private readonly IInventoryService _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryDto>>>> GetList(
        [FromQuery] string? search,
        [FromQuery] string? zone,
        [FromQuery] ItemStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var query = new InventoryQuery(search, null, zone, status, page, limit);
        var result = await _svc.GetListAsync(query, ct);
        return Ok(ApiResponse<PagedResult<InventoryDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> GetById(
        Guid id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return Ok(ApiResponse<InventoryDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "admin,manager")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> Create(
        [FromBody] CreateInventoryRequest request,
        [FromServices] IValidator<CreateInventoryRequest> validator,
        CancellationToken ct)
    {
        await ValidationFilter.ValidateAsync(validator, request);
        var result = await _svc.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<InventoryDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,manager")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id, [FromBody] UpdateInventoryRequest request,
        [FromServices] IValidator<UpdateInventoryRequest> validator,
        CancellationToken ct)
    {
        await ValidationFilter.ValidateAsync(validator, request);
        await _svc.UpdateAsync(id, request, ct);
        return Ok(ApiResponse.Ok(null, "Đã cập nhật sản phẩm"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return Ok(ApiResponse.Ok(null, "Đã xóa sản phẩm"));
    }
}
