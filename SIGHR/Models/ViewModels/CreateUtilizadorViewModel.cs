using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel que representa os dados necessários para criar um novo utilizador.
    /// Contém as propriedades e as regras de validação (Data Annotations) para o formulário de criação.
    /// </summary>
    public class CreateUtilizadorViewModel
    {
        /// <summary>
        /// O nome de utilizador para o login. É um campo obrigatório.
        /// </summary>
        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [Display(Name = "Nome de Utilizador")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// O endereço de email do utilizador. Deve ser um email válido e é obrigatório.
        /// </summary>
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O formato do email é inválido.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// O nome completo do utilizador, para apresentação na interface. Este campo é opcional.
        /// </summary>
        [Display(Name = "Nome Completo")]
        public string? NomeCompleto { get; set; }

        /// <summary>
        /// O PIN de 4 dígitos do utilizador. É um campo numérico obrigatório.
        /// </summary>
        [Required(ErrorMessage = "O PIN é obrigatório.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "O PIN deve conter exatamente 4 números.")]
        [Display(Name = "PIN")]
        public int PIN { get; set; }

        /// <summary>
        /// O tipo ou função (Role) do utilizador no sistema (ex: "Admin", "Collaborator").
        /// </summary>
        [Display(Name = "Tipo/Função")]
        public string? Tipo { get; set; }
    }
}