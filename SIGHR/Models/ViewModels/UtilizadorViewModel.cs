using System.Collections.Generic;

namespace SIGHR.Models.ViewModels
{
    public class UtilizadorViewModel
    {
        public required string Id { get; set; } // Marcado como required
        public string? UserName { get; set; } // Anulável se pode ser
        public string? Email { get; set; }    // Anulável se pode ser
        public string? NomeCompleto { get; set; }
        public int PIN { get; set; }
        public string? Tipo { get; set; }
        public IList<string>? Roles { get; set; }
    }
}
