# PROJECT.md — GymFlow (SaaS Multi-Tenant para Gimnasios)

> **Documento fuente de verdad.** Contexto obligatorio para Claude Code. Léelo completo antes de escribir código. Referenciado desde `CLAUDE.md` en la raíz del repo.

---

## 1. Visión del producto

SaaS multi-tenant que permite a **cualquier gimnasio** gestionar sus membresías, cobros, asistencia y reportes desde un panel web, y ofrecer a sus miembros una **app móvil** para ver su membresía, reservar acceso y revisar su historial.

**Modelo de negocio:** SaaS B2B. Cada gimnasio es un *tenant* que paga una suscripción a la plataforma. Los miembros del gimnasio son usuarios finales del tenant (no pagan a la plataforma directamente).

**Diferenciador:** simplicidad radical para gimnasios pequeños/medianos de LATAM que hoy usan Excel, WhatsApp o software caro y complejo. UX en lenguaje humano, no técnico.

### Segmento objetivo (validación)
Gimnasios independientes de barrio en Lima (1–3 sedes), dueño operador, que hoy controlan membresías a mano. Empezar validando con **1–2 gimnasios reales** antes de escalar features.

---

## 2. Alcance del MVP

| Módulo | En MVP | Notas |
|---|---|---|
| Membresías + planes | ✅ | Núcleo. Estados: activa / vencida / congelada / morosa |
| Pagos / cobros | ✅ | Recurrentes vía Culqi/Izipay + registro manual (efectivo) |
| Check-in / asistencia | ✅ | Recepción web + app del miembro |
| Reportes / dashboard | ✅ | Ingresos, churn, ocupación, morosidad |
| App móvil miembros | ✅ | Ver membresía, check-in, historial, reservas básicas |
| Reserva de clases | ⏳ Post-MVP | Se secuencia después del núcleo |
| Torniquete / hardware | ⏳ Post-MVP | Integración física deferida |
| Comisiones de staff | ⏳ Post-MVP | |

**Principio:** monetización de la plataforma (billing del SaaS) se activa recién con tracción más allá de los gimnasios de validación.

---

## 3. Stack tecnológico (confirmado)

### Backend
- **.NET 10** (LTS-track largo; .NET 8 termina soporte nov 2026)
- **Clean Architecture** con patrón **Hexagonal / Ports & Adapters** para aislar pasarelas de pago e infraestructura
- **EF Core 10** con **global query filters** para multi-tenancy
- **Hangfire** — jobs en background (renovaciones, recordatorios, cortes de morosidad, reintentos de cobro)
- **SignalR** — recepción en tiempo real (aforo, check-ins en vivo)
- **Redis** — caché de aforo/estado en tiempo real y rate limiting

### Frontend web (panel admin/recepción)
- **Angular** + **PrimeNG**
- Desktop-first responsive (recepción y admin operan desde PC)

### App móvil (miembros)
- **Flutter** — un solo codebase iOS + Android, multi-rol (miembro; opcionalmente staff)
- Se conecta a la misma API REST

### Datos
- **PostgreSQL** (Neon) — base principal
- **Redis** (Upstash o Railway) — caché/tiempo real

### Infraestructura
- **Railway** (API + workers Hangfire) + **Neon** (Postgres) — bajo overhead ops para fundador solo en etapa de validación
- Migrar a VPS/infra dedicada solo si la escala lo justifica

### Pagos
- **Culqi / Izipay** — en dos contextos distintos:
  1. **Cobro de membresías del gimnasio** (usuario final paga al gimnasio) → configurable por tenant
  2. **Billing de la suscripción SaaS** (gimnasio paga a la plataforma) → deferido hasta tracción

---

## 4. Arquitectura de capas

```
src/
├── GymFlow.Domain/          → Entidades, Value Objects, reglas de negocio puras
│                              (Money=Decimal SIEMPRE, sin dependencias externas)
├── GymFlow.Application/      → Casos de uso, DTOs, interfaces (PUERTOS)
│                              CQRS opcional vía MediatR
├── GymFlow.Infrastructure/   → ADAPTADORES: EF Core, Hangfire, Redis,
│                              pasarelas de pago, email/SMS
├── GymFlow.Api/              → Controllers, middleware de tenant, auth, SignalR hubs
└── clients/
    ├── web/    (Angular)     → Panel admin + recepción
    └── mobile/ (Flutter)     → App de miembros
```

**Regla de dependencias:** Domain no depende de nada. Application depende solo de Domain. Infrastructure y Api dependen hacia adentro. Las pasarelas de pago viven **solo** en Infrastructure detrás de una interfaz `IPaymentGateway` (puerto) — igual que el aislamiento de SUNAT en el invoicing SaaS.

---

## 5. Multi-tenancy (desde el día uno)

**Estrategia:** Single database + `TenantId` en cada tabla + **EF Core global query filters**.

- Un gimnasio = un tenant.
- Resolución de tenant en un **middleware** antes del pipeline: por subdominio (`acme.gymflow.pe`) o header `X-Tenant-Id` (app móvil).
- `TenantId` se inyecta vía `ITenantProvider` (scoped) y EF lo aplica automáticamente en cada query.
- **Nunca** confiar en el `TenantId` que venga del cliente sin validar contra el usuario autenticado.

> Retrofittear multi-tenancy es carísimo. Se arquitecta desde el inicio aunque el primer "tenant" sea un solo gimnasio de validación.

---

## 6. Bounded contexts (dominios core)

1. **Tenancy & Billing** — gimnasios (tenants), planes de la plataforma, suscripción SaaS
2. **Miembros** — perfil, estado (activo/moroso/congelado/inactivo), historial
3. **Membresías** — planes del gimnasio, precios, duración, congelamientos, renovaciones
4. **Pagos** — cobros recurrentes, pagos manuales, morosidad, recibos, reintentos
5. **Acceso / Check-in** — asistencia, aforo en tiempo real, validación de membresía activa
6. **Reportes** — ingresos, churn/retención, ocupación por hora, morosidad
7. **Staff & Roles** — recepcionistas, admin, dueño (RBAC por tenant)
8. **Clases** *(post-MVP)* — horarios, cupos, reservas, waitlist

---

## 7. Modelo de datos (esqueleto)

```
Tenant (Gym)
  ├─ id, nombre, subdominio, estado_suscripcion, plan_saas
Member
  ├─ id, tenant_id, nombre, doc, telefono, email, estado, foto_url
MembershipPlan
  ├─ id, tenant_id, nombre, precio (Decimal), duracion_dias, accesos_mes
Membership
  ├─ id, tenant_id, member_id, plan_id, inicio, fin, estado
  │   (activa|vencida|congelada|morosa), congelada_desde/hasta
Payment
  ├─ id, tenant_id, member_id, membership_id, monto (Decimal), metodo
  │   (culqi|izipay|efectivo), estado, gateway_ref, fecha
CheckIn
  ├─ id, tenant_id, member_id, timestamp, metodo (recepcion|app), valido
StaffUser
  ├─ id, tenant_id, nombre, email, rol (owner|admin|reception)
```

**No-negociables:**
- Dinero **siempre** `Decimal`, nunca `float/double`.
- `TenantId` en **toda** tabla del dominio de negocio.
- Fechas en UTC en DB; se muestran en zona del tenant (default `America/Lima`).

---

## 8. Jobs en background (Hangfire)

- **Corte de morosidad** — diario: marca membresías vencidas como morosas.
- **Recordatorios de vencimiento** — N días antes (email/WhatsApp).
- **Cobro recurrente** — intenta cargar la membresía vía pasarela; reintentos con backoff.
- **Reporte semanal al dueño** — resumen de ingresos/churn.
- **Limpieza de check-ins** antiguos / agregados para dashboard.

---

## 9. Roadmap por fases

### Fase −1 — Validación (pre-código)
- Sesiones de observación con 1–2 gimnasios reales usando su método actual (Excel/cuaderno/WhatsApp).
- Documentar fricciones exactas como input de diseño.
- **No escribir código hasta terminar esto.**

### Fase 0 — Fundaciones
- Solución .NET 10 con las 4 capas + Clean Architecture.
- Multi-tenancy (middleware + global query filters) funcionando con seed de 1 tenant.
- Auth (JWT) + RBAC básico.
- CI + deploy a Railway/Neon.

### Fase 1 — Miembros & Membresías
- CRUD de miembros, planes, membresías.
- Estados y transiciones (activa/vencida/congelada).
- Panel Angular básico.

### Fase 2 — Pagos
- Registro de pago manual (efectivo).
- Integración `IPaymentGateway` con Culqi (cobro de membresía).
- Morosidad + jobs Hangfire.

### Fase 3 — Check-in & Asistencia
- Check-in en recepción (web) validando membresía activa.
- Aforo en tiempo real (Redis + SignalR).
- Registro de asistencia.

### Fase 4 — App móvil (Flutter)
- Login del miembro.
- Ver membresía/estado/vencimiento.
- Check-in desde la app.
- Historial de asistencia y pagos.

### Fase 5 — Reportes & Dashboard
- Ingresos, morosidad, churn, ocupación por hora.

### Fase 6 — Billing del SaaS
- Suscripción de tenants a la plataforma (activar solo con tracción).

### Fase 7 — Clases (post-MVP)
- Horarios, cupos, reservas, waitlist.

---

## 10. Decisiones clave & pendientes

- **Verificar** SDK/paquete de Culqi compatible con .NET 10 antes de Fase 2.
- **Definir** canal de recordatorios: email (barato) vs WhatsApp Business API (mejor conversión, más caro) — evaluar en Fase 1.
- **Zona horaria por tenant** desde el inicio (LATAM multi-país a futuro).
- **RBAC por tenant** — roles owner/admin/reception; el owner no puede ver otros tenants.

---

## 11. Principios de arquitectura (recordatorios)

- Lógica de negocio en Domain/Application; infraestructura afuera y detrás de puertos.
- Pasarelas de pago aisladas tras `IPaymentGateway` (igual que el aislamiento SUNAT).
- Validar antes de construir; observar usuarios reales antes de asumir features.
- Simplicidad y UX en lenguaje humano son el diferenciador real, no la paridad de features.
- Multi-tenancy y `Decimal` para dinero son no-negociables desde el día uno.
