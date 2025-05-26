// Models/Horario.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SIGHR.Areas.Identity.Data; // Adicione este using para SIGHRUser

namespace SIGHR.Models
{
    public class Horario
    {
        public long Id { get; set; }

        [Required]
        public required string UtilizadorId { get; set; } // Deve ser string

        [Required]
        public DateTime Data { get; set; }

        [Required]
        public TimeSpan HoraEntrada { get; set; }

        [Required]
        public TimeSpan HoraSaida { get; set; }

        [Required]
        public TimeSpan EntradaAlmoco { get; set; }

        [Required]
        public TimeSpan SaidaAlmoco { get; set; }

        // Propriedade de navegação para o SIGHRUser
        [ForeignKey("UtilizadorId")]
        public virtual SIGHRUser? User { get; set; } // NOME DA PROPRIEDADE DE NAVEGAÇÃO: "User"
    }
}