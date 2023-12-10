using aluguel.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace aluguel.ViewModel
{
    public class ItemViewModel
    {
        [NotMapped]
        public string nmitem { get; set; }
        public string dsitem { get; set; }
        public DateTime dtcriacao { get; set; }
        public string modelo { get; set; }
        public string snativo { get; set; }
        public IFormFile imagem1 { get; set; }
        public IFormFile? imagem2 { get; set; }
        public IFormFile? imagem3 { get; set; }
        public IFormFile? imagem4 { get; set; }
        public IFormFile? imagem5 { get; set; }

        //AspNetUsers
        public string iduser { get; set; }


        //Categoria
        public int categoriaid { get; set; }
        public Categoria? categoria { get; set; }
        //Tipo
        public int tipoid { get; set; }
        public Tipo? tipo { get; set; }
    }
}
