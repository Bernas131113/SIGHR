using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    public class CreateUtilizadorViewModel
    {
        [Required]
        [Display(Name = "Nome de Utilizador")]
        public string UserName { get; set; } = string.Empty; // Inicializado

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty; // Inicializado

        [Required]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; } = string.Empty; // Inicializado

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Senha")]
        [Compare("Password", ErrorMessage = "A senha e a senha de confirmação não correspondem.")]
        public string ConfirmPassword { get; set; } = string.Empty; // Inicializado

        [Display(Name = "Nome Completo")]
        public string? NomeCompleto { get; set; } // Pode ser nulo, então 'string?' está OK

        [Required]
        public int PIN { get; set; } // int não tem esse aviso, a menos que seja uma classe.

        [Display(Name = "Tipo/Role")]
        public string? Tipo { get; set; } // Pode ser nulo/opcional
    }
}