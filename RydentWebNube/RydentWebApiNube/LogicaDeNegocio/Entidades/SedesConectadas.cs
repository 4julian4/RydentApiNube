using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
    [Table("TSedesConectadas")]
    public class SedesConectadas
    {
        [Key]
        public long idSedeConectada { get; set; }
        public long? idCliente { get; set; }
        public long? idSede { get; set; }
        public string? idActualSignalR { get; set; }
        public DateTime? fechaUltimoAcceso { get; set; }
        public bool? activo { get; set; }
    }
}
