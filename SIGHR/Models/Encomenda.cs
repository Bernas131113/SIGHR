// Models/Encomenda.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SIGHR.Areas.Identity.Data;

namespace SIGHR.Models
{
    /// <summary>
    /// Representa uma encomenda efetuada por um utilizador.
    /// Contém os dados gerais da encomenda e as relações com o utilizador e os itens requisitados.
    /// </summary>
    public class Encomenda
    {
        //
        // Bloco: Propriedades Principais da Entidade
        //

        /// <summary>
        /// O identificador único da encomenda (Chave Primária).
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Chave estrangeira que referencia o ID do utilizador que efetuou a encomenda.
        /// </summary>
        [Required(ErrorMessage = "O ID do utilizador é obrigatório.")]
        public required string UtilizadorId { get; set; }

        /// <summary>
        /// A data em que a encomenda foi criada.
        /// </summary>
        [Required(ErrorMessage = "A data da encomenda é obrigatória.")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        /// <summary>
        /// Representa o número de *tipos de item* distintos na encomenda.
        /// (Ex: uma encomenda com 10 tijolos e 5 sacos de cimento tem uma Quantidade de 2).
        /// </summary>
        [Required(ErrorMessage = "A quantidade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser, no mínimo, 1.")]
        public int Quantidade { get; set; }

        /// <summary>
        /// O estado atual da encomenda (ex: "Pendente", "Enviada", "Entregue").
        /// </summary>
        [Required(ErrorMessage = "O estado da encomenda é obrigatório.")]
        [StringLength(50, ErrorMessage = "O estado não pode exceder os 50 caracteres.")]
        public required string Estado { get; set; } = "Pendente"; // Valor padrão para novas encomendas.

        /// <summary>
        /// Um campo opcional para adicionar observações ou identificar a obra associada.
        /// </summary>
        [StringLength(200, ErrorMessage = "A descrição não pode exceder os 200 caracteres.")]
        public string? DescricaoObra { get; set; }

        // A propriedade 'EstadoAtual' (bool) foi corretamente substituída pela propriedade 'Estado' (string)
        // para permitir um maior controlo e mais estados possíveis.
        // public bool EstadoAtual { get; set; }

        //
        // Bloco: Propriedades de Navegação (Relações)
        //

        /// <summary>
        /// Propriedade de navegação que permite ao Entity Framework carregar
        /// o objeto SIGHRUser associado a esta encomenda.
        /// </summary>
        [ForeignKey("UtilizadorId")]
        public virtual SIGHRUser? User { get; set; }

        /// <summary>
        /// Coleção de todos os itens de requisição que pertencem a esta encomenda.
        /// Representa a relação "Um-para-Muitos" entre Encomenda e Requisicao.
        /// </summary>
        public virtual ICollection<Requisicao> Requisicoes { get; set; } = new List<Requisicao>();
    }
}