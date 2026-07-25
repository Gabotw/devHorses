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
├── web/    (Angular)         → Fase 1+ (aún no scaffoldeado).
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

## Estado del roadmap
- ✅ **Fase 0** — Fundaciones: 4 capas, multi-tenancy, JWT + RBAC, seed, migración inicial, CI.
- ⏭️ **Fase 1** — Miembros & Membresías (siguiente).
- Deploy Railway/Neon: pendiente de credenciales.
