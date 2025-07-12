// Models/ViewModels/HorarioAdminViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para apresentar os detalhes de um registo de ponto na área de administração.
    /// Inclui o nome do utilizador e o cálculo das horas trabalhadas para uma fácil visualização.
    /// </summary>
    public class HorarioAdminViewModel
    {
        /// <summary>
        /// O identificador único do registo de horário.
        /// </summary>
        public long HorarioId { get; set; }

        /// <summary>
        /// O nome do utilizador a quem o registo de ponto pertence.
        /// </summary>
        [Display(Name = "Utilizador")]
        public string? NomeUtilizador { get; set; }

        /// <summary>
        /// A data do registo de ponto.
        /// </summary>
        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        /// <summary>
        /// A hora de início do trabalho.
        /// </summary>
        [Display(Name = "Hora de Entrada")]
        [DataType(DataType.Time)]
        public TimeSpan HoraEntrada { get; set; }

        /// <summary>
        /// A hora de saída para o almoço.
        /// </summary>
        [Display(Name = "Saída Almoço")]
        [DataType(DataType.Time)]
        public TimeSpan SaidaAlmoco { get; set; }

        /// <summary>
        /// A hora de regresso do almoço.
        /// </summary>
        [Display(Name = "Entrada Almoço")]
        [DataType(DataType.Time)]
        public TimeSpan EntradaAlmoco { get; set; }

        /// <summary>
        /// A hora de fim do trabalho.
        /// </summary>
        [Display(Name = "Saída")]
        [DataType(DataType.Time)]
        public TimeSpan HoraSaida { get; set; }

        /// <summary>
        /// O total de horas trabalhadas, já formatado como uma string (ex: "08:30").
        /// </summary>
        [Display(Name = "Total de Horas")]
        public string? TotalHorasTrabalhadas { get; set; }

        /// <summary>
        /// A localização do registo, se aplicável.
        /// </summary>
        [Display(Name = "Localização")]
        public string? Localizacao { get; set; }
    }
}