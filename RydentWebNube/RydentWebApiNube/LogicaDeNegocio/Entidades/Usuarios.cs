using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
    [Table("TUsuarios")]
    public class Usuarios
    {
        [Key]
        public long idUsuario { get; set; }
        public long? idCliente { get; set; }
        public string? nombreUsuario { get; set; }
        public string? correoUsuario { get; set; }
        public bool? estado { get; set; }
    }
}
