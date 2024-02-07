using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
    [Table("THistorialDePagos")]
    public class HistorialDePagos
    {
        [Key]
        public long idHistorialDePago { get; set; }
        public long? idCliente { get; set; }
        public DateTime? fechaPago { get; set; }
        public int? diasPago { get; set; }
        public decimal? valorPago { get; set; }
    }
}

