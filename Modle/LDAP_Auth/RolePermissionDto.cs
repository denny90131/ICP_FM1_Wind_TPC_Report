public class RoleDetailDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string AdGroupName { get; set; } = string.Empty;
    public List<Guid> AssignedPermissionIds { get; set; } = new();
}

public class UpdateRolePermissionsRequest
{
    public Guid RoleId { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
}

public class PermissionGroupDto
{
    public string Module { get; set; } = string.Empty;
    public List<PermissionItemDto> Permissions { get; set; } = new();
}

public class PermissionItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

//各AD群組之 統計值 ((int)AD群組名稱 -- (int)有開啟幾個權限 -- (bool)是否有權限啟用)
public class RoleListItemDto
{
    public Guid RoleId { get; set; } // AD群組 UID
    public string RoleName { get; set; } = string.Empty;  // AD群組名稱
    public string AdGroupName { get; set; } = string.Empty; // AD群組名稱 (預防需要備稱)
    public int PermissionCount { get; set; } // 啟用之權限數量
    public bool HasPermissions { get; set; } // 是否啟用權限
}