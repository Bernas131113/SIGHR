// Models/Falta.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SIGHR.Areas.Identity.Data; // Namespace para a classe SIGHRUser

namespace SIGHR.Models
{
    /// <summary>
    /// Representa o registo de uma falta ou ausência de um utilizador.
    /// </summary>
    public class Falta
    {
        //
        // Bloco: Propriedades Principais da Entidade
        //

        /// <summary>
        /// O identificador único da falta (Chave Primária).
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Chave estrangeira que referencia o ID do utilizador que registou a falta.
        /// </summary>
        [Required(ErrorMessage = "O ID do utilizador é obrigatório.")]
        public required string UtilizadorId { get; set; }

        /// <summary>
        /// A data e hora em que a falta foi registada no sistema.
        /// </summary>
        [Required(ErrorMessage = "A data do registo é obrigatória.")]
        public DateTime Data { get; set; }

        /// <summary>
        /// A data específica em que a ausência ocorreu.
        /// </summary>
        [Required(ErrorMessage = "A data da falta é obrigatória.")]
        public DateTime DataFalta { get; set; }

        /// <summary>
        /// A hora de início da ausência.
        /// </summary>
        [Required(ErrorMessage = "A hora de início é obrigatória.")]
        public TimeSpan Inicio { get; set; }

        /// <summary>
        /// A hora de fim da ausência.
        /// </summary>
        [Required(ErrorMessage = "A hora de fim é obrigatória.")]
        public TimeSpan Fim { get; set; }

        /// <summary>
        /// A justificação ou motivo da falta.
        /// </summary>
        [Required(ErrorMessage = "O motivo é obrigatório.")]
        public required string Motivo { get; set; }

        //
        // Bloco: Propriedade de Navegação (Relação)
        //

        /// <summary>
        /// Propriedade de navegação que permite ao Entity Framework carregar
        /// o objeto SIGHRUser associado a esta falta.
        /// A palavra-chave 'virtual' permite o carregamento preguiçoso (lazy loading), se ativado.
        /// </summary>
        [ForeignKey("UtilizadorId")]
        public virtual SIGHRUser? User { get; set; }
    }
}