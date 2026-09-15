using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tai_wind_integration.Modle;

[Route("api/[controller]")]
[ApiController]
public class RoleManagementController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IGroupPermissionSyncService _syncService;

    public RoleManagementController(AppDbContext db, IGroupPermissionSyncService syncService)
    {
        _db = db;
        _syncService = syncService;
    }

    // 取得所有AD角色基本清單
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleListItemDto
            { 
                RoleId = r.Id, 
                RoleName = r.Name, 
                AdGroupName = r.AdGroupName,
                PermissionCount = r.RolePermissions.Count(),
                HasPermissions = r.RolePermissions.Any()
            })
            .ToListAsync();

        return Ok(roles);
    }

    // 取得指定角色完整資訊
    [HttpGet("roles/{roleId:guid}")]
    public async Task<ActionResult<RoleDetailDto>> GetRoleDetail(Guid roleId)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null) return NotFound("找不到該角色。");

        var dto = new RoleDetailDto
        {
            RoleId = role.Id,
            RoleName = role.Name,
            AdGroupName = role.AdGroupName,
            AssignedPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList()
        };

        return Ok(dto);
    }

    // 取得按模組分類的系統權限清單（使用 PermissionGroupDto）
    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionGroupDto>>> GetGroupedPermissions()
    {
        var permissions = await _db.Permissions
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .ToListAsync();

        var grouped = permissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto
            {
                Module = g.Key,
                Permissions = g.Select(p => new PermissionItemDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Description = p.Description ?? string.Empty
                }).ToList()
            }).ToList();

        return Ok(grouped);
    }

    // 儲存權限配置（使用 UpdateRolePermissionsRequest）
    [HttpPost("update-permissions")]
    public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsRequest req)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == req.RoleId);

        if (role == null) return NotFound("找不到該角色。");

        _db.RolePermissions.RemoveRange(role.RolePermissions);

        foreach (var permId in req.PermissionIds.Distinct())
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = req.RoleId,
                PermissionId = permId
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { Message = "權限配置更新成功！" });
    }

    // 同步 AD 群組
    [HttpPost("sync-ad")]
    public async Task<IActionResult> ForceSyncAd()
    {
        await _syncService.InitializeAsync();
        return Ok(new { Message = "AD 群組與系統權限已重新同步！" });
    }
}