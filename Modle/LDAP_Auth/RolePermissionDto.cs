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