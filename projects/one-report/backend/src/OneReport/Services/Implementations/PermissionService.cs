using Microsoft.EntityFrameworkCore;
using OneReport.Data;
using OneReport.Data.Entities;
using OneReport.Models.DTOs;
using OneReport.Services.Interfaces;

namespace OneReport.Services.Implementations;

/// <summary>
/// 权限服务实现 - 简单的用户权限管理
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        AppDbContext context,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId, 
        string resourceType, 
        Guid? resourceId, 
        string permission,
        CancellationToken cancellationToken = default)
    {
        // 检查是否是管理员
        if (await IsAdminAsync(userId, cancellationToken))
        {
            return true;
        }

        // 检查具体权限
        var hasPermission = await _context.UserPermissions
            .AsNoTracking()
            .AnyAsync(p => 
                p.UserId == userId &&
                p.ResourceType == resourceType.ToLower() &&
                (p.ResourceId == resourceId || p.ResourceId == null) &&
                p.Permission == permission.ToLower(),
                cancellationToken);

        return hasPermission;
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var role = await _context.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);

        return role?.Role == "admin";
    }

    public async Task<List<UserPermissionDto>> GetUserPermissionsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var permissions = await _context.UserPermissions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.ResourceType)
            .ThenBy(p => p.Permission)
            .ToListAsync(cancellationToken);

        return permissions.Select(MapToDto).ToList();
    }

    public async Task<UserPermissionDto> GrantPermissionAsync(
        CreateUserPermissionDto dto, 
        CancellationToken cancellationToken = default)
    {
        // 检查是否已存在相同权限
        var existingPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(p => 
                p.UserId == dto.UserId &&
                p.ResourceType == dto.ResourceType.ToLower() &&
                p.ResourceId == dto.ResourceId &&
                p.Permission == dto.Permission.ToLower(),
                cancellationToken);

        if (existingPermission != null)
        {
            _logger.LogInformation(
                "权限已存在: UserId={UserId}, ResourceType={ResourceType}, Permission={Permission}",
                dto.UserId, dto.ResourceType, dto.Permission);
            return MapToDto(existingPermission);
        }

        var permission = new UserPermission
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            ResourceType = dto.ResourceType.ToLower(),
            ResourceId = dto.ResourceId,
            Permission = dto.Permission.ToLower(),
            CreatedAt = DateTime.UtcNow
        };

        _context.UserPermissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "授予权限: UserId={UserId}, ResourceType={ResourceType}, ResourceId={ResourceId}, Permission={Permission}",
            dto.UserId, dto.ResourceType, dto.ResourceId, dto.Permission);

        return MapToDto(permission);
    }

    public async Task<bool> RevokePermissionAsync(
        Guid permissionId, 
        CancellationToken cancellationToken = default)
    {
        var permission = await _context.UserPermissions.FindAsync(new object[] { permissionId }, cancellationToken);
        if (permission == null)
        {
            return false;
        }

        _context.UserPermissions.Remove(permission);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "撤销权限: PermissionId={PermissionId}, UserId={UserId}",
            permissionId, permission.UserId);

        return true;
    }

    public async Task<UserRoleDto?> GetUserRoleAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var role = await _context.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);

        return role == null ? null : MapToDto(role);
    }

    public async Task<UserRoleDto> SetUserRoleAsync(
        SetUserRoleDto dto, 
        CancellationToken cancellationToken = default)
    {
        // 验证角色有效性
        var validRoles = new[] { "admin", "editor", "viewer" };
        if (!validRoles.Contains(dto.Role.ToLower()))
        {
            throw new ArgumentException($"无效的角色: {dto.Role}. 有效角色: {string.Join(", ", validRoles)}");
        }

        var existingRole = await _context.UserRoles
            .FirstOrDefaultAsync(r => r.UserId == dto.UserId, cancellationToken);

        if (existingRole != null)
        {
            // 更新角色
            existingRole.Role = dto.Role.ToLower();
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "更新用户角色: UserId={UserId}, Role={Role}",
                dto.UserId, dto.Role);
            
            return MapToDto(existingRole);
        }
        else
        {
            // 创建新角色
            var role = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Role = dto.Role.ToLower(),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "设置用户角色: UserId={UserId}, Role={Role}",
                dto.UserId, dto.Role);

            return MapToDto(role);
        }
    }

    private UserPermissionDto MapToDto(UserPermission permission)
    {
        return new UserPermissionDto
        {
            Id = permission.Id,
            UserId = permission.UserId,
            ResourceType = permission.ResourceType,
            ResourceId = permission.ResourceId,
            Permission = permission.Permission,
            CreatedAt = permission.CreatedAt
        };
    }

    private UserRoleDto MapToDto(UserRole role)
    {
        return new UserRoleDto
        {
            Id = role.Id,
            UserId = role.UserId,
            Role = role.Role,
            CreatedAt = role.CreatedAt
        };
    }
}