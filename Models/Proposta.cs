using System.ComponentModel.DataAnnotations;

namespace aluguel.Models
{
    public class Proposta
    {
        [Key]
        public int id { get; set; }
        public int multiplicador { get; set; }
        public float vltotal { get; set; }
        public string snAceitoProposta { get; set; }

        //AspNetUsers
        public string idlocatario { get; set; }
        public string idlocador { get; set; }
    }

}
