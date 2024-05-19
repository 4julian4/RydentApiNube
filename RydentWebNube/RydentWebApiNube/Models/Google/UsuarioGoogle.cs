namespace RydentWebApiNube.Models.Google
{
    public class UsuarioGoogle
    {
        public string sub { get; set; } = "";
        public string picture { get; set; } = "";
        public string email { get; set; } = "";
        public bool email_verified { get; set; } = false;
    }
}
