# Backend — Correcciones y mejoras aplicadas

Este documento resume los cambios aplicados al backend (`booking.api`, `booking.core`,
`booking.infrastructure`) a partir de una revisión de arquitectura (Clean Architecture) y
buenas prácticas. No se tocó el frontend en esta pasada.

## 1. Arquitectura — inversión de dependencias

**Problema:** las interfaces de los servicios (`IAuthService`, `IBookingService`,
`IOfficeService`, `IServicesService`) estaban definidas dentro de `booking.infrastructure`,
junto a su implementación. Esto dejaba a `booking.core` sin ningún contrato propio (solo
DTOs/Models) y obligaba a los controllers de `booking.api` a depender directamente de
Infrastructure.

**Cambio:**
- Las 4 interfaces se movieron a `booking.core/Interfaces/`.
- Las clases de `booking.infrastructure/Services/*` ahora solo las implementan.
- Los controllers y `Program.cs` referencian `booking.core.Interfaces` en vez de
  `booking.infrastructure.Services` para los tipos de contrato.

Archivos:
- `src/booking.core/Interfaces/IAuthService.cs` (nuevo)
- `src/booking.core/Interfaces/IBookingService.cs` (nuevo)
- `src/booking.core/Interfaces/IOfficeService.cs` (nuevo)
- `src/booking.core/Interfaces/IServicesService.cs` (nuevo)
- `src/booking.infrastructure/Services/AuthService.cs`, `BookingService.cs`,
  `OfficeService.cs`, `ServiceService.cs`
- `src/booking.api/Controllers/*.cs`, `src/booking.api/Program.cs`

## 2. Seguridad — endpoints sin autorización

**Problema:** `Program.cs` definía políticas de permisos detalladas
(`Booking.Read/Create/Update/Delete`, `Office.*`, `Service.*`) pero ningún controller tenía
`[Authorize]`. Cualquiera, sin token, podía crear/editar/borrar Offices, Services y Bookings.

**Cambio:** se agregó `[Authorize]` a nivel de controller y `[Authorize(Policy = "...")]`
por acción en `BookingController`, `OfficeController` y `ServicesController`, usando las
políticas que ya existían. `AuthController` (login/register) se mantiene público.

> **Importante:** el frontend actual no envía el token JWT en las llamadas HTTP (no hay
> interceptor configurado), así que tras este cambio las llamadas del frontend a estos
> endpoints devolverán `401`. Esto se resolverá en la siguiente pasada, sobre el frontend.

## 3. Seguridad — mass assignment en creación de recursos

**Problema:** `BookingUpsertDto`/`OfficeUpsertDto` incluyen `id`, y los servicios lo usaban
tal cual al crear (`Id = dto.id`), dejando que el cliente eligiera el ID del nuevo registro.
`BookingService.CreateAsync`/`UpdateAsync` también tomaban `createdAt` del cliente.

**Cambio:** `BookingService.CreateAsync` y `OfficeService.CreateAsync` ya no asignan
`Id` desde el DTO (lo genera la base de datos). `CreatedAt` en Booking ahora se fija en el
servidor (`DateTime.UtcNow`) tanto en `CreateAsync` como al dejar de sobrescribirse en
`UpdateAsync`.

Archivos: `src/booking.infrastructure/Services/BookingService.cs`, `OfficeService.cs`

## 4. Seguridad — secretos en el repositorio

**Problema 1:** `appsettings.json` tenía la clave de firma JWT (`Jwt:Key`) en texto plano,
commiteada al repo.

**Cambio:** se eliminó el valor de `appsettings.json`. El proyecto `booking.api` ya tenía
`UserSecretsId` configurado, así que se generó una clave nueva y se guardó localmente con
`dotnet user-secrets set "Jwt:Key" "..."` (fuera del repo). **La clave anterior debe
considerarse comprometida** — si se usó en algún ambiente real, hay que rotarla ahí también.
El check ya existente en `Program.cs` (`Jwt:Key missing.`) ahora sirve como guardarraíl real:
si no está configurada, el arranque falla con un mensaje claro en vez de correr insegura.

**Problema 2:** la contraseña de los usuarios de demo (`"P@ssw0rd!"`) estaba escrita en
código (`DatabaseBootstrapper.cs`), y el seed de datos de demo corría en **cualquier
entorno**, incluida una eventual producción.

**Cambio:**
- La contraseña se movió a configuración: `Seed:DemoUserPassword` en
  `appsettings.Development.json`.
- El seed se dividió en dos partes: `SeedRolesAndPermissionsAsync` (roles y permisos, corre
  siempre — son necesarios para que las políticas de autorización tengan sentido) y
  `SeedDemoDataAsync` (usuarios/oficinas/servicios de ejemplo, **solo corre en
  Development**).
- De paso se corrigió que el rol `r_admin` solo tenía permisos de `Booking.*`; ahora
  también tiene `Office.*` y `Service.*`, si no el propio usuario admin de demo quedaría
  bloqueado por los `[Authorize]` nuevos del punto 2.

Archivos: `src/booking.infrastructure/Persistence/DatabaseBootstrapper.cs`,
`DbInitializerHostedService.cs`, `src/booking.api/appsettings.json`,
`appsettings.Development.json`

## 5. Bug — excepción no controlada en disponibilidad

**Problema:** `BookingService.GetAvailability` usaba `DateTime.Parse`/`int.Parse` sin
manejo de errores ni cultura invariante; una fecha o frecuencia mal formada tumbaba el
endpoint con un 500.

**Cambio:** se reemplazó por `TryParse` con `CultureInfo.InvariantCulture`, validando
también que la frecuencia sea un número positivo. Ante entrada inválida, el servicio
devuelve `ok = false` con un mensaje, y el controller responde `400 Bad Request` (antes
devolvía `404`, que no reflejaba bien un error de validación).

Archivos: `src/booking.infrastructure/Services/BookingService.cs`,
`src/booking.api/Controllers/BookingController.cs`, `src/booking.core/Constants/Codes.cs`
(nuevo código `ERR_INVALID_INPUT`)

## 6. CORS configurable

**Problema:** el origen permitido (`http://localhost:4200`) estaba hardcodeado en
`Program.cs`, sin forma de cambiarlo por entorno.

**Cambio:** ahora se lee de `Cors:AllowedOrigins` en la configuración, con
`http://localhost:4200` como fallback si no se define (mismo comportamiento que antes por
defecto). Para producción, basta con agregar la sección en `appsettings.Production.json`.

Archivo: `src/booking.api/Program.cs`

## 7. Limpieza de código muerto y duplicado

- Eliminado `DbInitializer.cs`: nunca se invocaba (el hosted service solo llama a
  `DatabaseBootstrapper`), dejaba dos caminos de inicialización de BD incoherentes.
- Eliminados `Class1.cs` en `booking.core` y `booking.infrastructure`: plantillas por
  defecto de `dotnet new classlib`, sin uso.
- Eliminada la validación manual duplicada de email/password en
  `AuthService.RegisterAsync` — ya la cubre `RegisterRequestValidator` (FluentValidation),
  registrado con auto-validación en `Program.cs`.
- Renombrado `Controllers/ServiceController.cs` → `Controllers/ServicesController.cs` para
  que el nombre de archivo coincida con la clase `ServicesController` (la ruta HTTP
  `api/Services` no cambió).

## Pendiente / fuera de alcance de esta pasada

- El frontend no envía el JWT en sus requests (no hay `HttpInterceptor`) y sus guards de
  ruta (`AuthGuard`, `RoleGuard`) no están aplicados a ninguna ruta — hoy cualquiera puede
  navegar a `/booking/*` sin login. Con los `[Authorize]` de este cambio, las llamadas del
  frontend a Booking/Office/Services van a fallar con 401 hasta que se resuelva eso.
- El patrón de retorno con tuplas gigantes (`(bool ok, T data, string code, string?
  message)`) se dejó tal cual — es un cambio de estilo más grande que toca todas las firmas
  y no se consideró parte de las correcciones urgentes.
- No se restauró la validación de reglas de negocio (rango de fechas, solapamiento de
  bookings, etc.) a nivel de servidor para Booking/Office/Service — sigue dependiendo
  únicamente del frontend.
