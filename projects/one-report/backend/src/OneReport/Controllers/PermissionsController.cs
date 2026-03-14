using Microsoft.AspNetCore.Mvc;
using OneReport.Models.DTOs;
using OneReport.Models.Responses;
using OneReport.Services.Interfaces;

namespace OneReport.Controllers;

/// <summary>
/// 权限管理 API - 用户权限管理
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        IPermissionService permissionService,
        ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// 检查用户权限
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckPermission(
        [FromQuery] Guid userId,
        [FromQuery] string resourceType,
        [FromQuery] Guid? resourceId,
        [FromQuery] string permission,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.HasPermissionAsync(userId, resourceType, resourceId, permission, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    /// 获取用户权限列表
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<List<UserPermissionDto>>>> GetUserPermissions(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetUserPermissionsAsync(userId, cancellationToken);
        return Ok(ApiResponse<List<UserPermissionDto>>.Ok(result));
    }

    /// <summary>
    /// 授予用户权限
    /// </summary>
    [HttpPost("grant")]
    public async Task<ActionResult<ApiResponse<UserPermissionDto>>> GrantPermission(
        [FromBody] CreateUserPermissionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _permissionService.GrantPermissionAsync(dto, cancellationToken);
            return Ok(ApiResponse<UserPermissionDto>.Ok(result, "权限授予成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "授予权限失败");
            return BadRequest(ApiResponse<UserPermissionDto>.Fail("授予权限失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 撤销用户权限
    /// </summary>
    [HttpDelete("{permissionId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokePermission(
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.RevokePermissionAsync(permissionId, cancellationToken);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("权限不存在"));
        
        return Ok(ApiResponse<bool>.Ok(true, "权限已撤销"));
    }

    /// <summary>
    /// 获取用户角色
    /// </summary>
    [HttpGet("user/{userId:guid}/role")]
    public async Task<ActionResult<ApiResponse<UserRoleDto?>>> GetUserRole(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetUserRoleAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserRoleDto?>.Ok(result));
    }

    /// <summary>
    /// 设置用户角色
    /// </summary>
    [HttpPost("user/role")]
    public async Task<ActionResult<ApiResponse<UserRoleDto>>> SetUserRole(
        [FromBody] SetUserRoleDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _permissionService.SetUserRoleAsync(dto, cancellationToken);
            return Ok(ApiResponse<UserRoleDto>.Ok(result, "角色设置成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UserRoleDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置角色失败");
            return BadRequest(ApiResponse<UserRoleDto>.Fail("设置角色失败", new List<string> { ex.Message }));
        }
    }
}