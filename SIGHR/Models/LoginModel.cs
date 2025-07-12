using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models
{
    /// <summary>
    /// ViewModel que representa os dados de login (utilizador e PIN) submetidos num formulário.
    /// É utilizado para transferir as credenciais da View para o Controller para validação.
    /// </summary>
    public class LoginModel
    {
        /// <summary>
        /// O nome de utilizador introduzido no formulário.
        /// </summary>
        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [Display(Name = "Nome de Utilizador")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        ///  O PIN numérico introduzido no formulário.
        /// </summary>
        [Required(ErrorMessage = "O PIN é obrigatório.")]
        [DataType(DataType.Password)]
        [Display(Name = "PIN")]
        public int PIN { get; set; }
    }
}