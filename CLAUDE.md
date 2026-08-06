# CLAUDE.md — GymFlow

> **Contexto obligatorio:** lee [`PROJECT.md`](./PROJECT.md) completo antes de tocar código.
> Es la fuente de verdad del producto, stack, arquitectura y roadmap.

## Qué es esto
Sistema de gestión de gimnasios (SaaS multi-tenant, LATAM) **operado por la recepción del
gimnasio**. No hay app para el miembro ni cobros dentro del sistema: la recepción registra
clientes y sus membresías, controla la asistencia con un **código de 4 dígitos**, ve cuánto
falta para que venza cada suscripción y avisa por **WhatsApp** cuando está por vencer (el pago
se hace fuera del sistema, en el gimnasio). Además, dashboard y reportes.

Backend .NET 10 (Clean Architecture + Hexagonal), panel Angular (recepción/admin). Postgres
(Neon) + Redis (opcional). Ver PROJECT.md §3–§4.

## Estructura
```
src/
├── GymFlow.Domain/          → Entidades, VOs, reglas puras. SIN dependencias externas.
├── GymFlow.Application/      → Casos de uso, DTOs, PUERTOS (interfaces).
├── GymFlow.Infrastructure/   → ADAPTADORES: EF Core, seguridad, Hangfire, (Redis futuro).
├── GymFlow.Api/              → Controllers, middleware de tenant, JWT, DI.
clients/
└── web/    (Angular + PrimeNG) → Panel de recepción/admin (login, miembros, planes,
                                  check-in, dashboard).
tests/
└── GymFlow.Domain.Tests/     → xUnit.
```

## No-negociables (de PROJECT.md §7)
- Dinero **siempre** `decimal`, nunca `float/double`.
- `TenantId` en **toda** entidad de negocio (implementa `ITenantScoped`).
- Fechas en **UTC** en DB; se muestran en la zona del tenant (`America/Lima` por defecto).
- Regla de dependencias: Domain no depende de nada; Application solo de Domain;
  Infrastructure/Api hacia adentro.

## Multi-tenancy (implementado en Fase 0)
- Single DB + `TenantId` por fila + **EF Core global query filters** (`AppDbContext`).
- Resolución en `TenantResolutionMiddleware`: request autenticada usa el claim `tenant_id`
  del JWT (fuente de verdad); anónima (login) usa header `X-Tenant-Id`/`X-Tenant-Subdomain`
  o subdominio, validando contra la tabla `tenants`. Nunca se confía en el TenantId del
  cliente sin validar.
- `SaveChanges` asigna el TenantId a entidades nuevas y bloquea escrituras cross-tenant.
- Un único tipo de actor en el token: **staff** (claim `actor=staff`). Políticas RBAC:
  `Manager` (owner/admin) y `Staff` (incluye recepción).

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
(password `Cambiar123!`) y un miembro demo (documento `12345678`) con un plan `Mensual` y una
membresía activa, para poblar el panel. Todo en Development.

## Frontend (clients/web)
Angular 22 (standalone + signals) + PrimeNG (tema Aura). Dev server con proxy
(`proxy.conf.json`) que redirige `/api` al backend en `http://localhost:5066`.
Login pide **gimnasio (subdominio) + correo + contraseña**: en localhost no hay
subdominio, así que el front envía la cabecera `X-Tenant-Subdomain` solo en el login;
tras autenticar, el tenant viaja en el claim del JWT. El JWT se guarda en localStorage
y un interceptor lo adjunta; ante 401 cierra sesión.
```bash
cd clients/web
npm start              # ng serve con proxy → backend local http://localhost:5066
npm run start:render   # ng serve apuntando al backend desplegado en Render (proxy.render.conf.json)
npm run build
```

## Estado del roadmap
**Listo (base operativa de recepción):**
- ✅ **Fundaciones** — 4 capas, multi-tenancy, JWT + RBAC (staff), seed, migración inicial, CI.
- ✅ **Miembros & Membresías** — CRUD de miembros/planes, membresías con estados
  (activa/congelada/vencida/morosa). Panel: login, miembros, planes.
- ✅ **Pagos en recepción** — registro de pago manual (efectivo) e historial por miembro.
  Morosidad automática: job diario Hangfire (`overdue-sweep`, 03:00) marca morosas las
  membresías vencidas sin renovar.
- ✅ **Check-in & aforo** — check-in en recepción **por código de 4 dígitos** (o búsqueda por
  nombre/DNI) validando membresía vigente, aforo en tiempo real por SignalR
  (`/hubs/occupancy`, backplane Redis opcional) y asistencia del día.
- ✅ **Reportes & Dashboard** — `GET /api/reports/dashboard` (policy Manager): ingresos,
  morosidad, retención/churn, ocupación por hora. Panel: página **Dashboard** (owner/admin).
- ✅ **Vencimientos & aviso por WhatsApp (manual)** — `GET /api/memberships/expiring?withinDays=N`
  lista membresías por vencer (o ya vencidas) de miembros activos. Panel: página **Vencimientos**
  con "días para vencer" y botón **WhatsApp** que abre un enlace `wa.me` con el mensaje
  prellenado para que la recepción lo envíe con un clic.
- ✅ **Personal (staff)** — CRUD de usuarios del panel con roles (owner/admin/recepción),
  reset de contraseña y activar/desactivar. `api/staff/*` (policy Manager). Panel: página
  **Personal** (solo owner/admin).

**Pendiente (núcleo del nuevo enfoque):**
- ⏭️ **WhatsApp automático (Meta Cloud API)** — envío automático del aviso de vencimiento
  (hoy es manual vía `wa.me`) + job de recordatorio N días antes. Requiere WhatsApp Business
  verificado y plantillas aprobadas. Recordar implementarla cuando haya tracción.

**Deploy:** DB en **Neon** (user-secrets/env). Backend dockerizado para **Render**
(`Dockerfile` + `render.yaml`); ver "Deploy (Render)".

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
El panel Angular apunta a la URL pública de Render (`environment.apiBaseUrl`/proxy).

## Reportes & Dashboard
- `ReportService` (Application) arma el dashboard del tenant en un solo `GET /api/reports/dashboard`
  (policy **Manager**; la recepción no ve ingresos ni churn). Acepta `from`/`to` (fechas locales
  yyyy-MM-dd); sin ellas cubre los últimos 30 días terminando hoy (tope 366 días).
- Todo se calcula en la zona del tenant: `IClock` tiene `ToLocalTime` y `StartOfDayUtc` para
  agrupar por día/hora. Los timestamps UTC se traen acotados por rango y se agrupan en memoria
  (volumen chico); los conteos de estado/morosidad salen directo de la DB. Dinero siempre decimal.
- Devuelve KPIs (ingresos, ticket promedio, morosidad + monto en riesgo, miembros activos/nuevos,
  retención/churn) + series: ingresos por día (con ceros), ingresos por medio, membresías por
  estado y ocupación por hora (0–23).
- Front: página **Dashboard** (`clients/web`, ruta `/dashboard`, solo owner/admin) con KPIs,
  gráficas de barras en CSS (sin dependencias de charting) y selector de rango (`p-datepicker`).

## Check-in & aforo
- **Código de 4 dígitos:** cada `Member` tiene `AccessCode` (4 dígitos, único por tenant vía
  índice único parcial; se genera al crear el miembro y se puede regenerar con
  `POST /api/members/{id}/regenerate-code`). La recepción lo teclea en la página Check-in y
  `POST /api/checkins/by-code` (`{code}`) busca al miembro y registra el ingreso. Se mantiene
  la búsqueda por nombre/DNI (`POST /api/checkins`) como alternativa.
- `CheckIn` (ITenantScoped) guarda `OccurredAtUtc` + `LocalDate` (día en zona del tenant)
  e `IsValid`/`Reason`. El aforo del día = ingresos válidos con `LocalDate == hoy`.
- Tiempo real: puerto `IOccupancyNotifier` (Application) → adaptador `SignalROccupancyNotifier`
  (Api) que difunde `occupancyChanged` al grupo `tenant:{id}`. El JWT viaja por query string
  en el handshake del hub (ver `OnMessageReceived`). Con `ConnectionStrings:Redis` se activa
  el backplane Redis (solo necesario con varias instancias de la Api).
- Front: `@microsoft/signalr` conecta a `/hubs/occupancy` (proxied en dev con `ws:true`).

## Vencimientos & WhatsApp
- `GET /api/memberships/expiring?withinDays=N` (policy Staff) devuelve, de **miembros activos**,
  las membresías `Active`/`Overdue` cuyo `EndDate <= hoy + N` (hoy en la zona del tenant), con
  `DaysToExpiry` (negativo si ya venció), nombre, teléfono y plan. Ordenadas por `EndDate`.
- Front: página **Vencimientos** (ruta `/expirations`) con selector de rango (3/7/15/30 días),
  badge de "días para vencer" y botón **WhatsApp**. El aviso es **manual**: arma el mensaje y abre
  `https://wa.me/{telefono}?text=...` en una pestaña. El teléfono se normaliza a solo dígitos y,
  si parece móvil peruano de 9 dígitos, se antepone `51` (heurística; ajustar para otros países).
- **Pendiente:** envío automático vía Meta WhatsApp Cloud API + job de recordatorio (ver roadmap).

## Personal (staff)
- `api/staff/*` (policy **Manager**): `GET` lista, `POST` crea (nombre, correo, contraseña, rol),
  `PUT {id}` edita nombre+rol (el correo no se cambia), `POST {id}/reset-password`,
  `POST {id}/activate|deactivate`. Correo único por tenant; contraseña mínima 6 caracteres.
- Guardas: no se puede dejar al tenant sin un **owner activo** (bloquea desactivar o cambiarle el
  rol al último owner). Todo acotado al tenant por el global query filter.
- Front: página **Personal** (`/staff`, solo owner/admin) con alta/edición, cambio de contraseña
  y activar/desactivar.

## Pagos (en recepción)
- El pago se cobra fuera del sistema; la recepción solo lo **registra** (`POST /api/payments/cash`,
  policy Staff) e historial por miembro (`GET /api/payments/by-member/{id}`). Nace completado.
- Dinero siempre `decimal`. No hay pasarela de pago ni cobro dentro del sistema.
- **Morosidad**: job diario `overdue-sweep` (Hangfire, 03:00) marca morosas las membresías
  activas vencidas. Corre sin tenant resuelto → itera tenants e ignora el filtro global.
  Hangfire usa Postgres como almacenamiento; dashboard en `/hangfire` solo en Development.
- Migraciones EF: hay una `DesignTimeDbContextFactory` para que el tooling no arranque el
  host completo (Hangfire/seed); acepta `ConnectionStrings__Default` por variable de entorno.
