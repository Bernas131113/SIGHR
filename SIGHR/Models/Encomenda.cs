// Models/Encomenda.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SIGHR.Areas.Identity.Data;


namespace SIGHR.Models
{
    public class Encomenda
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "O ID do utilizador é obrigatório.")]
        public required string UtilizadorId { get; set; }

        [Required(ErrorMessage = "A data da encomenda é obrigatória.")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [Required(ErrorMessage = "A quantidade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser pelo menos 1.")]
        public int Quantidade { get; set; } // Ex: número de tipos de itens diferentes

        [Required(ErrorMessage = "O estado da encomenda é obrigatório.")]
        [StringLength(50, ErrorMessage = "O estado não pode exceder 50 caracteres.")]
        public required string Estado { get; set; } = "Pendente"; // Novo campo string

        [StringLength(200)] // Defina um tamanho apropriado
        public string? DescricaoObra { get; set; }

        // public bool EstadoAtual { get; set; } // REMOVA OU COMENTE ESTA LINHA

        [ForeignKey("UtilizadorId")]
        public virtual SIGHRUser? User { get; set; }

        public virtual ICollection<Requisicao> Requisicoes { get; set; } = new List<Requisicao>();
    }
}