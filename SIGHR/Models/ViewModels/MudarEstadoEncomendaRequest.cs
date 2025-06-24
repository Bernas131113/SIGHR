// Models/ViewModels/MudarEstadoEncomendaRequest.cs
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    public class MudarEstadoEncomendaRequest
    {
        [Required]
        public long Id { get; set; }

        [Required(ErrorMessage = "O novo estado é obrigatório.")]
        [StringLength(50, ErrorMessage = "O estado não pode exceder 50 caracteres.")]
        public string NovoEstado { get; set; } = string.Empty;
    }
}