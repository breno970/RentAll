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

      
        [Range(0.01, 10000000, ErrorMessage = "Informe um valor positivo para a diária.") ]
        public float vlitem { get; set; }

        [Range(1, 365, ErrorMessage = "Informe um período entre 1 e 365 dias.") ]
        public int diasAluguel { get; set; }

        [Required(ErrorMessage = "O modelo é obrigatório.")]
        public string modelo { get; set; }


        [Required(ErrorMessage = "O CEP é obrigatório.")]
        [RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "Informe um CEP válido com oito dígitos.") ]
        public string CEP { get; set; }
        public string Rua { get; set; }

        [RegularExpression("^[0-9]*$", ErrorMessage = "Somente números são permitidos.")]
        public string? Numero { get; set; }

        [Required]
        public string Bairro { get; set; }
        [Required]
        public string Cidade { get; set; }
        [Required]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Selecione uma UF válida.")]
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
