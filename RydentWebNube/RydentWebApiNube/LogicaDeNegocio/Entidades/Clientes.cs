using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
    [Table("TClientes")]
    public class Clientes
    {
        [Key]
        public long idCliente { get; set; }
        public string? nombreCliente { get; set; }
        public DateTime? activoHasta { get; set; }
        public string? observacion { get; set; }
    }
}

