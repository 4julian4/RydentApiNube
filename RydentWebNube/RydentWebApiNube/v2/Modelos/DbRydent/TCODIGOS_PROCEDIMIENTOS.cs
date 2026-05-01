using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RydentWebApiNube.v2.Modelos.DbRydent
{
    public class TCODIGOS_PROCEDIMIENTOS
    {
        public int? ID;
        public string? NOMBRE { get; set; }
        public string? CODIGO { get; set; }
        public string? CATEGORIA { get; set; }
        public double? COSTO { get; set; }
        public string? TIPO { get; set; }
        public string? TIPO_RIPS { get; set; }
        public string? INSUMO { get; set; }
        public string? INSUMO_REF { get; set; }
        public int? IVA { get; set; }
    }
   
}