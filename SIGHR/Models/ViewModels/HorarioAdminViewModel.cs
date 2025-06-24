// Models/ViewModels/HorarioAdminViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    public class HorarioAdminViewModel
    {
        public long HorarioId { get; set; }

        [Display(Name = "Utilizador")]
        public string? NomeUtilizador { get; set; }

        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [Display(Name = "Hora Entrada")]
        [DataType(DataType.Time)]
        public TimeSpan HoraEntrada { get; set; }

        [Display(Name = "Saída Almoço")]
        [DataType(DataType.Time)]
        public TimeSpan SaidaAlmoco { get; set; }

        [Display(Name = "Entrada Almoço")]
        [DataType(DataType.Time)]
        public TimeSpan EntradaAlmoco { get; set; }

        [Display(Name = "Saída")]
        [DataType(DataType.Time)]
        public TimeSpan HoraSaida { get; set; }

        [Display(Name = "Total de Horas")]
        public string? TotalHorasTrabalhadas { get; set; }

        [Display(Name = "Localização")] // Se você tiver este campo
        public string? Localizacao { get; set; }
    }
}