// Models/ViewModels/FaltaViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel que representa os dados necessários para registar uma nova falta.
    /// Contém as propriedades e as regras de validação (Data Annotations) para o formulário de registo.
    /// </summary>
    public class FaltaViewModel
    {
        /// <summary>
        /// A data em que a ausência ocorreu. É um campo obrigatório.
        /// </summary>
        [Required(ErrorMessage = "A data da falta é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data da Falta")]
        public DateTime DataFalta { get; set; } = DateTime.Today;

        /// <summary>
        /// A hora de início da ausência. É um campo obrigatório.
        /// </summary>
        [Required(ErrorMessage = "A hora de início é obrigatória.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de Início")]
        public TimeSpan Inicio { get; set; } // Sendo um 'struct', o valor padrão é TimeSpan.Zero.

        /// <summary>
        /// A hora de fim da ausência. É um campo obrigatório.
        /// </summary>
        [Required(ErrorMessage = "A hora de fim é obrigatória.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de Fim")]
        public TimeSpan Fim { get; set; }

        /// <summary>
        /// A justificação ou motivo da falta. É um campo de texto obrigatório.
        /// </summary>
        [Required(ErrorMessage = "O motivo é obrigatório.")]
        [StringLength(500, ErrorMessage = "O motivo não pode exceder os 500 caracteres.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Motivo")]
        public string Motivo { get; set; } = string.Empty;
    }
}