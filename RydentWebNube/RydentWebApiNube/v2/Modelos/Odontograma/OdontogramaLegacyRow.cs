using System;

namespace RydentWebApiNube.v2.Modelos.Odontograma
{
    public class OdontogramaLegacyRow
    {
        public int IdTratamiento { get; set; }
        public DateTime Fecha { get; set; }

        // Inicial / Actual
        public string Tipo { get; set; } = string.Empty;

        // DIENTES / DIENTESF
        public string OrigenLegacy { get; set; } = string.Empty;

        public string? Nombre { get; set; }
        public string? Caries { get; set; }
        public string? Enfermedad { get; set; }
        public string? Recidiva { get; set; }
        public string? Amalgama { get; set; }
        public string? Ionomero { get; set; }
        public string? Enfermedad2 { get; set; }
        public string? Enfermedad3 { get; set; }
        public string? Observaciones { get; set; }
        public string? Fractura { get; set; }
        public string? AmalgamaDes { get; set; }
        public string? ResinaDes { get; set; }
        public string? Resina { get; set; }
        public string? ResinaIndicada { get; set; }
        public string? IonomeroIndicado { get; set; }
        public string? IonomeroDesadaptado { get; set; }
        public string? Abrasion { get; set; }

        // Solo DIENTES
        public int? Firma { get; set; }
    }
}