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
├── GymFlow.Infrastructure/   → ADAPTADORES: EF Core, seguridad, (pagos/Hangfire/Redis futuros).
├── GymFlow.Api/              → Controllers, middleware de tenant, JWT, DI.
clients/
├── web/    (Angular + PrimeNG) → Panel admin/recepción (login, miembros, planes).
└── mobile/ (Flutter)         → Fase 4 (aún no scaffoldeado).
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
El seed (`AppDbSeeder`) crea el tenant `demo` y un owner `owner@demo.gymflow.pe`
(password inicial `Cambiar123!`) al arrancar en Development.

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
- ⏭️ **Fase 2** — Pagos (siguiente): pago manual, IPaymentGateway/Culqi, morosidad, Hangfire.
- Deploy Railway/Neon: pendiente de credenciales.
