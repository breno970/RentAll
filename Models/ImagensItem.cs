using System.ComponentModel.DataAnnotations;

namespace aluguel.Models
{
    public class ImagensItem
    {
        [Key]
        public int id { get; set; }
        public string codimagem { get; set; }
        public string format { get; set; }


    }
}
