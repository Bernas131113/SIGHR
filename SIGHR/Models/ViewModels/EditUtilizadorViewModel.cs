using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    public class EditUtilizadorViewModel
    {
        public required string Id { get; set; } // Marcado como required

        [Required]
        [Display(Name = "Nome de Utilizador")]
        public required string UserName { get; set; } // Marcado como required

        [Required]
        [EmailAddress]
        public required string Email { get; set; } // Marcado como required

        [Display(Name = "Nome Completo")]
        public string? NomeCompleto { get; set; }

        [Required(ErrorMessage = "O PIN é obrigatório.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "O PIN deve conter exatamente 4 números.")]
        [Display(Name = "PIN")]
        public int PIN { get; set; }

        [Display(Name = "Tipo/Role")]
        public string? Tipo { get; set; }
    }
}