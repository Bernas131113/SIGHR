// Models/ViewModels/MinhaEncomendaViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para apresentar um resumo de uma encomenda na página "As Minhas Encomendas" do colaborador.
    /// Contém apenas a informação essencial para uma listagem rápida.
    /// </summary>
    public class MinhaEncomendaViewModel
    {
        /// <summary>
        /// O identificador único da encomenda.
        /// </summary>
        public long EncomendaId { get; set; }

        /// <summary>
        /// A data em que a encomenda foi efetuada.
        /// </summary>
        [Display(Name = "Data da Encomenda")]
        [DataType(DataType.Date)]
        public DateTime DataEncomenda { get; set; }

        /// <summary>
        /// Um resumo dos primeiros itens da encomenda, para uma visualização rápida.
        /// </summary>
        [Display(Name = "Descrição Resumida")]
        public string DescricaoResumida { get; set; } = string.Empty;

        /// <summary>
        /// O número total de itens (somatório das quantidades) na encomenda.
        /// </summary>
        [Display(Name = "Nº de Itens")]
        public int QuantidadeTotalItens { get; set; }

        /// <summary>
        /// O estado atual da encomenda (ex: "Pendente", "Enviada").
        /// </summary>
        [Display(Name = "Estado")]
        public string Estado { get; set; } = string.Empty;
    }
}