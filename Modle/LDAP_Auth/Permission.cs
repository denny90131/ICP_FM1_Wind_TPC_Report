public class Permission
{
    // ID
    public Guid Id { get; set; } = Guid.NewGuid();

    // 權限唯一代碼
    public string Code { get; set; } = string.Empty;

    // 描述
    public string Description { get; set; } = string.Empty;

    // 分類模塊
    public string Module { get; set; } = string.Empty;
    
    //內外部權限對照表
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}