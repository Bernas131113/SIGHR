namespace SIGHR.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Horario
    {
        public long Id { get; set; }

        [Required]
        public long UtilizadorId { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [Required]
        public DateTime HoraEntrada { get; set; }

        [Required]
        public DateTime HoraSaida { get; set; }

        [Required]
        public DateTime EntradaAlmoco { get; set; }

        [Required]
        public DateTime SaidaAlmoco { get; set; }

        [ForeignKey("UtilizadorId")]
        public Utilizadores Utilizadores { get; set; }
    }

}
