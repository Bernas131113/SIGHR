// Models/ViewModels/HorarioColaboradorViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para apresentar os detalhes de um registo de ponto na página "O Meu Registo de Ponto" do colaborador.
    /// É semelhante ao HorarioAdminViewModel, mas focado nos dados do próprio utilizador.
    /// </summary>
    public class HorarioColaboradorViewModel
    {
        /// <summary>
        /// O identificador único do registo de horário.
        /// </summary>
        public long HorarioId { get; set; }

        // O nome do utilizador é omitido, pois esta View mostra sempre os registos do colaborador autenticado.

        /// <summary>
        /// A data do registo de ponto.
        /// </summary>
        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        /// <summary>
        /// A hora de início do trabalho.
        /// </summary>
        [Display(Name = "Entrada")]
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
        public string TotalHorasTrabalhadas { get; set; } = "--:--";

        /// <summary>
        /// A localização do registo, se aplicável.
        /// </summary>
        [Display(Name = "Localização")]
        public string? Localizacao { get; set; }
    }
}