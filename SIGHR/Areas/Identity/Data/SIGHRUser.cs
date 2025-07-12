// Local: Data/SIGHRUser.cs (ou Areas/Identity/Data/SIGHRUser.cs)
using Microsoft.AspNetCore.Identity;
using SIGHR.Models; // Namespace para as entidades Horario, Falta, Encomenda
using System.Collections.Generic;

namespace SIGHR.Areas.Identity.Data
{
    /// <summary>
    /// Representa um utilizador da aplicação.
    /// Esta classe estende a classe IdentityUser padrão do ASP.NET Core Identity,
    /// adicionando propriedades personalizadas.
    /// </summary>
    public class SIGHRUser : IdentityUser
    {
        //
        // Bloco: Propriedades Personalizadas do Utilizador
        // Campos adicionais que serão armazenados na tabela de utilizadores (AspNetUsers).
        //

        /// <summary>
        /// Armazena a versão "hashed" (codificada) e "salted" (com sal) do PIN do utilizador.
        /// O PIN original nunca é guardado diretamente por razões de segurança.
        /// </summary>
        public string? PinnedHash { get; set; }

        /// <summary>
        /// Define o tipo ou a função principal do utilizador no sistema (ex: "Admin", "Collaborator").
        /// </summary>
        public string? Tipo { get; set; }

        /// <summary>
        /// O nome completo do utilizador, para apresentação na interface.
        /// </summary>
        public string? NomeCompleto { get; set; }


        //
        // Bloco: Propriedades de Navegação (Relações)
        // Definem as relações "Um-para-Muitos" com outras entidades.
        //

        /// <summary>
        /// Coleção de todos os registos de horário associados a este utilizador.
        /// </summary>
        public virtual ICollection<Horario> Horarios { get; set; } = new List<Horario>();

        /// <summary>
        /// Coleção de todas as faltas registadas por este utilizador.
        /// </summary>
        public virtual ICollection<Falta> Faltas { get; set; } = new List<Falta>();

        /// <summary>
        /// Coleção de todas as encomendas efetuadas por este utilizador.
        /// </summary>
        public virtual ICollection<Encomenda> Encomendas { get; set; } = new List<Encomenda>();
    }
}