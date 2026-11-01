# Authorization.Server

Servidor de autorización OAuth 2.0/OpenID Connect para la solución. Usa ASP.NET Core Identity, OpenIddict y Google como proveedor externo.

## Configuración

La configuración común está en `appsettings.json`. Los valores de Development están en
`appsettings.Development.json`; las credenciales se mantienen en User Secrets:

```powershell
dotnet user-secrets --project src/Authorization/Authorization.Server/Authorization.Server.csproj set "Authorization:GoogleClientId" "..."
dotnet user-secrets --project src/Authorization/Authorization.Server/Authorization.Server.csproj set "Authorization:GoogleClientSecret" "..."
```

En Google Cloud Console registrar `https://localhost:7000/signin-google` como URI de redirección autorizada.

En Production proporcionar mediante variables de entorno o un proveedor de secretos:

- `ConnectionStrings__DefaultConnection`
- `Authorization__Issuer`
- `Authorization__Audience`
- `Authorization__ClientRedirectUri`
- `Authorization__GoogleClientId`
- `Authorization__GoogleClientSecret`
- `Authorization__GoogleRedirectUri`
- `Authorization__GoogleCallbackPath`
- `Authorization__SigningCertificatePath`
- `Authorization__SigningCertificatePassword`
- `Authorization__EncryptionCertificatePath`
- `Authorization__EncryptionCertificatePassword`

El issuer y las URI deben ser HTTPS en Production. Los certificados PFX de signing y encryption
deben existir, contener clave privada y ser diferentes de los certificados de desarrollo.

La base `AuthorizationDb` se crea automáticamente solo en Development. En Production el esquema
debe estar provisionado antes de iniciar la aplicación; el servidor únicamente registra o actualiza
el cliente `availability-web` durante el arranque.

## Endpoints

- `GET/POST /auth/login`: login local.
- `GET /auth/google`: inicia el login de Google.
- `GET /connect/authorize`: autorización OAuth con PKCE.
- `POST /connect/token`: intercambio del código por access/identity tokens y refresh tokens.
- `GET/POST /connect/logout`: cierre de sesión.
- `GET /api/auth/validate`: valida el bearer token enviado en `Authorization`.

La autoridad y la audiencia/recurso se obtienen de `Authorization:Issuer` y
`Authorization:Audience`; no están acopladas al código.
