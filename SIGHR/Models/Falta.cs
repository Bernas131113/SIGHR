namespace SIGHR.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Falta
    {
        public long Id { get; set; }

        [Required]
        public long UtilizadorId { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [Required]
        public DateTime DataFalta { get; set; }

        [Required]
        public DateTime Inicio { get; set; }

        [Required]
        public DateTime Fim { get; set; }

        [Required]
        public string Motivo { get; set; }

        [ForeignKey("UtilizadorId")]
        public Utilizadores Utilizadores { get; set; }
    }
}
