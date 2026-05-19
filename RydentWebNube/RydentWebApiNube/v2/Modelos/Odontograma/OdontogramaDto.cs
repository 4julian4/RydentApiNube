using System;
using System.Collections.Generic;

namespace RydentWebApiNube.v2.Modelos.Odontograma
{
    public class OdontogramaDto
    {
        public int IdTratamiento { get; set; }
        public DateTime Fecha { get; set; }

        // Inicial / Actual
        public string Tipo { get; set; } = string.Empty;

        // DIENTES / DIENTESF
        public string OrigenLegacy { get; set; } = string.Empty;

        public string? Nombre { get; set; }
        public string? Observaciones { get; set; }

        // DIENTES se bloquea cuando FIRMA tiene valor.
        // DIENTESF queda editable durante el tratamiento.
        public bool Bloqueado { get; set; }
        public int? FirmaInicial { get; set; }

        public List<DienteOdontogramaDto> Dientes { get; set; } = new List<DienteOdontogramaDto>();
        public List<GrupoOdontogramaDto> Grupos { get; set; } = new List<GrupoOdontogramaDto>();
    }

    public class DienteOdontogramaDto
    {
        public int Index { get; set; }
        public int Numero { get; set; }

        public List<MarcaOdontogramaDto> Marcas { get; set; } = new List<MarcaOdontogramaDto>();
    }

    public class MarcaOdontogramaDto
    {
        public string Tipo { get; set; } = string.Empty;

        // superficie / diente / raiz / grupo
        public string Ambito { get; set; } = string.Empty;

        // Oclusal, Mesial, Distal, Vestibular, Palatino, Lingual, Incisal...
        public string? Superficie { get; set; }

        // Código legacy para ENFERMEDAD, ENFERMEDAD2, ENFERMEDAD3
        public int? Codigo { get; set; }

        // Campo origen: CARIES, FRACTURA, ENFERMEDAD, etc.
        public string CampoOrigen { get; set; } = string.Empty;

        // Valor exacto leído de la cadena legacy.
        public int ValorLegacy { get; set; }

        // Debug útil mientras validamos contra Delphi
        public int IndexDienteLegacy { get; set; }
        public int? PosicionSuperficieLegacy { get; set; }
    }

    public class GrupoOdontogramaDto
    {
        public string Tipo { get; set; } = string.Empty;
        public string Ambito { get; set; } = "grupo";

        public List<int> Dientes { get; set; } = new List<int>();
        public List<int> CodigosLegacy { get; set; } = new List<int>();
    }
}