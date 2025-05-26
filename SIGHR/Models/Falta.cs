// Models/Falta.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SIGHR.Areas.Identity.Data; // Adicione este using para SIGHRUser

namespace SIGHR.Models
{
    public class Falta
    {
        public long Id { get; set; }

        [Required]
        public required string UtilizadorId { get; set; } // Deve ser string para FK do IdentityUser

        [Required]
        public DateTime Data { get; set; } // Data do registo da falta?

        [Required]
        public DateTime DataFalta { get; set; } // Data em que a falta ocorreu

        [Required]
        public TimeSpan Inicio { get; set; } // Sugestão: Usar TimeSpan

        [Required]
        public TimeSpan Fim { get; set; }    // Sugestão: Usar TimeSpan

        [Required]
        public required string Motivo { get; set; } // Removido '?' por causa do [Required]

        // Propriedade de navegação para o SIGHRUser
        [ForeignKey("UtilizadorId")]
        public virtual SIGHRUser? User { get; set; } // NOME DA PROPRIEDADE DE NAVEGAÇÃO: "User" (ou "SIGHRUser", ou "Utilizador")
                                                     // O 'virtual' é bom para lazy loading, se habilitado.
                                                     // O '?' indica que pode não ser sempre carregada (não que pode ser nula no BD se a FK for required).
    }
}