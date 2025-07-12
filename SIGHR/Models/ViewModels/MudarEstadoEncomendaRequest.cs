// Models/ViewModels/MudarEstadoEncomendaRequest.cs
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// Modelo de dados (DTO - Data Transfer Object) para receber um pedido da API
    /// para alterar o estado de uma encomenda.
    /// Contém as regras de validação para os dados recebidos no corpo do pedido.
    /// </summary>
    public class MudarEstadoEncomendaRequest
    {
        /// <summary>
        /// O identificador único da encomenda cujo estado será alterado.
        /// </summary>
        [Required(ErrorMessage = "O ID da encomenda é obrigatório.")]
        public long Id { get; set; }

        /// <summary>
        /// O novo estado a ser atribuído à encomenda.
        /// </summary>
        [Required(ErrorMessage = "O novo estado é obrigatório.")]
        [StringLength(50, ErrorMessage = "O estado não pode exceder os 50 caracteres.")]
        public string NovoEstado { get; set; } = string.Empty;
    }
}