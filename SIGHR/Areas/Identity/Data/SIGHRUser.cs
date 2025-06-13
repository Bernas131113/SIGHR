// Local: Data/SIGHRUser.cs (ou Areas/Identity/Data/SIGHRUser.cs)
using Microsoft.AspNetCore.Identity;
using SIGHR.Models; // Para Horario, Falta, Encomenda
using System.Collections.Generic;

namespace SIGHR.Areas.Identity.Data
{
    public class SIGHRUser : IdentityUser
    {
        // Propriedade PIN original foi removida.
        // PinnedHash armazena o salt + hash do PIN.
        public string? PinnedHash { get; set; }

        public string? Tipo { get; set; } // Ex: "Admin", "Collaborator", "Office"
        public string? NomeCompleto { get; set; }

        // Propriedades de navegação
        public virtual ICollection<Horario> Horarios { get; set; } = new List<Horario>();
        public virtual ICollection<Falta> Faltas { get; set; } = new List<Falta>();
        public virtual ICollection<Encomenda> Encomendas { get; set; } = new List<Encomenda>();
    }
}