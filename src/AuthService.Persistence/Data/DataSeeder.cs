using AuthService.Domain.Entities;
using AuthService.Application.Services;
using AuthService.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Persistence.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 1. Seed de Roles
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new() { 
                    Id = UuidGenerator.GenerateRoleId(), 
                    Name = RoleConstants.ADMIN_ROLE // "ADMIN"
                },
                new() { 
                    Id = UuidGenerator.GenerateRoleId(), 
                    Name = RoleConstants.USER_ROLE // "CLIENT"
                },
                new() { 
                    Id = UuidGenerator.GenerateRoleId(), 
                    Name = RoleConstants.WORKER_ROLE // "WORKER"
                }
            };
            
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        if (!await context.Users.AnyAsync())
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleConstants.ADMIN_ROLE);
            
            if (adminRole != null)
            {
                var passwordHasher = new PasswordHashService();
                
                var userId = UuidGenerator.GenerateUserId();
                var profileId = UuidGenerator.GenerateUserId().Replace("usr_", "prf_");
                var emailId = UuidGenerator.GenerateUserId().Replace("usr_", "eml_");
                var userRoleId = UuidGenerator.GenerateUserId().Replace("usr_", "url_");

                var adminUser = new User
{
                    Id = UuidGenerator.GenerateUserId(),
                    FirstName = "Admin",     
                    LastName = "System",    
                    Email = "admin@system.com",
                    Password = passwordHasher.HashPassword("Admin123!"),
                    Status = true,
                    UserProfile = new UserProfile
                    {
                        Id = UuidGenerator.GenerateUserId(),
                        ProfilePhoto = "default-avatar-url",
                        Phone = "12345678"
                    },
                    
                    UserEmail = new UserEmail
                    {
                        Id = emailId,
                        UserId = userId,
                        EmailVerified = true,
                        EmailVerificationToken = null,
                        EmailVerificationTokenExpiration = null
                    },
                    
                    UserRoles = new List<UserRole>
                    {
                        new UserRole
                        {
                            Id = userRoleId,
                            UserId = userId,
                            RoleId = adminRole.Id
                        }
                    }
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }
        }
    }
}