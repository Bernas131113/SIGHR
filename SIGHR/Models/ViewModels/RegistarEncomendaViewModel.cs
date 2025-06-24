// Models/ViewModels/RegistarEncomendaViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SIGHR.Models.ViewModels
{
    public class ItemRequisicaoViewModel // Este ViewModel é para CADA linha de item no formulário
    {
        [Required(ErrorMessage = "Selecione um material.")]
        [Display(Name = "Material")]
        public string? NomeMaterialOuId { get; set; } // Vai armazenar o Value do SelectList (o nome do material)

        [Required(ErrorMessage = "A quantidade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser pelo menos 1.")]
        [Display(Name = "Quantidade")]
        public int Quantidade { get; set; } = 1; // Valor padrão
    }

    public class RegistarEncomendaViewModel
    {
        [Required(ErrorMessage = "A data da encomenda é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data da Encomenda")]
        public DateTime DataEncomenda { get; set; } = DateTime.Today;

        [Display(Name = "Materiais da Encomenda")]
        [MinLength(1, ErrorMessage = "Adicione pelo menos um material à encomenda.")]
        public List<ItemRequisicaoViewModel> ItensRequisicao { get; set; } = new List<ItemRequisicaoViewModel>();

        // Para popular o dropdown de materiais no formulário
        public SelectList? MateriaisDisponiveis { get; set; } // Será nossa lista fixa

        [StringLength(200)]
        [Display(Name = "Observações / Obra (Opcional)")]
        public string? DescricaoObra { get; set; }
    }
}