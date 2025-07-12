// Models/Horario.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SIGHR.Areas.Identity.Data; // Namespace para a classe SIGHRUser

namespace SIGHR.Models
{
    /// <summary>
    /// Representa um registo diário de ponto de um utilizador.
    /// Contém todas as marcações de entrada e saída.
    /// </summary>
    public class Horario
    {
        //
        // Bloco: Propriedades Principais da Entidade
        //

        /// <summary>
        /// O identificador único do registo de horário (Chave Primária).
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Chave estrangeira que referencia o ID do utilizador a quem este registo pertence.
        /// </summary>
        [Required(ErrorMessage = "O ID do utilizador é obrigatório.")]
        public required string UtilizadorId { get; set; }

        /// <summary>
        /// A data a que este registo de ponto se refere.
        /// </summary>
        [Required(ErrorMessage = "A data é obrigatória.")]
        public DateTime Data { get; set; }

        /// <summary>
        /// A hora de início do dia de trabalho.
        /// </summary>
        [Required]
        public TimeSpan HoraEntrada { get; set; }

        /// <summary>
        /// A hora de fim do dia de trabalho.
        /// </summary>
        [Required]
        public TimeSpan HoraSaida { get; set; }

        /// <summary>
        /// A hora de regresso do almoço.
        /// </summary>
        [Required]
        public TimeSpan EntradaAlmoco { get; set; }

        /// <summary>
        /// A hora de saída para o almoço.
        /// </summary>
        [Required]
        public TimeSpan SaidaAlmoco { get; set; }

        //
        // Bloco: Propriedade de Navegação (Relação)
        //

        /// <summary>
        /// Propriedade de navegação que permite ao Entity Framework carregar
        /// o objeto SIGHRUser associado a este registo de horário.
        /// A palavra-chave 'virtual' permite o carregamento preguiçoso (lazy loading), se ativado.
        /// </summary>
        [ForeignKey("UtilizadorId")]
        public virtual SIGHRUser? User { get; set; }
    }
}