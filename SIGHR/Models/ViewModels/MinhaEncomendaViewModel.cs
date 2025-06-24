// Models/ViewModels/MinhaEncomendaViewModel.cs
using System;
using System.ComponentModel.DataAnnotations; // Para DisplayName, DataType

namespace SIGHR.Models.ViewModels
{
    public class MinhaEncomendaViewModel
    {
        public long EncomendaId { get; set; }

        [Display(Name = "Data da Encomenda")]
        [DataType(DataType.Date)]
        public DateTime DataEncomenda { get; set; }

        [Display(Name = "Descrição Resumida")]
        public string DescricaoResumida { get; set; } = string.Empty; // Inicializado

        [Display(Name = "Nº Itens")]
        public int QuantidadeTotalItens { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; } = string.Empty; // Inicializado
    }
}