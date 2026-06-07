# AGENTS.md

## Contexto del proyecto

Proyecto: `RydentWebApiNube`

Ruta esperada:

```text
RydentWebNube/RydentWebApiNube
```

API ASP.NET Core publicada en hosting MyASP/SmarterASP.NET sobre IIS mediante `web.config`.

El repositorio se está organizando para tres objetivos:

```text
1. Manejo seguro de variables de entorno y secretos.
2. web.config versionado para hosting IIS/MyASP.
3. Preparación para despliegue continuo.
```

## Reglas obligatorias para cambios futuros

### 1. Secretos y configuración

No agregar secretos reales al repositorio.

Nunca versionar valores reales de:

```text
CONEXIONDB
JWT_SECRET
OAUTH2_GOOGLE_SECRET
OAUTH2_AZURE_SECRET
Wompi__PrivateKey
Wompi__IntegritySecret
Wompi__EventsSecret
credenciales FTP/WebDeploy
contraseñas de base de datos
```

Los valores reales deben estar en:

```text
- .env local, ignorado por Git
- variables de entorno del Application Pool en MyASP
- GitHub Secrets para pipelines
```

El archivo `.env.example` sí debe versionarse, pero solo con nombres de variables y valores vacíos.

### 2. appsettings.json

`appsettings.json` puede contener valores no sensibles y estables, por ejemplo:

```text
- Logging
- Wompi:Currency
- endpoints públicos fijos de Microsoft/Google
- reglas de sesión si son iguales para todos los ambientes
```

No poner en `appsettings.json` valores que cambien por servidor si se busca reutilizar el mismo artefacto publicado en varios ambientes.

### 3. Variables anidadas

Para secciones de ASP.NET Core usar doble guion bajo:

```text
Jwt__Issuer
Jwt__Audience
OAuth__RedirectURI
OAuthGoogle__RedirectURI
Wompi__BaseUrl
FacturacionApi__BaseUrl
```

No usar `:` en variables de entorno para despliegues Windows/Linux compartidos. Preferir siempre `__`.

### 4. web.config

`web.config` debe quedar versionado junto a `appsettings.json`, en:

```text
RydentWebNube/RydentWebApiNube/web.config
```

No meter secretos en `web.config`.

La configuración base esperada es para despliegue como `.exe`:

```xml
<aspNetCore processPath=".\RydentWebApiNube.exe"
            stdoutLogEnabled="false"
            stdoutLogFile=".\logs\stdout"
            hostingModel="outofprocess" />
```

Si el publish cambia a DLL, ajustar a:

```xml
<aspNetCore processPath="dotnet"
            arguments=".\RydentWebApiNube.dll"
            stdoutLogEnabled="false"
            stdoutLogFile=".\logs\stdout"
            hostingModel="outofprocess" />
```

`stdoutLogEnabled` solo debe ponerse en `true` de forma temporal para diagnóstico de arranque en servidor.

### 5. .gitignore

Mantener estas reglas:

```gitignore
.env
.env.*
!.env.example
!.env.*.example
```

Esto permite versionar plantillas como:

```text
.env.example
.env.server.example
.env.production.example
```

y bloquea archivos reales como:

```text
.env
.env.server
.env.production
.env.local
```

Antes de sugerir commit, verificar que ningún `.env` real aparezca en `git status`.

### 6. Debug en VS Code

Se permite versionar:

```text
.vscode/launch.json
.vscode/tasks.json
.vscode/settings.json
.vscode/extensions.json
```

No guardar secretos en archivos `.vscode`.

El debug local debe usar:

```text
ASPNETCORE_ENVIRONMENT=Development
```

para habilitar Swagger si el `Program.cs` mantiene Swagger condicionado por ambiente.

### 7. Exception handler

Si se usa:

```csharp
app.UseExceptionHandler("/error");
```

debe existir un endpoint `/error`. Evitar dejar un `ExceptionHandlerPath` que produzca 404.

En desarrollo se prefiere:

```csharp
app.UseDeveloperExceptionPage();
```

y en producción:

```csharp
app.UseExceptionHandler("/error");
```

### 8. Despliegue continuo

El objetivo de despliegue continuo es:

```text
GitHub Actions -> dotnet publish -> FTP/WebDeploy a MyASP
```

Las credenciales del hosting deben estar en GitHub Secrets. Nunca en YAML ni en archivos del repo.

Nombres sugeridos:

```text
MYASP_FTP_SERVER
MYASP_FTP_USERNAME
MYASP_FTP_PASSWORD
MYASP_FTP_PATH
```

### 9. Cuidado con .NET 6

El proyecto actualmente apunta a `net6.0`, que ya genera warning de fin de soporte en SDKs recientes. No cambiar el target framework sin validación funcional, pero tener presente que una migración a .NET 8 LTS es una mejora técnica pendiente.

### 10. Estilo de trabajo

Preferir cambios pequeños, revisables y seguros.

Antes de commit:

```powershell
git status -s
git diff --check
```

No mezclar en el mismo commit limpieza de configuración con cambios funcionales grandes.
