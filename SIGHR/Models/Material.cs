using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SIGHR.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Material
    {
        public long Id { get; set; }

        [Required]
        public string Descricao { get; set; }

        public ICollection<Requisicao> Requisicoes { get; set; }
    }
}

