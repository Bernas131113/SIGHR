using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel que representa os dados necessários para editar um utilizador existente.
    /// Contém as propriedades e as regras de validação (Data Annotations) para o formulário de edição.
    /// </summary>
    public class EditUtilizadorViewModel
    {
        /// <summary>
        /// O identificador único (ID) do utilizador a ser editado.
        /// A palavra-chave 'required' garante que esta propriedade não pode ser nula.
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// O nome de utilizador para o login. É um campo obrigatório.
        /// </summary>
        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [Display(Name = "Nome de Utilizador")]
        public required string UserName { get; set; }

        /// <summary>
        /// O endereço de email do utilizador. Deve ser um email válido e é obrigatório.
        /// </summary>
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O formato do email é inválido.")]
        public required string Email { get; set; }

        /// <summary>
        /// O nome completo do utilizador, para apresentação na interface. Este campo é opcional.
        /// </summary>
        [Display(Name = "Nome Completo")]
        public string? NomeCompleto { get; set; }

        /// <summary>
        /// O PIN de 4 dígitos do utilizador. Apenas é atualizado se um novo valor for introduzido.
        /// </summary>
        [Required(ErrorMessage = "É necessário fornecer um PIN.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "O PIN deve conter exatamente 4 números.")]
        [Display(Name = "PIN (introduza um novo para alterar)")]
        public int PIN { get; set; }

        /// <summary>
        /// O tipo ou função (Role) do utilizador no sistema (ex: "Admin", "Collaborator").
        /// </summary>
        [Display(Name = "Tipo/Função")]
        public string? Tipo { get; set; }
    }
}