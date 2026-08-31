public class Role
{
    // ID
    public Guid Id { get; set; } = Guid.NewGuid();
    // Name
    public string Name { get; set; } = string.Empty;
    // AD群組名稱
    public string AdGroupName { get; set; } = string.Empty;
    // 描述
    public string? Description { get; set; }
    // 內外部權限對照表
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}