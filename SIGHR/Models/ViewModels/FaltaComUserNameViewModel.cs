// Models/ViewModels/FaltaComUserNameViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para apresentar os detalhes de uma falta, incluindo o nome do utilizador associado.
    /// É usado nas listas de faltas (tanto na área do colaborador como na do administrador).
    /// </summary>
    public class FaltaComUserNameViewModel
    {
        /// <summary>
        /// O identificador único da falta.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// O nome do utilizador que registou a falta.
        /// </summary>
        [Display(Name = "Utilizador")]
        public string? UserName { get; set; }

        /// <summary>
        /// A data em que a ausência ocorreu.
        /// </summary>
        [Display(Name = "Data da Falta")]
        [DataType(DataType.Date)]
        public DateTime DataFalta { get; set; }

        /// <summary>
        /// A hora de início da ausência.
        /// </summary>
        [Display(Name = "Início")]
        [DataType(DataType.Time)]
        public TimeSpan Inicio { get; set; }

        /// <summary>
        /// A hora de fim da ausência.
        /// </summary>
        [Display(Name = "Fim")]
        [DataType(DataType.Time)]
        public TimeSpan Fim { get; set; }

        /// <summary>
        /// A justificação ou motivo da falta.
        /// A palavra-chave 'required' indica que esta propriedade não pode ser nula.
        /// </summary>
        [Display(Name = "Motivo")]
        public required string Motivo { get; set; } = string.Empty;

        /// <summary>
        /// A data e hora em que a falta foi registada no sistema.
        /// </summary>
        [Display(Name = "Data do Registo")]
        [DataType(DataType.DateTime)]
        public DateTime DataRegisto { get; set; }
    }
}