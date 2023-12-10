using System.ComponentModel.DataAnnotations;

namespace aluguel.Models
{
    public class Categoria
    {
        [Key]
        public int id { get; set; }
        public string nmcategoria { get; set; }
        public ICollection<Tipo> Tipos { get; set; }
    }
}
