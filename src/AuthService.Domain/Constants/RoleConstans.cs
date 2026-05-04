namespace AuthService.Domain.Constants;

public class RoleConstants
{
    public const string ADMIN_ROLE = "ADMIN";
    public const string USER_ROLE = "CLIENT"; 
    public const string WORKER_ROLE = "WORKER";

    public static readonly string[] AllowedRoles = 
    [
        ADMIN_ROLE, 
        USER_ROLE, 
        WORKER_ROLE
    ];
}