// Models/ViewModels/RegistarEncomendaViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para representar uma única linha de item no formulário de registo de encomenda.
    /// </summary>
    public class ItemRequisicaoViewModel
    {
        /// <summary>
        /// O nome do material selecionado no dropdown.
        /// </summary>
        [Required(ErrorMessage = "Selecione um material.")]
        [Display(Name = "Material")]
        public string? NomeMaterialOuId { get; set; }

        /// <summary>
        /// A quantidade desejada para o material selecionado.
        /// </summary>
        [Required(ErrorMessage = "A quantidade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser, no mínimo, 1.")]
        [Display(Name = "Quantidade")]
        public int Quantidade { get; set; } = 1; // Valor padrão inicial.
    }

    /// <summary>
    /// ViewModel principal que representa os dados necessários para o formulário de registo de uma nova encomenda.
    /// Contém os dados gerais da encomenda e uma lista de itens.
    /// </summary>
    public class RegistarEncomendaViewModel
    {
        /// <summary>
        /// A data em que a encomenda está a ser efetuada.
        /// </summary>
        [Required(ErrorMessage = "A data da encomenda é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data da Encomenda")]
        public DateTime DataEncomenda { get; set; } = DateTime.Today;

        /// <summary>
        /// Lista de todos os itens (material e quantidade) que compõem a encomenda.
        /// </summary>
        [Display(Name = "Materiais da Encomenda")]
        [MinLength(1, ErrorMessage = "Adicione pelo menos um material à encomenda.")]
        public List<ItemRequisicaoViewModel> ItensRequisicao { get; set; } = new List<ItemRequisicaoViewModel>();

        /// <summary>
        /// Utilizado para popular a lista de opções (dropdown) de materiais disponíveis no formulário.
        /// </summary>
        public SelectList? MateriaisDisponiveis { get; set; }

        /// <summary>
        /// Um campo opcional para adicionar observações ou identificar a obra a que a encomenda se destina.
        /// </summary>
        [StringLength(200, ErrorMessage = "A descrição não pode exceder os 200 caracteres.")]
        [Display(Name = "Observações / Obra (Opcional)")]
        public string? DescricaoObra { get; set; }
    }
}