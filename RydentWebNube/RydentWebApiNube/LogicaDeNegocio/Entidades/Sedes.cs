using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
    [Table("TSedes")]
    public class Sedes
    {
        [Key]
        public long idSede { get; set; }
        public long? idCliente { get; set; }
        public string? nombreSede { get; set; }
        public string? identificadorLocal { get; set; }
        public string? observacion { get; set; }
    }
}
