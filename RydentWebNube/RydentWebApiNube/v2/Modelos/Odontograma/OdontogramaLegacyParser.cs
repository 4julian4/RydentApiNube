using System;
using System.Collections.Generic;
using System.Linq;
using RydentWebApiNube.v2.Modelos.Odontograma;

namespace RydentWebApiNube.v2.Servicios.Odontograma
{
    public static class OdontogramaLegacyParser
    {
        private const int TotalDientes = 52;

        private static readonly int[] MapaDientes =
        {
            18,17,16,15,14,13,12,11,
            21,22,23,24,25,26,27,28,

            55,54,53,52,51,
            61,62,63,64,65,

            85,84,83,82,81,
            71,72,73,74,75,

            48,47,46,45,44,43,42,41,
            31,32,33,34,35,36,37,38
        };

        public static OdontogramaDto Parse(OdontogramaLegacyRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            var dto = new OdontogramaDto
            {
                IdTratamiento = row.IdTratamiento,
                Fecha = row.Fecha,
                Tipo = row.Tipo,
                OrigenLegacy = row.OrigenLegacy,
                Nombre = row.Nombre,
                Observaciones = row.Observaciones,

                Bloqueado = EsOdontogramaInicialBloqueado(row),
                FirmaInicial = row.OrigenLegacy == "DIENTES" ? row.Firma : null
            };

            for (int index = 0; index < TotalDientes; index++)
            {
                var diente = new DienteOdontogramaDto
                {
                    Index = index,
                    Numero = MapaDientes[index]
                };

                LeerCampoPorSuperficie(diente, row.Caries, "CARIES", "Caries", index);
                LeerCampoPorSuperficie(diente, row.Fractura, "FRACTURA", "Fractura", index);
                LeerCampoPorSuperficie(diente, row.Recidiva, "RECIDIVA", "Recidiva", index);
                LeerCampoPorSuperficie(diente, row.Amalgama, "AMALGAMA", "Amalgama", index);
                LeerCampoPorSuperficie(diente, row.AmalgamaDes, "AMALGAMADES", "Amalgama desadaptada", index);
                LeerCampoPorSuperficie(diente, row.Ionomero, "IONOMERO", "Ionómero", index);
                LeerCampoPorSuperficie(diente, row.IonomeroIndicado, "IONOMEROINDICADO", "Ionómero indicado", index);
                LeerCampoPorSuperficie(diente, row.IonomeroDesadaptado, "IONOMERODESADAPTADO", "Ionómero desadaptado", index);
                LeerCampoPorSuperficie(diente, row.Resina, "RESINA", "Resina", index);
                LeerCampoPorSuperficie(diente, row.ResinaIndicada, "RESINAINDICADA", "Resina indicada", index);
                LeerCampoPorSuperficie(diente, row.ResinaDes, "RESINADES", "Resina desadaptada", index);
                LeerCampoPorSuperficie(diente, row.Abrasion, "ABRASION", "Abrasión", index);

                LeerCampoEnfermedad(diente, row.Enfermedad, "ENFERMEDAD", index);
                LeerCampoEnfermedad(diente, row.Enfermedad2, "ENFERMEDAD2", index);
                LeerCampoEnfermedad(diente, row.Enfermedad3, "ENFERMEDAD3", index);

                dto.Dientes.Add(diente);
            }

            dto.Grupos = DetectarGrupos(dto.Dientes);

            return dto;
        }

        private static bool EsOdontogramaInicialBloqueado(OdontogramaLegacyRow row)
        {
            return string.Equals(row.OrigenLegacy, "DIENTES", StringComparison.OrdinalIgnoreCase)
                && row.Firma.HasValue
                && row.Firma.Value > 0;
        }

        private static void LeerCampoPorSuperficie(
            DienteOdontogramaDto diente,
            string? cadenaLegacy,
            string campoOrigen,
            string tipo,
            int index)
        {
            int valor = ObtenerByteLegacy(cadenaLegacy, index);

            // En los campos por superficie, 128 representa vacío.
            // Pero no hacemos "if valor == 128"; simplemente evaluamos bits bajos.
            // Ejemplo: 139 = 128 + 8 + 2 + 1.
            for (int pos = 0; pos <= 6; pos++)
            {
                int bit = 1 << pos;

                if ((valor & bit) == 0)
                    continue;

                diente.Marcas.Add(new MarcaOdontogramaDto
                {
                    Tipo = tipo,
                    Ambito = "superficie",
                    Superficie = ObtenerNombreSuperficie(diente.Numero, pos),
                    Codigo = null,
                    CampoOrigen = campoOrigen,
                    ValorLegacy = valor,
                    IndexDienteLegacy = index,
                    PosicionSuperficieLegacy = pos
                });
            }
        }

        private static void LeerCampoEnfermedad(
            DienteOdontogramaDto diente,
            string? cadenaLegacy,
            string campoOrigen,
            int index)
        {
            int codigo = ObtenerByteLegacy(cadenaLegacy, index);

            if (EsCodigoVacioEnfermedad(codigo))
                return;

            diente.Marcas.Add(new MarcaOdontogramaDto
            {
                Tipo = ObtenerNombreEnfermedad(codigo),
                Ambito = ObtenerAmbitoEnfermedad(codigo),
                Superficie = null,
                Codigo = codigo,
                CampoOrigen = campoOrigen,
                ValorLegacy = codigo,
                IndexDienteLegacy = index,
                PosicionSuperficieLegacy = null
            });
        }

        private static int ObtenerByteLegacy(string? texto, int index)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length <= index)
                return 128;

            char ch = texto[index];

            // Soporta casos vistos:
            // \u0080 = 128
            // \u0081 = 129
            // \u0089 = 137
            // \u008B = 139
            // ÿ      = 255
            return ch & 0xFF;
        }

        private static bool EsCodigoVacioEnfermedad(int codigo)
        {
            return codigo == 0 || codigo == 128 || codigo == 255;
        }

        private static string ObtenerNombreEnfermedad(int codigo)
        {
            switch (codigo)
            {
                case 1: return "Diente ausente clínicamente";
                case 2: return "Zona desdentada reemplazada";
                case 3: return "Diente perdido por trauma";
                case 4: return "Diente sano";
                case 5: return "Sellante en boca";
                case 6: return "Sellante indicado";
                case 7: return "Provisional";
                case 8: return "Implante";
                case 9: return "Puente fijo colocado";
                case 10: return "Obturación metálica colocada";
                case 11: return "Obturación colocada en porcelana";
                case 12: return "Rotaciones y migraciones";
                case 13: return "Diente sin erupcionar";
                case 14: return "Endodoncia buena";
                case 15: return "Endodoncia indicada";
                case 16: return "Pin intraradicular";
                case 17: return "Extracción";
                case 18: return "Provisional indicado";
                case 19: return "Implante indicado";
                case 20: return "Pilar derecho";
                case 21: return "Pilar derecho con póntico";
                case 22: return "Póntico";
                case 23: return "Pilar izquierdo";
                case 24: return "Pilar izquierdo con póntico";
                case 25: return "Extracción indicada";
                case 26: return "Endodoncia desadaptada";
                case 27: return "Diente semi incluido";
                case 28: return "Resto radicular";
                case 29: return "Corona hecha";
                case 30: return "Corona indicada";
                case 31: return "Corona desadaptada";
                case 32: return "Perno desadaptado";
                case 33: return "Perno indicado";
                default: return "Código legacy " + codigo;
            }
        }

        private static string ObtenerAmbitoEnfermedad(int codigo)
        {
            if (EsCodigoPuente(codigo))
                return "grupo";

            // Estos se suelen pintar como símbolo de raíz/conducto/perno.
            if (codigo == 14 || codigo == 15 || codigo == 16 || codigo == 26 || codigo == 32 || codigo == 33)
                return "raiz";

            return "diente";
        }

        private static bool EsCodigoPuente(int codigo)
        {
            return codigo == 9
                || codigo == 20
                || codigo == 21
                || codigo == 22
                || codigo == 23
                || codigo == 24;
        }

        private static string ObtenerNombreSuperficie(int diente, int pos)
        {
            bool esAnterior = EsAnterior(diente);
            bool esSuperior = EsSuperior(diente);
            bool ladoDerecho = EsLadoDerecho(diente);

            switch (pos)
            {
                case 0:
                    return esAnterior ? "Incisal" : "Oclusal";

                case 1:
                    return esSuperior ? "Vestibular" : "Lingual";

                case 2:
                    return ladoDerecho ? "Mesial" : "Distal";

                case 3:
                    return esSuperior ? "Palatino" : "Vestibular";

                case 4:
                    return ladoDerecho ? "Distal" : "Mesial";

                case 5:
                    return esSuperior ? "Vestibular secundaria" : "Lingual secundaria";

                case 6:
                    return esSuperior ? "Palatino secundaria" : "Vestibular secundaria";

                default:
                    return "Superficie " + pos;
            }
        }

        private static bool EsSuperior(int diente)
        {
            return (diente >= 11 && diente <= 28)
                || (diente >= 51 && diente <= 65);
        }

        private static bool EsAnterior(int diente)
        {
            return (diente >= 11 && diente <= 13)
                || (diente >= 21 && diente <= 23)
                || (diente >= 31 && diente <= 33)
                || (diente >= 41 && diente <= 43)
                || (diente >= 51 && diente <= 53)
                || (diente >= 61 && diente <= 63)
                || (diente >= 71 && diente <= 73)
                || (diente >= 81 && diente <= 83);
        }

        private static bool EsLadoDerecho(int diente)
        {
            return (diente >= 11 && diente <= 18)
                || (diente >= 41 && diente <= 48)
                || (diente >= 51 && diente <= 55)
                || (diente >= 81 && diente <= 85);
        }

        private static List<GrupoOdontogramaDto> DetectarGrupos(List<DienteOdontogramaDto> dientes)
        {
            var grupos = new List<GrupoOdontogramaDto>();

            GrupoOdontogramaDto? grupoActual = null;

            foreach (var diente in dientes)
            {
                var marcasGrupo = diente.Marcas
                    .Where(x => x.Codigo.HasValue && EsCodigoPuente(x.Codigo.Value))
                    .ToList();

                if (marcasGrupo.Count == 0)
                {
                    if (grupoActual != null && grupoActual.Dientes.Count > 1)
                        grupos.Add(grupoActual);

                    grupoActual = null;
                    continue;
                }

                if (grupoActual == null)
                {
                    grupoActual = new GrupoOdontogramaDto
                    {
                        Tipo = "Puente",
                        Ambito = "grupo"
                    };
                }

                grupoActual.Dientes.Add(diente.Numero);

                foreach (var marca in marcasGrupo)
                    grupoActual.CodigosLegacy.Add(marca.Codigo.Value);
            }

            if (grupoActual != null && grupoActual.Dientes.Count > 1)
                grupos.Add(grupoActual);

            return grupos;
        }
    }
}