namespace SIGHR.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Encomenda
    {
        public long Id { get; set; }

        [Required]
        public long UtilizadorId { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [Required]
        public int Quantidade { get; set; }

        [Required]
        public bool EstadoAtual { get; set; }

        [ForeignKey("UtilizadorId")]
        public Utilizadores Utilizadores { get; set; }

        public ICollection<Requisicao> Requisicoes { get; set; }
    }
}
