namespace SIGHR.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Utilizadores
    {
        public long Id { get; set; }

        [Required]
        public string Nome { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public int PIN { get; set; }

        [Required]
        public string Tipo { get; set; }

        public ICollection<Horario> Horarios { get; set; }
        public ICollection<Falta> Faltas { get; set; }
        public ICollection<Encomenda> Encomendas { get; set; }
    }
}
