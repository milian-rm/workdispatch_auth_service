using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain.Entities;

    public class Role
    {
        [Key]
        [MaxLength(16)]
        public string Id { get; set; }

        [Required(ErrorMessage = "El nombre de rol es obligatorio")]
        [MaxLength(100, ErrorMessage= "El nombre de rol no puede superar los 100 caracteres.")]
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        //Relaciones con UserRole
        public ICollection<UserRole> UserRoles { get; set; }
    }
