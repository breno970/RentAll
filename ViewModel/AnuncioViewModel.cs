using aluguel.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace aluguel.ViewModel
{
    public class AnuncioViewModel
    {
        [NotMapped]
        public int id { get; set; }
        public float vlitem { get; set; }
        public int diasAluguel { get; set; }

        //AspNetUsers
        public string iduser { get; set; }

        //Item
        [ForeignKey("itemid")]
        public int itemid { get; set; }

        public Item? item { get; set; }


    }
}