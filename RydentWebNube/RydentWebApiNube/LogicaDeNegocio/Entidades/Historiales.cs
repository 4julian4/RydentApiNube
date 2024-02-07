using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
    [Table("THistoriales")]
    public class Historiales
    {
        [Key]
        public long idHistorial { get; set; }
        public long? idCliente { get; set; }
        public long? idUsuario { get; set; }
        public DateTime? fecha { get; set; }
        public DateTime? hora { get; set; }
        public string? observacion { get; set; }

    }
}

