// Models/ViewModels/HorarioColaboradorViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    public class HorarioColaboradorViewModel
    {
        public long HorarioId { get; set; }

        // NomeUtilizador é opcional aqui, pois são os registos do próprio colaborador
        // public string? NomeUtilizador { get; set; }

        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [Display(Name = "Entrada")]
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
        public string TotalHorasTrabalhadas { get; set; } = "--:--"; // Inicializado

        [Display(Name = "Localização")]
        public string? Localizacao { get; set; } // Se você tiver este campo na entidade Horario
    }
}