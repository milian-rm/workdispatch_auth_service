using AuthService.Domain.Entities;

using Microsoft.EntityFrameworkCore;
 
namespace AuthService.Persistence.Data;
 
public class ApplicationDbContext : DbContext

{

    // MÉTODO CONSTRUCTOR
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }
 
    // REPRESENTACIÓN DE TABLAS EN EL MODELO
    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserEmail> UserEmails { get; set; }
    public DbSet<UserPasswordReset> UserPasswordResets { get; set; }
 
 
    // CONVIERTE CAMEL CASE A SNAKE CASE
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }
            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (!string.IsNullOrEmpty(columnName))
                {
                    property.SetColumnName(ToSnakeCase(columnName));
                }
            }
        }
 
            // ------------------------------------------------------------
            // CONFIGURACIÓN ESPECÍFICA DE LA ENTIDAD USER
            // ------------------------------------------------------------
            modelBuilder.Entity<User>(entity =>
            {
            // llave primarira
            entity.HasKey(e => e.Id);
            // indices únicos
            entity.HasIndex(e => e.Email).IsUnique();
            // Relación de 1:1 con UserProfile
            entity.HasOne(e => e.UserProfile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Relación 1:N con UserRoles (un usuario puede tener varios roles)
            entity.HasMany(e => e.UserRoles)

                .WithOne(ur => ur.User)

                .HasForeignKey(ur => ur.UserId)

                .OnDelete(DeleteBehavior.Cascade);
 
            // Relación 1:1 con UserEmail

            entity.HasOne(e => e.UserEmail)

                .WithOne(ue => ue.User)

                .HasForeignKey<UserEmail>(ue => ue.UserId)

                .OnDelete(DeleteBehavior.Cascade);
 
            // Relación 1:1 con UserPasswordReset

            entity.HasOne(e => e.UserPasswordReset)

                .WithOne(upr => upr.User)

                .HasForeignKey<UserPasswordReset>(upr => upr.UserId)

                .OnDelete(DeleteBehavior.Cascade);

            });
 
            // CONFIGURACIÓN ESPECÍFICA DE LA ENTIDAD USERROLE

            modelBuilder.Entity<UserRole>(entity =>

            {

                entity.HasKey(e => e.Id);

                // El usuario no puede tener el mismo rol más de una vez

                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

            });
 
            // ------------------------------------------------------------

            // CONFIGURACIÓN ESPECÍFICA DE LA ENTIDAD ROLE

            // ------------------------------------------------------------

            modelBuilder.Entity<Role>(entity =>

            {

                entity.HasKey(e => e.Id);

                // Los nombbre de rol deben ser únicos

                entity.HasIndex(e => e.Name).IsUnique();

            });

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(p =>p.RatingAverage).HasPrecision(3, 2); 
                entity.Property(p => p.Latitude).HasPrecision(18, 15); 
                entity.Property(p => p.Longitude).HasPrecision(18, 15); 

        });

    }

    // ------------------------------------------------------------

    // FUNCIÓN PARA CONFIGURAR EL NOMBRE DE DE CLASE A NOMBRE DE DB

    private static string ToSnakeCase(string input)

    {

        if (string.IsNullOrEmpty(input))

            return input;
 
        return string.Concat(

            input.Select((x, i) => i > 0 && char.IsUpper(x) 

                ? "_" + x.ToString().ToLower() 

                : x.ToString().ToLower())

        );

    }
 
}
 