// Models/Encomenda.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SIGHR.Areas.Identity.Data; // Adicione este using para SIGHRUser

namespace SIGHR.Models
{
    public class Encomenda
    {
        public long Id { get; set; }

        [Required]
        public required string UtilizadorId { get; set; } // Deve ser string

        [Required]
        public DateTime Data { get; set; }

        [Required]
        public int Quantidade { get; set; } // Revise a semântica desta propriedade

        [Required]
        public bool EstadoAtual { get; set; }

        // Propriedade de navegação para o SIGHRUser
        [ForeignKey("UtilizadorId")]
        public virtual SIGHRUser? User { get; set; } // NOME DA PROPRIEDADE DE NAVEGAÇÃO: "User"

        public virtual ICollection<Requisicao> Requisicoes { get; set; } = new List<Requisicao>();
    }
}