# Frontend — Correcciones y mejoras aplicadas

Este documento resume los cambios aplicados al frontend (Angular 19) a partir de la
revisión previa. Complementa `backend/CHANGES.md`.

## 1. Autenticación no se propagaba a las peticiones HTTP

**Problema:** el token JWT se guardaba en `AuthStore`/`localStorage` tras el login, pero
no existía ningún `HttpInterceptor` que lo adjuntara a las peticiones salientes. Ahora que
el backend exige `[Authorize]` en Booking/Office/Services, todas esas llamadas fallaban
con 401.

**Cambio:**
- Nuevo interceptor funcional `core/interceptors/auth.interceptor.ts`: agrega el header
  `Authorization: Bearer <token>` cuando hay sesión activa. Si el backend responde 401 para
  una petición que sí llevaba token (sesión expirada/ inválida), limpia la sesión y
  redirige a `/auth/login`. No interfiere con un intento de login fallido (ese 401 no lleva
  token, así que no dispara el logout).
- Registrado en `app.config.ts` vía `provideHttpClient(withInterceptors([authInterceptor]))`.
- `AuthStore` ganó `getToken()` para exponer el token actual de forma síncrona.

## 2. Rutas sin proteger

**Problema:** `AuthGuard` se importaba en `app.routes.ts` pero el bloque que lo aplicaba
estaba comentado; `RoleGuard` no se referenciaba en ninguna ruta. Cualquiera podía navegar
a `/booking/*` sin loguearse.

**Cambio:** `canActivate: [AuthGuard]` aplicado a la ruta `booking` en `app.routes.ts`, y se
eliminó el bloque comentado muerto. `RoleGuard` no se aplicó a ninguna ruta porque hoy no
existe una sección admin en la app — se dejó listo (ver punto 6) para cuando exista.

## 3. La sesión se perdía al refrescar la página

**Problema:** `AuthStore.loadSession()` (restaura la sesión desde `localStorage`) estaba
definido pero nunca se llamaba desde ningún lado.

**Cambio:** se registra como `provideAppInitializer(() => inject(AuthStore).loadSession())`
en `app.config.ts`, así que corre antes de que el router resuelva la navegación inicial —
un refresh de página ya no cierra la sesión en memoria.

## 4. URL de la API hardcodeada en 4 archivos

**Problema:** `'http://localhost:5170/api'` estaba repetido y hardcodeado en
`auth-impl.repository.ts`, `booking-impl.repository.ts`, `office-impl.repository.ts` y
`service-impl.repository.ts`. No había forma de cambiar de entorno sin editar código.

**Cambio:**
- Nuevos `src/environments/environment.ts` (default/producción, `apiUrl: '/api'`) y
  `environment.development.ts` (`apiUrl: 'http://localhost:5170/api'`, mismo valor que
  antes).
- `angular.json`: la configuración `development` del builder ahora hace
  `fileReplacements` de `environment.ts` → `environment.development.ts`.
- Los 4 repositorios ahora leen `environment.apiUrl` en vez del literal.

> Antes de desplegar a producción real, ajustar `apiUrl` en `environment.ts` a la URL real
> del backend si no se sirve desde el mismo origen que el frontend.

## 5. Bug de integración: filtro por oficina no funcionaba

**Problema:** `ServiceImplRepository.getServices(officeId)` mandaba `?officeId=X` al
backend, pero `ServicesController.GetAll` (backend) no lee ese parámetro — siempre devuelve
todos los servicios de todas las oficinas.

**Cambio:** mientras no se agregue el filtro en el backend, se filtra client-side en
`ServiceImplRepository` por `entity.officeId === officeId` antes de mapear la respuesta.

## 6. Guard de roles inconsistente

**Problema:** `RoleGuard.canActivate()` devolvía `boolean` síncrono y llamaba
`router.navigate()` como efecto secundario, a diferencia de `AuthGuard` que devuelve
`Observable<boolean | UrlTree>`.

**Cambio:** `RoleGuard` ahora devuelve `boolean | UrlTree` (usa `createUrlTree` en vez de
`navigate` con efecto secundario), mismo patrón que `AuthGuard`.

## 7. Login sin feedback de error

**Problema:** si el login fallaba, el único rastro era un `console.error`; el usuario no
veía nada en pantalla.

**Cambio:** `LoginComponent` ahora tiene un signal `errorMessage` que se muestra en el
template (`login.component.html`) cuando el login falla, y se deshabilita el botón mientras
`isLoading()` es true. Se quitaron los `console.log`/`console.error` de debug.

## 8. Limpieza de código muerto / copy-paste

- `BookingImplRepository` declaraba `mapper = new OfficeRepositoryMapper()` (mapper de
  **Office** dentro del repositorio de **Booking**) sin usarlo — resto de copiar/pegar.
  Eliminado el campo y su import.
- `ServiceImplRepository` importaba `OfficeRepositoryMapper` y `ServiceDetailRepositoryMapper`/
  `ServiceDetailEntity` sin usarlos. Eliminados los imports muertos.
- Bloques comentados "TESTING" y el `console.log('wPlaces', ...)` eliminados de
  `booking.store.ts`.

## Pendiente / fuera de alcance de esta pasada

- El token JWT sigue en `localStorage` (no `HttpOnly` cookie) — trade-off conocido, no se
  cambió porque requeriría soporte del backend para cookies de sesión.
- `RoleGuard` sigue sin aplicarse a ninguna ruta porque no existe todavía una sección
  admin en el frontend.
- El filtro por oficina en Services sigue siendo un parche client-side; lo correcto es que
  el backend (`ServicesController.GetAll`) acepte y aplique `officeId`.
- Las reglas de negocio de `BookingModal` (horario laboral, máximo 7 semanas, días hábiles)
  siguen validándose solo en el cliente; el backend no las revalida.
