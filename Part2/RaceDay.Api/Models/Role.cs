namespace RaceDay.Api.Models;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    // One role has many users (Roles 1 : M Users)
    public ICollection<User> Users { get; set; } = new List<User>();
}