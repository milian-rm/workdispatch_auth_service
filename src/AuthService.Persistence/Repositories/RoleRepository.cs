using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Persistence.Data;
using Microsoft.EntityFrameworkCore;
 
namespace AuthService.Persistence.Repositories;
 
public class RoleRepository(ApplicationDbContext context) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(string id)
    {
        return await context.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
 
    public async Task<Role?> GetByNameAsync(string roleName)
    {
        return await context.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => EF.Functions.ILike(r.Name, roleName));
    }
 
    public async Task<int> CountUsersByRoleIdAsync(string roleId)
    {
        return await context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .CountAsync();
    }
    
    public async Task<IReadOnlyList<User>> GetUsersByRoleIdAsync(string roleId)
    {
        var users = await context.UserRoles
            .Where(ur => ur.RoleId == roleId) // Corregido: Usar el parámetro roleId
            .Select(ur => ur.User)
            .Include(u => u.UserProfile)
            .Include(u => u.UserEmail)
            .Include(u => u.UserPasswordReset)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync();

        return users.AsReadOnly();
    }
 
    public async Task<IReadOnlyList<string>> GetUserRoleNamesAsync(string userId)
    {
        var roles = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
           
        return roles.AsReadOnly();
    }
 
    // Agrega estos dos métodos dentro de tu clase RoleRepository
 
    public async Task<int> CountUsersInRoleAsync(string roleName)
    {
    // Cuenta cuántos registros hay en la tabla intermedia basándose en el nombre del rol
    return await context.UserRoles
        .Include(ur => ur.Role)
        .CountAsync(ur => ur.Role.Name == roleName);
    }
 
    public async Task<List<User>> GetUsersByRoleAsync(string roleName)
{
    // Buscamos en la tabla intermedia, incluimos la información del usuario
    // y su perfil/email porque tu DTO los va a necesitar para mapear.
    return await context.UserRoles
        .Include(ur => ur.User)
            .ThenInclude(u => u.UserProfile)
        .Include(ur => ur.User)
            .ThenInclude(u => u.UserEmail)
        .Where(ur => ur.Role.Name == roleName)
        .Select(ur => ur.User)
        .ToListAsync();
}
}