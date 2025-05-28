// Models/ViewModels/FaltaViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    public class FaltaViewModel
    {
        [Required(ErrorMessage = "A data da falta é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data da Falta")]
        public DateTime DataFalta { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "A hora de início é obrigatória.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de Início")]
        public TimeSpan Inicio { get; set; } // TimeSpan é struct, inicializado com Zero por padrão

        [Required(ErrorMessage = "A hora de fim é obrigatória.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de Fim")]
        public TimeSpan Fim { get; set; }    // TimeSpan é struct

        [Required(ErrorMessage = "O motivo é obrigatório.")] // Validação do formulário
        [StringLength(500, ErrorMessage = "O motivo não pode exceder 500 caracteres.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Motivo")]
        public string Motivo { get; set; } = string.Empty; // <<<< CORRIGIDO AQUI: Removido 'required', inicializado com string.Empty
    }
}