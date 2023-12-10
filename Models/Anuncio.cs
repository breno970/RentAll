using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace aluguel.Models
{
    public class Anuncio
    {
        [Key]
        public int id { get; set; }

      
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Informe um valor válido.")]
        public float vlitem { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Somente números positivos são permitidos.")]
        public int diasAluguel { get; set; }

        [Required(ErrorMessage = "O modelo é obrigatório.")]
        public string modelo { get; set; }


        [Required(ErrorMessage = "O CEP é obrigatório.")]
        [StringLength(9, MinimumLength = 8, ErrorMessage = "O campo CEP deve conter entre 8 e 9 caracteres.")]
        public string CEP { get; set; }
        public string Rua { get; set; }

        [RegularExpression("^[0-9]*$", ErrorMessage = "Somente números são permitidos.")]
        public string? Numero { get; set; }

        [Required]
        public string Bairro { get; set; }
        [Required]
        public string Cidade { get; set; }
        [Required]
        public string Estado { get; set; }
        public string? Complemento { get; set; }

        [Required]
        public string contato { get; set; }

        [Required]
        public string NomeFantasia { get; set; }

        //AspNetUsers
        public string iduser { get; set; }

        //Item
        [ForeignKey("itemid")]
        public int itemid { get; set; }

        public Item? item { get; set; }

        public int categoriaid { get; set; }
        public Categoria? categoria { get; set; }
        //Tipo
        public int tipoid { get; set; }
        public Tipo? tipo { get; set; }

        public List<Anuncio> GetAnuncios()
        {
            List<Anuncio> anuncios = new List<Anuncio>();
            return anuncios;
        }

    }
}
