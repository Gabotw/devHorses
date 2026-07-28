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
npm start          # ng serve con proxy → backend local http://localhost:5066
npm run start:render  # ng serve apuntando al backend desplegado en Render (proxy.render.conf.json)
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
- ✅ **Fase 5** — Reportes & Dashboard: endpoint `GET /api/reports/dashboard` (policy Manager)
  con ingresos por día/medio, morosidad (monto en riesgo), retención/churn, miembros y
  ocupación por hora. Panel Angular: página **Dashboard** (KPIs, gráficas de barras CSS,
  selector de rango), visible solo para owner/admin.
- ✅ **Fase 6** — Billing del SaaS: capa de plataforma (super-admin `actor=platform`) con
  catálogo de planes, suscripción por tenant (estado/período) y corte de morosidad SaaS
  (job `saas-billing-sweep`). API `/api/platform/*` (backend; consola web pendiente).
- ✅ **Fase 7** — Clases (post-MVP): sesiones con cupo, reservas con lista de espera
  (promoción FIFO al liberarse un cupo) y asistencia. API staff `/api/classes/*` + miembro
  `/api/me/classes*` y `/api/me/reservations`. Panel: página **Clases**; app: tab **Clases**.
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

## Reportes & Dashboard (Fase 5)
- `ReportService` (Application) arma el dashboard del tenant en un solo `GET /api/reports/dashboard`
  (policy **Manager**; la recepción no ve ingresos ni churn). Acepta `from`/`to` (fechas locales
  yyyy-MM-dd); sin ellas cubre los últimos 30 días terminando hoy (tope 366 días).
- Todo se calcula en la zona del tenant: `IClock` ganó `ToLocalTime` y `StartOfDayUtc` para
  agrupar por día/hora. Los timestamps UTC se traen acotados por rango y se agrupan en memoria
  (volumen chico); los conteos de estado/morosidad salen directo de la DB. Dinero siempre decimal.
- Devuelve KPIs (ingresos, ticket promedio, morosidad + monto en riesgo, miembros activos/nuevos,
  retención/churn) + series: ingresos por día (con ceros), ingresos por medio, membresías por
  estado y ocupación por hora (0–23).
- Front: página **Dashboard** (`clients/web`, ruta `/dashboard`, solo owner/admin) con KPIs,
  gráficas de barras en CSS (sin dependencias de charting) y selector de rango (`p-datepicker`).

## Clases & reservas (Fase 7)
- Tenant-scoped. `ClassSession` (ITenantScoped) guarda `StartsAtUtc` + `LocalDate` (día en la
  zona del tenant, como `CheckIn`), `Capacity` y estado (Scheduled/Cancelled). `ClassReservation`
  (ITenantScoped) enlaza sesión↔miembro con estado (Booked/Waitlisted/Cancelled/Attended).
- **Cupos y waitlist**: al reservar, si `Booked < Capacity` → Booked; si no → Waitlisted. Al
  cancelar una reserva con cupo, se promueve (FIFO por `CreatedAtUtc`) a la primera en espera.
  Índice único parcial `(ClassSessionId, MemberId) WHERE Status IN (1,2)`: un miembro no puede
  tener dos reservas activas en la misma sesión (tras cancelar sí puede volver a reservar).
  Reservar exige membresía vigente en la fecha de la clase (misma regla que el check-in).
- **API staff** (policy Staff): `GET/POST /api/classes`, `POST /api/classes/{id}/cancel`
  (libera las reservas), `GET /api/classes/{id}/roster`, `POST /api/classes/{id}/attendance/{memberId}`.
- **API miembro** (policy Member, `/me`): `GET /me/classes` (próximas con mi estado),
  `POST /me/classes/{id}/reserve`, `POST /me/classes/{id}/cancel`, `GET /me/reservations`.
- Front: panel Angular página **Clases** (crear, agenda con ocupación, roster, asistencia,
  cancelar); app Flutter tab **Clases** (reservar / lista de espera / cancelar, pull-to-refresh).

## Billing del SaaS (Fase 6)
- Capa de **plataforma** (cross-tenant), separada del panel del gimnasio. Entidades sin
  `ITenantScoped` (no llevan global query filter, igual que `Tenant`): `PlatformPlan`
  (catálogo SaaS: precio decimal, días de período, tope de miembros), `Subscription`
  (suscripción vigente de un tenant, snapshot de plan + estado + período; única por tenant) y
  `PlatformAdmin` (super-admin).
- **Auth**: `POST /api/platform/auth/login` emite un JWT con `actor=platform` y **sin**
  `tenant_id`. Policy `Platform` exige ese claim. El `TenantResolutionMiddleware` exenta
  `/api/platform/*` (opera cross-tenant); por eso un token de plataforma **no** entra a los
  endpoints por-tenant (401) y un token de staff/member **no** entra a `/api/platform` (403).
- **Endpoints** (super-admin): `GET/POST/PUT /api/platform/plans` (+ activate/deactivate),
  `GET /api/platform/tenants` (gimnasios con su suscripción y conteo de miembros),
  `POST /api/platform/tenants/{id}/subscription` (asignar/cambiar plan),
  `.../subscription/renew` y `.../subscription/cancel`. Al cambiar la suscripción se sincroniza
  el estado cacheado en `Tenant.SubscriptionStatus` (lo que decide si el gimnasio está activo).
- **Morosidad SaaS**: job diario `saas-billing-sweep` (Hangfire, 04:00) marca `PastDue` las
  suscripciones vigentes cuyo período venció (hoy en la zona del tenant) y sincroniza el tenant.
- **Cobro**: manual/interno por ahora (coherente con "activar solo con tracción"); el cobro real
  reusaría `IPaymentGateway`. **Consola web del super-admin: pendiente** (hoy solo API).
- Seed (Development): super-admin `admin@gymflow.pe` / `Superadmin123!`, planes `Starter`
  (S/ 99, 200 miembros) y `Pro` (S/ 199, ilimitado), y el gimnasio demo suscrito a `Starter`.

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
