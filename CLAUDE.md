# CLAUDE.md — GymFlow

> **Contexto obligatorio:** lee [`PROJECT.md`](./PROJECT.md) completo antes de tocar código.
> Es la fuente de verdad del producto, stack, arquitectura y roadmap por fases.

## Qué es esto
SaaS multi-tenant para gimnasios (LATAM). Backend .NET 10 (Clean Architecture +
Hexagonal), panel Angular, app Flutter. Postgres (Neon) + Redis. Ver PROJECT.md §3–§4.

## Estructura
```
src/
├── GymFlow.Domain/          → Entidades, VOs, reglas puras. SIN dependencias externas.
├── GymFlow.Application/      → Casos de uso, DTOs, PUERTOS (interfaces).
├── GymFlow.Infrastructure/   → ADAPTADORES: EF Core, seguridad, pagos (Culqi), Hangfire, (Redis futuro).
├── GymFlow.Api/              → Controllers, middleware de tenant, JWT, DI.
clients/
├── web/    (Angular + PrimeNG) → Panel admin/recepción (login, miembros, planes).
└── mobile/ (Flutter)         → App del miembro (login, mi plan, check-in, historial).
tests/
└── GymFlow.Domain.Tests/     → xUnit.
```

## No-negociables (de PROJECT.md §7)
- Dinero **siempre** `decimal`, nunca `float/double`.
- `TenantId` en **toda** entidad de negocio (implementa `ITenantScoped`).
- Fechas en **UTC** en DB; se muestran en la zona del tenant (`America/Lima` por defecto).
- Regla de dependencias: Domain no depende de nada; Application solo de Domain;
  Infrastructure/Api hacia adentro. Pasarelas de pago SIEMPRE detrás de `IPaymentGateway`.

## Multi-tenancy (implementado en Fase 0)
- Single DB + `TenantId` por fila + **EF Core global query filters** (`AppDbContext`).
- Resolución en `TenantResolutionMiddleware`: request autenticada usa el claim `tenant_id`
  del JWT (fuente de verdad); anónima usa header `X-Tenant-Id` o subdominio, validando
  contra la tabla `tenants`. Nunca se confía en el TenantId del cliente sin validar.
- `SaveChanges` asigna el TenantId a entidades nuevas y bloquea escrituras cross-tenant.

## Comandos
```bash
dotnet build GymFlow.slnx
dotnet test  GymFlow.slnx
# Migraciones (desde la raíz):
dotnet ef migrations add <Nombre> --project src/GymFlow.Infrastructure --startup-project src/GymFlow.Api --output-dir Persistence/Migrations
dotnet ef database update      --project src/GymFlow.Infrastructure --startup-project src/GymFlow.Api
```

## Configuración local
`appsettings.Development.json` trae una connection string a Postgres local y una
`Jwt:SigningKey` SOLO para desarrollo. Para apuntar a Neon, sobrescribe
`ConnectionStrings:Default` (idealmente vía user-secrets o variable de entorno, no en el repo).
El seed (`AppDbSeeder`) crea el tenant `demo`, un owner `owner@demo.gymflow.pe`
(password `Cambiar123!`) y un **miembro demo** con acceso a la app: documento `12345678`,
password `Miembro123!`, con un plan `Mensual` y una membresía activa. Todo en Development.

## Frontend (clients/web)
Angular 22 (standalone + signals) + PrimeNG (tema Aura). Dev server con proxy
(`proxy.conf.json`) que redirige `/api` al backend en `http://localhost:5066`.
Login pide **gimnasio (subdominio) + correo + contraseña**: en localhost no hay
subdominio, así que el front envía la cabecera `X-Tenant-Subdomain` solo en el login;
tras autenticar, el tenant viaja en el claim del JWT. El JWT se guarda en localStorage
y un interceptor lo adjunta; ante 401 cierra sesión.
```bash
cd clients/web
npm start   # ng serve con proxy → http://localhost:4200
npm run build
```

## Estado del roadmap
- ✅ **Fase 0** — Fundaciones: 4 capas, multi-tenancy, JWT + RBAC, seed, migración inicial, CI.
- ✅ **Fase 1** — Miembros & Membresías: CRUD miembros/planes, membresías con estados
  (activa/congelada/vencida), panel Angular (login, miembros, planes).
- ✅ **Fase 2** — Pagos: registro de pago manual (efectivo), `IPaymentGateway` + adaptador
  Culqi, morosidad (`MembershipStatus.Overdue`) y job diario Hangfire (`overdue-sweep`).
  Panel: registrar pago e historial en el diálogo de membresías.
- ✅ **Fase 3** — Check-in & Asistencia: registro de ingreso en recepción validando
  membresía vigente (ingresos no válidos se guardan como traza), aforo en tiempo real
  por SignalR (`/hubs/occupancy`, backplane Redis opcional) y asistencia del día.
  Panel: página Check-in (buscar miembro, registrar, aforo en vivo, asistencia).
- ✅ **Fase 4** — App móvil Flutter + backend del miembro: app (`clients/mobile`) con login
  del miembro (DNI + contraseña + gimnasio), Mi plan, Check-in e Historial (asistencia/pagos),
  consumiendo la API `/member-auth` + `/me/*` (ya implementada, ver abajo).
- ⏭️ **Fase 5** — Reportes & Dashboard (siguiente): ingresos, morosidad, churn, ocupación por hora.
- **Deploy**: DB en **Neon** (conectada vía user-secrets/env). Backend dockerizado para
  **Render** (`Dockerfile` + `render.yaml`); ver sección "Deploy (Render)".

## Deploy (Render)
Backend containerizado (`Dockerfile` multi-stage .NET 10) para Render, con blueprint
`render.yaml` (servicio web Docker, healthCheck `/health`, región `ohio` cerca de Neon).
- **Puerto**: Render inyecta `PORT`; `Program.cs` bindea Kestrel a `0.0.0.0:$PORT`.
- **TLS**: la termina el proxy de Render; `UseHttpsRedirection` solo corre en Development.
- **Migraciones**: se aplican en el arranque en todo entorno (`Database.Migrate()` en `Program`).
  El seed solo corre en Development o con `Seed:Enabled=true` (poblar el demo una vez).
- **CORS**: sin `Cors:AllowedOrigins` permite cualquier origen (JWT por header/query, sin
  cookies); con la lista, restringe y habilita credenciales (para SignalR).
- **Variables en Render** (secretos, dashboard): `ConnectionStrings__Default` (Neon .NET),
  `Jwt__SigningKey` (>=32 bytes). `ASPNETCORE_ENVIRONMENT=Production` ya va en el blueprint.
- Deploy: Render → New → Blueprint → conecta el repo → aplica → carga los secretos.
Las apps cliente apuntan a la URL pública de Render (`API_BASE_URL` en Flutter,
`environment.apiBaseUrl`/proxy en Angular).

## App móvil del miembro (Fase 4)
Flutter 3.44 (Material 3), en `clients/mobile`. Estado: `AuthService` (ChangeNotifier) con
JWT en `SharedPreferences`; `MemberApi` (paquete `http`). Config del backend en
`lib/config.dart` (`--dart-define=API_BASE_URL=...`; emulador Android usa `10.0.2.2`).
```bash
cd clients/mobile
flutter run              # requiere Android SDK (Android Studio) para dispositivo/emulador
flutter run -d chrome    # web, para probar sin Android
flutter analyze && flutter test && flutter build web
```
**Backend del miembro (implementado).** El `Member` puede tener `PasswordHash` (acceso a la
app); el staff lo asigna con `POST /api/members/{id}/set-password` (policy Manager). Endpoints:
- `POST /api/member-auth/login` (header `X-Tenant-Subdomain`, body `{documentId, password}`)
  → `{accessToken, expiresAtUtc, memberId, fullName}`. Emite un JWT con `sub`=memberId,
  `tenant_id` y `actor=member`. Solo entran miembros activos y con contraseña.
- `GET /api/me/membership` → membresía del miembro con nombre de plan (o 204).
- `GET /api/me/checkins`, `GET /api/me/payments` → historial del propio miembro.
- `POST /api/me/checkins` → auto check-in (método `App`), reusa `CheckInService`.
Autorización: policy `Member` exige el claim `actor=member`; los tokens de staff (con `role`)
no acceden a `/me/*` y viceversa. El memberId sale del claim `sub`, nunca del cliente.

## Check-in & aforo (Fase 3)
- `CheckIn` (ITenantScoped) guarda `OccurredAtUtc` + `LocalDate` (día en zona del tenant)
  e `IsValid`/`Reason`. El aforo del día = ingresos válidos con `LocalDate == hoy`.
- Tiempo real: puerto `IOccupancyNotifier` (Application) → adaptador `SignalROccupancyNotifier`
  (Api) que difunde `occupancyChanged` al grupo `tenant:{id}`. El JWT viaja por query string
  en el handshake del hub (ver `OnMessageReceived`). Con `ConnectionStrings:Redis` se activa
  el backplane Redis (solo necesario con varias instancias de la Api).
- Front: `@microsoft/signalr` conecta a `/hubs/occupancy` (proxied en dev con `ws:true`).

## Pagos (Fase 2)
- Pasarela **siempre** detrás de `IPaymentGateway` (Application); el único adaptador que
  conoce Culqi es `CulqiPaymentGateway` (Infrastructure), sobre la API REST v2.
- La llave `Culqi:SecretKey` NO va al repo: user-secrets / variable de entorno. Sin llave,
  el cobro por pasarela se rechaza limpiamente; el pago en efectivo no la necesita.
- **Morosidad**: job diario `overdue-sweep` (Hangfire, 03:00) marca morosas las membresías
  activas vencidas. Corre sin tenant resuelto → itera tenants e ignora el filtro global.
  Hangfire usa Postgres como almacenamiento; dashboard en `/hangfire` solo en Development.
- Migraciones EF: hay una `DesignTimeDbContextFactory` para que el tooling no arranque el
  host completo (Hangfire/seed); acepta `ConnectionStrings__Default` por variable de entorno.
