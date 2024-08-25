using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using RydentWebApiNube.Models.MSN;
using RydentWebApiNube.Models.Google;
using System.Net.Http;

namespace RydentWebApiNube.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IUsuariosServicios iUsuariosServicios;
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly string AuthCodeEndPoint;
        private readonly string TokenEndPoint;
        private readonly string ClientId;
        private readonly string Secret;
        private readonly string Scope;
        private readonly string RedirectURI;
        private readonly string API_EndPoint;


        private readonly string GoogleTokenEndPoint;
        private readonly string GoogleClientId;
        private readonly string GoogleSecret;
        private readonly string GoogleRedirectURI;
        private readonly string GoogleAPI_EndPoint;
        public AuthController(
            IConfiguration configuration,
            IUsuariosServicios iUsuariosServicios
            )
        {
            this.configuration = configuration;
            this.iUsuariosServicios = iUsuariosServicios;
            this.AuthCodeEndPoint = configuration["OAuth:AuthCodeEndPoint"] ?? "";
            this.TokenEndPoint = configuration["OAuth:TokenEndPoint"] ?? "";
            this.ClientId = configuration["OAUTH2_AZURE_CLIENTID"] ?? "";
            this.Secret = configuration["OAUTH2_AZURE_SECRET"] ?? "";
            this.Scope = configuration["OAuth:Scope"] ?? "";
            this.RedirectURI = configuration["OAuth:RedirectURI"] ?? "";
            this.API_EndPoint = configuration["OAuth:API_EndPoint"] ?? "";


            this.GoogleTokenEndPoint = configuration["OAuthGoogle:TokenEndPoint"] ?? "";
            this.GoogleClientId = configuration["OAUTH2_GOOGLE_CLIENTID"] ?? "";
            this.GoogleSecret = configuration["OAUTH2_GOOGLE_SECRET"] ?? "";
            this.GoogleRedirectURI = configuration["OAuthGoogle:RedirectURI"] ?? "";
            this.GoogleAPI_EndPoint = configuration["OAuthGoogle:API_EndPoint"] ?? "";
        }
        [HttpGet("getcode")]
        public IActionResult GetCode()
        {
            string URL = $"{this.AuthCodeEndPoint}?" +
                $"response_type=code&" +
                $"client_id={ClientId}&" +
                $"Redirect_uri={RedirectURI}&" +
                $"scope={Scope}&" +
                $"state=1234567890";
            return Redirect(URL);
        }

        [HttpPost("")]
        public async Task<IActionResult> Autenticar([FromBody] loginRequest modelo)
        {
            string grant_type = "authorization_code";

            //Dictionary<string, string> BodyData = new Dictionary<string, string>()
            var BodyData = new Dictionary<string, string>
            {
                { "grant_type", grant_type },
                { "code", modelo.code },
                { "Redirect_uri", this.RedirectURI },
                { "client_id", this.ClientId },
                { "client_secret", this.Secret },
                { "scope", this.Scope }
            };
            //HttpClient client = new HttpClient();
            var body = new FormUrlEncodedContent(BodyData);
            var response = await httpClient.PostAsync(TokenEndPoint, body).ConfigureAwait(false);
            var status = $"{(int)response.StatusCode} {response.ReasonPhrase}";

            var jsonContent = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var prettyJson = JsonSerializer.Serialize(jsonContent, new JsonSerializerOptions { WriteIndented = true });

            var accessToken = jsonContent.GetProperty("access_token").GetString();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response1 = await httpClient.GetAsync(API_EndPoint).ConfigureAwait(false);
            dynamic ojJSON = new ExpandoObject();
            ojJSON.respuesta = "";
            ojJSON.autenticado = false;

            if (response1.IsSuccessStatusCode)
            {
                var usrMSNAzure = await response1.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jsUsuarioMSN = JsonSerializer.Deserialize<UsuarioMSN>(usrMSNAzure);

                status = $"{(int)response1.StatusCode} {response1.ReasonPhrase}";
                if (!string.IsNullOrEmpty(jsUsuarioMSN?.id))
                {
                    var usuario = await iUsuariosServicios.ConsultarPorCodigoExterno(jsUsuarioMSN.id).ConfigureAwait(false);
                    var respuesta = generateJwtToken(usuario);
                    ojJSON.respuesta = respuesta;
                    ojJSON.autenticado = true;
                    return Ok(ojJSON);
                }
                else return Ok(ojJSON);
            }
            else
            {
                return Ok(ojJSON);
            }
        }
        [HttpGet("prueba/{id}")]
        public IActionResult Prueba(string id)
        {
            string grant_type = "authorization_code";
            string stringURI = new Uri(this.GoogleRedirectURI).ToString();
            var BodyData = new Dictionary<string, string>
            {
                { "code", "" },
                { "client_id", this.GoogleClientId },
                { "client_secret", this.GoogleSecret },
                { "redirect_uri", stringURI },
                { "grant_type", grant_type }
            };
            return Ok(id == "123" ? BodyData : "");
        }

        [HttpPost("authgoogle")]
        public async Task<IActionResult> AutenticarGoogle([FromBody] loginRequest modelo)
        {
            string grant_type = "authorization_code";
            string stringURI = new Uri(this.GoogleRedirectURI).ToString();
            var BodyData = new Dictionary<string, string>
            {
                { "code", modelo.code },
                { "client_id", this.GoogleClientId },
                { "client_secret", this.GoogleSecret },
                { "redirect_uri", stringURI },
                { "grant_type", grant_type }
            };

            var body = new FormUrlEncodedContent(BodyData);
            var response = await httpClient.PostAsync(this.GoogleTokenEndPoint, body).ConfigureAwait(false);
            var status = $"{(int)response.StatusCode} {response.ReasonPhrase}";

            var jsonContent = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var prettyJson = JsonSerializer.Serialize(jsonContent, new JsonSerializerOptions { WriteIndented = true });

            var accessToken = jsonContent.GetProperty("access_token").GetString();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            string DatosUsuarioGoogle = this.GoogleAPI_EndPoint + accessToken;

            var response1 = await httpClient.GetAsync(DatosUsuarioGoogle).ConfigureAwait(false);
            dynamic ojJSON = new ExpandoObject();
            ojJSON.respuesta = "";
            ojJSON.autenticado = false;

            if (response1.IsSuccessStatusCode)
            {
                var s = await response1.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jsUsuarioGoogle = JsonSerializer.Deserialize<UsuarioGoogle>(s);

                status = $"{(int)response1.StatusCode} {response1.ReasonPhrase}";
                if (!string.IsNullOrEmpty(jsUsuarioGoogle?.email))
                {
                    var usuario = await iUsuariosServicios.ConsultarPorCorreo(jsUsuarioGoogle.email).ConfigureAwait(false);
                    var respuesta = generateJwtToken(usuario);
                    ojJSON.respuesta = respuesta;
                    ojJSON.autenticado = true;
                    return Ok(ojJSON);
                }
                else return Ok(ojJSON);
            }
            else
            {
                return Ok(ojJSON);
            }
        }

        private string generateJwtToken(Usuarios user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["JWT_SECRET"] ?? "");
            var lstClaims = new List<Claim>
            {
                new Claim("id", user.idUsuario.ToString()),
                new Claim("idCliente", user.idCliente.ToString()),
                new Claim("correo", user.correoUsuario.ToString())
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(lstClaims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = configuration["Jwt:Issuer"] ?? "",
                Audience = configuration["JWT_SECRET"] ?? "",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [HttpGet("login-callback")]
        public async Task<IActionResult> GetCallback([FromQuery] string code, string state)
        {
            string grant_type = "authorization_code";

            var BodyData = new Dictionary<string, string>
            {
                { "grant_type", grant_type },
                { "code", code },
                { "Redirect_uri", this.RedirectURI },
                { "client_id", this.ClientId },
                { "client_secret", this.Secret },
                { "scope", this.Scope }
            };

            var body = new FormUrlEncodedContent(BodyData);
            var response = await httpClient.PostAsync(TokenEndPoint, body).ConfigureAwait(false);
            var status = $"{(int)response.StatusCode} {response.ReasonPhrase}";

            var jsonContent = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var prettyJson = JsonSerializer.Serialize(jsonContent, new JsonSerializerOptions { WriteIndented = true });

            var accessToken = jsonContent.GetProperty("access_token").GetString();
            var jwtToken = new JwtSecurityToken(accessToken);
            var ss = jwtToken.Subject;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response1 = await httpClient.GetAsync(API_EndPoint).ConfigureAwait(false);
            if (response1.IsSuccessStatusCode)
            {
                var resultado = await response1.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jsonRes = JsonSerializer.Deserialize<JsonElement>(resultado, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                status = $"{(int)response1.StatusCode} {response1.ReasonPhrase}";
                return Ok($"{status + Environment.NewLine}{jsonRes + Environment.NewLine}{response1.IsSuccessStatusCode}");
            }
            else
            {
                return Ok($"{status + Environment.NewLine}{prettyJson + Environment.NewLine}{response.IsSuccessStatusCode}");
            }
        }
    }
}

