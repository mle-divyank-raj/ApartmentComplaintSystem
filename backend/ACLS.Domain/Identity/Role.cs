namespace ACLS.Domain.Identity;

/// <summary>
/// The three actor roles in the ACLS system.
/// Stored as nvarchar(50) strings via RoleConverter in EF Core configuration.
/// JWT role claim values must match these enum names exactly.
/// Valid values: "Resident", "Manager", "MaintenanceStaff"
/// </summary>
public enum Role
{
    Resident,
    Manager,
    MaintenanceStaff
}
