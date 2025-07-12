// Models/ViewModels/CollaboratorDashboardViewModel.cs
using SIGHR.Models; // Namespace para as entidades Horario e Falta
using System.Collections.Generic;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel que representa os dados necessários para exibir o dashboard do colaborador.
    /// Agrega informações de diferentes entidades (Utilizador, Horario, Falta) num único objeto
    /// para ser facilmente consumido pela View.
    /// </summary>
    public class CollaboratorDashboardViewModel
    {
        /// <summary>
        /// O nome completo do colaborador, para a mensagem de boas-vindas.
        /// </summary>
        public string? NomeCompleto { get; set; }

        /// <summary>
        /// O registo de ponto do dia atual do colaborador.
        /// Pode ser nulo se ainda não houver nenhum registo para o dia.
        /// </summary>
        public Horario? HorarioDeHoje { get; set; }

        /// <summary>
        /// Uma lista com as últimas faltas registadas pelo colaborador.
        /// </summary>
        public List<Falta> UltimasFaltas { get; set; } = new List<Falta>();

        // Pode adicionar aqui outras propriedades que queira exibir no dashboard,
        // como por exemplo, um resumo das últimas encomendas.
    }
}