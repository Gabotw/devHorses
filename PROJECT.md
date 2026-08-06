# PROJECT.md — GymFlow (SaaS Multi-Tenant para Gimnasios)

> **Documento fuente de verdad.** Contexto obligatorio para Claude Code. Léelo completo antes de escribir código. Referenciado desde `CLAUDE.md` en la raíz del repo.

---

## 1. Visión del producto

SaaS multi-tenant **operado por la recepción del gimnasio**: registra clientes y sus
membresías, controla la asistencia con un **código de 4 dígitos**, avisa por **WhatsApp**
cuando una membresía está por vencer (el pago se hace fuera del sistema, en el gimnasio) y
ofrece dashboard + reportes. **No hay app para el miembro ni cobros dentro del sistema** — el
software es solo para el gimnasio.

**Modelo de negocio:** SaaS B2B. Cada gimnasio es un *tenant*. Los miembros del gimnasio son
datos que gestiona la recepción (no son usuarios del software ni pagan a la plataforma).

**Diferenciador:** simplicidad radical para gimnasios pequeños/medianos de LATAM que hoy usan Excel, WhatsApp o software caro y complejo. UX en lenguaje humano, no técnico.

### Segmento objetivo (validación)
Gimnasios independientes de barrio en Lima (1–3 sedes), dueño operador, que hoy controlan membresías a mano. Empezar validando con **1–2 gimnasios reales** antes de escalar features.

---

## 2. Alcance

| Módulo | Estado | Notas |
|---|---|---|
| Membresías + planes | ✅ | Núcleo. Estados: activa / vencida / congelada / morosa |
| Registro de pago en recepción | ✅ | Manual (efectivo). El cobro real ocurre fuera del sistema |
| Check-in / asistencia | ✅ | En recepción (panel web) + aforo en tiempo real |
| Reportes / dashboard | ✅ | Ingresos, churn, ocupación, morosidad |
| **Check-in por código de 4 dígitos** | ✅ | Cada miembro tiene un código; la recepción lo teclea |
| **Vencimientos + aviso por WhatsApp (manual)** | ✅ | Lista "por vencer" + botón `wa.me` con mensaje prellenado |
| **Personal (staff)** | ✅ | CRUD de usuarios del panel con roles (owner/admin/recepción), reset de contraseña y activar/desactivar |
| **WhatsApp automático (Meta Cloud API)** | ⏭️ Siguiente | Envío automático + recordatorio N días antes |
| App móvil de miembros | ❌ Fuera de alcance | El software es solo para el gimnasio |
| Pagos online / pasarela | ❌ Fuera de alcance | El pago no se hace dentro del sistema |
| Reserva de clases | ❌ Fuera de alcance | |
| Billing del SaaS (cobro a gimnasios) | ❌ Fuera de alcance | Se retomará si hay tracción |

**Principio:** simplicidad radical para la recepción. Solo lo que el gimnasio usa a diario.

---

## 3. Stack tecnológico (confirmado)

### Backend
- **.NET 10** (LTS-track largo; .NET 8 termina soporte nov 2026)
- **Clean Architecture** con patrón **Hexagonal / Ports & Adapters** para aislar pasarelas de pago e infraestructura
- **EF Core 10** con **global query filters** para multi-tenancy
- **Hangfire** — jobs en background (cortes de morosidad, recordatorios de vencimiento)
- **SignalR** — recepción en tiempo real (aforo, check-ins en vivo)
- **Redis** — caché de aforo/estado en tiempo real y rate limiting

### Frontend web (panel de recepción/admin)
- **Angular** + **PrimeNG**
- Desktop-first responsive (recepción y admin operan desde PC). Es el único cliente:
  no hay app de miembro.

### Datos
- **PostgreSQL** (Neon) — base principal
- **Redis** (Upstash o Railway) — caché/tiempo real

### Infraestructura
- **Railway** (API + workers Hangfire) + **Neon** (Postgres) — bajo overhead ops para fundador solo en etapa de validación
- Migrar a VPS/infra dedicada solo si la escala lo justifica

### Pagos
- El pago se cobra **fuera del sistema** (en el gimnasio). La recepción solo **registra** que
  el pago ocurrió (efectivo), lo que renueva/mantiene la membresía. Sin pasarela de pago.

### Notificaciones (WhatsApp)
- Aviso de vencimiento de membresía. **Fase 1:** enlace `wa.me` que la recepción envía con un
  clic (cero costo/API, ideal para validar). **Fase 2:** Meta WhatsApp Cloud API para envío
  automático (recordar implementarla cuando haya tracción).

---

## 4. Arquitectura de capas

```
src/
├── GymFlow.Domain/          → Entidades, Value Objects, reglas de negocio puras
│                              (Money=Decimal SIEMPRE, sin dependencias externas)
├── GymFlow.Application/      → Casos de uso, DTOs, interfaces (PUERTOS)
│                              CQRS opcional vía MediatR
├── GymFlow.Infrastructure/   → ADAPTADORES: EF Core, Hangfire, Redis, WhatsApp (futuro)
├── GymFlow.Api/              → Controllers, middleware de tenant, auth, SignalR hubs
└── clients/
    └── web/    (Angular)     → Panel de recepción + admin (único cliente)
```

**Regla de dependencias:** Domain no depende de nada. Application depende solo de Domain. Infrastructure y Api dependen hacia adentro. La integración externa (p.ej. WhatsApp Cloud API) vivirá **solo** en Infrastructure detrás de un puerto en Application.

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

1. **Tenancy** — gimnasios (tenants) y su resolución multi-tenant
2. **Miembros** — perfil, estado (activo/inactivo), código de acceso de 4 dígitos, historial
3. **Membresías** — planes del gimnasio, precios, duración, congelamientos, renovaciones,
   vencimiento (cuánto falta para caducar)
4. **Pagos** — registro manual del pago cobrado en recepción, morosidad
5. **Acceso / Check-in** — asistencia por código de 4 dígitos, aforo en tiempo real,
   validación de membresía vigente
6. **Reportes** — ingresos, churn/retención, ocupación por hora, morosidad
7. **Staff & Roles** — recepcionistas, admin, dueño (RBAC por tenant)
8. **Notificaciones** — aviso de vencimiento por WhatsApp (enlace `wa.me`; luego Cloud API)

---

## 7. Modelo de datos (esqueleto)

```
Tenant (Gym)
  ├─ id, nombre, subdominio, zona_horaria, estado
Member
  ├─ id, tenant_id, nombre, doc, telefono, email, estado, foto_url
  │   codigo_acceso (4 dígitos, único por tenant)
MembershipPlan
  ├─ id, tenant_id, nombre, precio (Decimal), duracion_dias, accesos_mes
Membership
  ├─ id, tenant_id, member_id, plan_id, inicio, fin, estado
  │   (activa|vencida|congelada|morosa), congelada_desde/hasta
Payment
  ├─ id, tenant_id, member_id, membership_id, monto (Decimal), metodo
  │   (efectivo), estado, fecha  ← registro manual del pago cobrado en recepción
CheckIn
  ├─ id, tenant_id, member_id, timestamp, metodo (recepcion), valido
StaffUser
  ├─ id, tenant_id, nombre, email, rol (owner|admin|reception)
```

**No-negociables:**
- Dinero **siempre** `Decimal`, nunca `float/double`.
- `TenantId` en **toda** tabla del dominio de negocio.
- Fechas en UTC en DB; se muestran en zona del tenant (default `America/Lima`).

---

## 8. Jobs en background (Hangfire)

- **Corte de morosidad** — diario: marca membresías vencidas como morosas. *(implementado)*
- **Recordatorios de vencimiento** — N días antes, para el aviso por WhatsApp. *(pendiente)*
- **Reporte semanal al dueño** — resumen de ingresos/churn. *(pendiente, opcional)*

---

## 9. Roadmap

### Ya construido (base operativa de recepción)
- **Fundaciones** — .NET 10, 4 capas, multi-tenancy (middleware + global query filters),
  JWT + RBAC (staff), seed de 1 tenant, CI, deploy Neon/Render.
- **Miembros & Membresías** — CRUD de miembros, planes, membresías con estados
  (activa/vencida/congelada/morosa). Panel Angular.
- **Pagos en recepción** — registro manual (efectivo) e historial; morosidad + job Hangfire.
- **Check-in & Asistencia** — check-in en recepción **por código de 4 dígitos** (o búsqueda
  por nombre/DNI) validando membresía vigente; aforo en tiempo real (SignalR + Redis opcional).
- **Reportes & Dashboard** — ingresos, morosidad, churn, ocupación por hora.
- **Vencimientos + aviso por WhatsApp (manual)** — página con las membresías por vencer y botón
  que abre `wa.me` con el mensaje prellenado para que la recepción lo envíe con un clic.
- **Personal (staff)** — CRUD de usuarios del panel con roles (owner/admin/recepción), reset de
  contraseña y activar/desactivar (`api/staff/*`, policy Manager). Guarda el último owner activo.

### Siguiente (núcleo del enfoque actual)
1. **WhatsApp automático** — Meta WhatsApp Cloud API (envío automático del aviso, hoy manual) +
   job de recordatorio N días antes. Requiere WhatsApp Business verificado y plantillas aprobadas.

### Fuera de alcance (por ahora)
App de miembro, pagos online/pasarela, reserva de clases, billing del SaaS a los gimnasios.

---

## 10. Decisiones clave & pendientes

- **Canal de aviso de vencimiento:** WhatsApp. Empezar con enlace `wa.me` (manual, un clic
  desde recepción); migrar a **Meta WhatsApp Cloud API** (envío automático) cuando haya
  tracción. Requiere número de WhatsApp Business verificado y plantillas aprobadas.
- **Código de acceso de 4 dígitos:** *(resuelto)* aleatorio único por tenant, generado al crear
  el miembro; unicidad garantizada por índice único parcial; regenerable desde el panel.
- **Zona horaria por tenant** desde el inicio (LATAM multi-país a futuro).
- **RBAC por tenant** — roles owner/admin/reception; el owner no puede ver otros tenants.

---

## 11. Principios de arquitectura (recordatorios)

- Lógica de negocio en Domain/Application; infraestructura afuera y detrás de puertos.
- Integraciones externas (WhatsApp) aisladas tras un puerto en Application.
- Validar antes de construir; observar usuarios reales antes de asumir features.
- Simplicidad y UX en lenguaje humano son el diferenciador real, no la paridad de features.
- Multi-tenancy y `Decimal` para dinero son no-negociables desde el día uno.
