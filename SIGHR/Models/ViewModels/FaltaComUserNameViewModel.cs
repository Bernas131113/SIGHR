// Models/ViewModels/FaltaComUserNameViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels // Certifique-se que este namespace está correto
{
    public class FaltaComUserNameViewModel
    {
        public long Id { get; set; }

        [Display(Name = "Utilizador")]
        public string? UserName { get; set; }

        [Display(Name = "Data da Falta")]
        [DataType(DataType.Date)]
        public DateTime DataFalta { get; set; }

        [Display(Name = "Início")]
        [DataType(DataType.Time)]
        public TimeSpan Inicio { get; set; }

        [Display(Name = "Fim")]
        [DataType(DataType.Time)]
        public TimeSpan Fim { get; set; }

        [Display(Name = "Motivo")]
        public required string Motivo { get; set; } = string.Empty; // Ou = null!; ou inicialize no controller

        [Display(Name = "Data do Registo")]
        [DataType(DataType.DateTime)]
        public DateTime DataRegisto { get; set; }
    }
}