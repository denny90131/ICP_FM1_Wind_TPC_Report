public class RolePermission
{
    //外部ID
    public Guid RoleId { get; set; }
    //外部Class
    public Role Role { get; set; } = null!;
    //內部ID
    public Guid PermissionId { get; set; }
    //內部Class
    public Permission Permission { get; set; } = null!;
}