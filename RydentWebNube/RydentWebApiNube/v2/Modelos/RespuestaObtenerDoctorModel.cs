

using RydentWebApiNube.v2.Modelos.DbRydent;

namespace RydentWebApiNube.v2.Modelos
{
    public class RespuestaObtenerDoctorModel
    {
        public TDATOSDOCTORES doctor { set; get; } = new TDATOSDOCTORES();
        public int totalPacientes { set; get; } = 0;
        public bool tieneAlarma { set; get; } = false;
        public bool facturaElectronica { set; get; } = false;
    }
}