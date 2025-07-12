// Models/ViewModels/UtilizadorViewModel.cs
using System.Collections.Generic;

namespace SIGHR.Models.ViewModels
{
    /// <summary>
    /// ViewModel para apresentar os dados de um utilizador em várias páginas,
    /// como listas, detalhes ou ecrãs de confirmação.
    /// Agrega as propriedades principais de um utilizador para serem facilmente consumidas pela interface.
    /// </summary>
    public class UtilizadorViewModel
    {
        /// <summary>
        /// O identificador único do utilizador (proveniente do ASP.NET Core Identity).
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// O nome de utilizador usado para o login.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// O endereço de email do utilizador.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// O nome completo do utilizador, para apresentação.
        /// </summary>
        public string? NomeCompleto { get; set; }


        /// <summary>
        /// A propriedade personalizada que define o tipo de utilizador (ex: "Admin", "Collaborator").
        /// </summary>
        public string? Tipo { get; set; }

        /// <summary>
        /// A lista de funções (Roles) do ASP.NET Core Identity associadas a este utilizador.
        /// </summary>
        public IList<string>? Roles { get; set; }
    }
}