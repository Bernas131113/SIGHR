// Data/SIGHRUser.cs
using Microsoft.AspNetCore.Identity;
using SIGHR.Models; // Para referenciar Horario, Falta, Encomenda
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // Para [PersonalData] se quiser usar

namespace SIGHR.Areas.Identity.Data // Ou SIGHR.Areas.Identity.Data
{
    public class SIGHRUser : IdentityUser // Herda de IdentityUser (que já tem Id, UserName, Email, PasswordHash etc.)
    {
        // Suas propriedades personalizadas
        // [PersonalData] // Marque dados pessoais para conformidade com GDPR se necessário
        public int PIN { get; set; }

        // [PersonalData]
        public string? Tipo { get; set; } // Ex: "Admin", "Colaborador", "Office"

        // [PersonalData]
        public string? NomeCompleto { get; set; } // Para um nome de exibição se diferente do UserName

        // Propriedades de navegação para suas outras entidades
        // Indicam que um SIGHRUser pode ter muitos Horarios, Faltas, Encomendas
        public virtual ICollection<Horario> Horarios { get; set; } = new List<Horario>();
        public virtual ICollection<Falta> Faltas { get; set; } = new List<Falta>();
        public virtual ICollection<Encomenda> Encomendas { get; set; } = new List<Encomenda>();
    }
}