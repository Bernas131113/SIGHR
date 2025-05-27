// Models/ViewModels/CollaboratorDashboardViewModel.cs
using SIGHR.Models; // Para Horario, Falta
using System.Collections.Generic;

namespace SIGHR.Models.ViewModels
{
    public class CollaboratorDashboardViewModel
    {
        public string? NomeCompleto { get; set; }
        public Horario? HorarioDeHoje { get; set; }
        public List<Falta> UltimasFaltas { get; set; } = new List<Falta>();
        // Adicione outras propriedades que você queira exibir
    }
}