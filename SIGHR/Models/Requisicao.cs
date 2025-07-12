// Models/Requisicao.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGHR.Models
{
    /// <summary>
    /// Representa um item específico dentro de uma encomenda, funcionando como uma
    /// tabela de junção entre as entidades 'Material' e 'Encomenda'.
    /// Define qual o material e a quantidade requisitada para uma determinada encomenda.
    /// </summary>
    public class Requisicao
    {
        //
        // Bloco: Chave Primária Composta
        // A chave primária desta tabela é a combinação do ID do Material e do ID da Encomenda.
        // A sua configuração é feita via Fluent API no SIGHRContext.
        //

        /// <summary>
        /// Parte da chave primária composta e a chave estrangeira para a entidade `Material`.
        /// </summary>
        public long MaterialId { get; set; }

        /// <summary>
        /// Parte da chave primária composta e a chave estrangeira para a entidade `Encomenda`.
        /// </summary>
        public long EncomendaId { get; set; }

        //
        // Bloco: Propriedades Adicionais
        //

        /// <summary>
        /// A quantidade do material especificado que foi requisitada nesta encomenda.
        /// </summary>
        [Required(ErrorMessage = "A quantidade é obrigatória.")]
        public long Quantidade { get; set; }

        //
        // Bloco: Propriedades de Navegação (Relações)
        //

        /// <summary>
        /// Propriedade de navegação que permite ao Entity Framework carregar
        /// o objeto 'Material' completo associado a esta requisição.
        /// </summary>
        [ForeignKey("MaterialId")]
        public virtual Material? Material { get; set; }

        /// <summary>
        /// Propriedade de navegação que permite ao Entity Framework carregar
        /// o objeto 'Encomenda' completo a que esta requisição pertence.
        /// </summary>
        [ForeignKey("EncomendaId")]
        public virtual Encomenda? Encomenda { get; set; }
    }
}