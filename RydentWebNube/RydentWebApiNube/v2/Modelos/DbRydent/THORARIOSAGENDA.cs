using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RydentWebApiNube.v2.Modelos.DbRydent
{
    public class THORARIOSAGENDA
    {
        public int SILLA { set; get; }
        public int? HORAINICIO { set; get; }
        public int? MINUTOINICIO { set; get; }
        public int? MIN1 { set; get; }
        public int? MIN2 { set; get; }
        public int? MIN3 { set; get; }
        public int? MIN4 { set; get; }
        public int? MIN5 { set; get; }
        public int? MIN6 { set; get; }
        public int? INTERVALO { set; get; }
        public string? TIPOAGENDA { set; get; }
        public string? HORAFINAL { set; get; }
        public string? BLOQDIA { set; get; }
        public string? HORAINICIAL { set; get; }
        public string? RESTRICCIONES { set; get; }
        public string? DESCRIPCION { set; get; }
        public string? NUEVO { set; get; }
        public int? PICO { set; get; }
        public string? CORREO { set; get; }
    }
}
