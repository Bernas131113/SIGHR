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

        [Display(Name = "Nome Completo")]
        public string? NomeCompleto { get; set; } // Pode ser nulo, então 'string?' está OK

        [Required(ErrorMessage = "O PIN é obrigatório.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "O PIN deve conter exatamente 4 números.")]
        [Display(Name = "PIN")]
        public int PIN { get; set; }

        [Display(Name = "Tipo/Role")]
        public string? Tipo { get; set; } // Pode ser nulo/opcional
    }
}