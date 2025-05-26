using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [Display(Name = "Nome de Utilizador")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O PIN é obrigatório.")]
        [DataType(DataType.Password)]
        [Display(Name = "PIN")]
        public int PIN { get; set; }
    }
}