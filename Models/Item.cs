using aluguel.ViewModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace aluguel.Models
{
    public class Item
    {
        [Key]
        public int id { get; set; }
        [Required(ErrorMessage = "o nome é obrigatório.")]
        public string nmitem { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string dsitem { get; set; }
        public DateTime dtcriacao { get; set; }
        public string snativo { get; set; }

        [Required(ErrorMessage = "Insira ao menos uma foto.")]
        public string imagem1 { get; set; }
        [AllowNull]
        public string? imagem2 { get; set; }
        [AllowNull]
        public string? imagem3 { get; set; }
        [AllowNull]
        public string? imagem4 { get; set; }
        [AllowNull]
        public string? imagem5 { get; set; }

        //AspNetUsers
        public string iduser { get; set; }
        //Categoria
        [NotMapped]
        public ItemViewModel? itemViewModel { get; set; }

        private List<Item> GetItems()
        {
            List<Item> items = new List<Item>();
            return items;
        }
    }

}
