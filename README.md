# RydentWebApiNube

API ASP.NET Core para Rydent Nube.

Este repositorio se está preparando para trabajar de forma más ordenada con configuración por ambiente, variables de entorno, `web.config` para hosting IIS/MyASP y despliegue continuo.

## Estructura relevante

El proyecto principal está en:

```text
RydentWebNube/
└── RydentWebApiNube/
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── .env.example
    ├── web.config
    ├── Program.cs
    └── RydentWebApiNube.csproj
```

## 1. Configuración por variables de entorno

La aplicación lee variables de entorno mediante la configuración estándar de ASP.NET Core y una carga opcional de archivo `.env` para desarrollo local.

El patrón recomendado es:

```text
Desarrollo local:
.env

Repositorio:
.env.example

Servidor MyASP:
Application Pool > Environment Variables

Código:
appsettings.json solo para valores no sensibles y valores fijos
```

### Variables principales

El archivo `.env.example` debe mantenerse sin valores reales:

```env
# Database
CONEXIONDB=

# JWT
JWT_SECRET=
Jwt__Issuer=
Jwt__Audience=

# OAuth provider secrets
OAUTH2_GOOGLE_CLIENTID=
OAUTH2_GOOGLE_SECRET=
OAUTH2_AZURE_CLIENTID=
OAUTH2_AZURE_SECRET=

# OAuth redirect URLs
OAuth__RedirectURI=
OAuthGoogle__RedirectURI=

# Wompi
Wompi__BaseUrl=
Wompi__PublicKey=
Wompi__PrivateKey=
Wompi__IntegritySecret=
Wompi__EventsSecret=
Wompi__RedirectUrl=

# External APIs
FacturacionApi__BaseUrl=
```

### Qué debe ir en variables de entorno

Deben mantenerse fuera de Git:

```text
- cadenas de conexión
- secretos JWT
- client secrets de OAuth
- llaves privadas de Wompi
- secretos de integridad/eventos
- URLs que cambian por servidor o ambiente
```

### Qué puede quedar en appsettings.json

Pueden quedar en `appsettings.json` los valores no sensibles y estables:

```text
- Logging
- Wompi:Currency = COP
- endpoints públicos fijos de Microsoft/Google
- reglas generales de sesiones, si no cambian por servidor
```

Ejemplo:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Wompi": {
    "Currency": "COP"
  }
}
```

## 2. web.config para MyASP / IIS

El archivo `web.config` debe versionarse porque es parte de la configuración estructural del hosting IIS/MyASP.

Ejemplo recomendado para despliegue con `.exe`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\RydentWebApiNube.exe"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="outofprocess" />
    </system.webServer>
  </location>
</configuration>
```

### stdoutLogEnabled

`stdoutLogEnabled` debe quedar en `false` normalmente.

Solo cambiar temporalmente a `true` si la aplicación no arranca en el hosting y se necesitan logs de arranque:

```xml
stdoutLogEnabled="true"
```

Después de diagnosticar, devolverlo a:

```xml
stdoutLogEnabled="false"
```

No se deben guardar secretos dentro de `web.config`.

## 3. Despliegue continuo

El despliegue continuo puede hacerse con GitHub Actions publicando la API y subiendo el resultado al hosting por FTP/WebDeploy.

Flujo esperado:

```text
push a main
  ↓
GitHub Actions
  ↓
dotnet restore
dotnet publish -c Release
  ↓
subida al hosting MyASP
  ↓
reinicio/reciclado de aplicación si aplica
```

El pipeline no debe contener secretos en texto plano. Las credenciales de FTP/WebDeploy deben ir en GitHub Secrets.

Ejemplos de nombres de secretos:

```text
MYASP_FTP_SERVER
MYASP_FTP_USERNAME
MYASP_FTP_PASSWORD
MYASP_FTP_PATH
```

## 4. Desarrollo local

Crear el archivo local desde la plantilla:

```powershell
Copy-Item .\RydentWebNube\RydentWebApiNube\.env.example .\RydentWebNube\RydentWebApiNube\.env
```

Editar `.env` con los valores reales locales.

El archivo `.env` está ignorado por Git y no debe subirse.

## 5. Debug en VS Code

Se permite versionar:

```text
.vscode/launch.json
.vscode/tasks.json
.vscode/settings.json
.vscode/extensions.json
```

No se deben guardar secretos en esos archivos.

Para correr en desarrollo, usar:

```text
ASPNETCORE_ENVIRONMENT=Development
```

en `.env` local o en la configuración de debug.

## 6. Validaciones antes de commit

Ejecutar:

```powershell
git status -s
```

Deben aparecer archivos como:

```text
?? RydentWebNube/RydentWebApiNube/.env.example
?? RydentWebNube/RydentWebApiNube/web.config
```

No debe aparecer:

```text
.env
.env.server
.env.production
```

Si aparece un `.env` real, no hacer commit hasta corregir `.gitignore` o removerlo del índice:

```powershell
git rm --cached RydentWebNube/RydentWebApiNube/.env
```

## 7. Nota sobre .NET 6

El proyecto actualmente compila para `net6.0`. Esa versión ya aparece como fuera de soporte en SDKs recientes. La migración a una versión LTS soportada, preferiblemente .NET 8, debe planearse como una mejora posterior.
