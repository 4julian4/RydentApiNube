using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RydentWebApiNube.v2.Modelos.DbRydent
{
    public class TCONFIGURACIONES_RYDENT
    {
        public int ID { get; set; }
        public string? NOMBRE { get; set; }
        public int? PERMISO { get; set; } = 0;
    }
}