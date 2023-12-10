using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace aluguel.Models
{
    public class Tipo
    {
        public int id { get; set; }
        public string nmtipo { get; set; }
        [ForeignKey("categoriaid")]
        public int categoriaid { get; set; }
        public Categoria Categoria { get; set; }
    }
}
