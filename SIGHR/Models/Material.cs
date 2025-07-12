using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models
{
    /// <summary>
    /// Representa um material que pode ser requisitado numa encomenda.
    /// </summary>
    public class Material
    {
        /// <summary>
        /// O identificador único do material (Chave Primária).
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// A descrição ou nome do material (ex: "Tijolo 11", "Cimento").
        /// </summary>
        [Required(ErrorMessage = "A descrição do material é obrigatória.")]
        public string? Descricao { get; set; }

        /// <summary>
        /// Coleção de todas as requisições associadas a este material.
        /// Representa a relação "Um-para-Muitos" entre Material e Requisicao.
        /// Inicializar a coleção é uma boa prática para evitar NullReferenceException.
        /// </summary>
        public virtual ICollection<Requisicao> Requisicoes { get; set; } = new List<Requisicao>();
    }
}