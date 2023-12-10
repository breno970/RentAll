using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aluguel.Models
{
    public class Avaliacao
    {
        [Key]
        public int id { get; set; }
        public int qtestrelas { get; set; }
        public string dscomentario { get; set; }

        //AspNetUsers
        public string idavaliado { get; set; }
        public string idavaliador { get; set; }
        //Item
        [ForeignKey("iditem")]
        public int iditem { get; set; }
        public Item item { get; set; }

        //Anuncio
        [ForeignKey("idanuncio")]
        public int idanuncio { get; set; }
        public Anuncio anuncio { get; set; }
    }
}
