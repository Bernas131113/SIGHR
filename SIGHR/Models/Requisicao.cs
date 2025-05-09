namespace SIGHR.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Requisicao
    {
        public long MaterialId { get; set; }  // Remover o atributo [Key] aqui
        public long EncomendaId { get; set; } // Remover o atributo [Key] aqui

        [Required]
        public long Quantidade { get; set; }

        [ForeignKey("MaterialId")]
        public Material ?Material { get; set; }

        [ForeignKey("EncomendaId")]
        public Encomenda ?Encomenda { get; set; }
    }
}
