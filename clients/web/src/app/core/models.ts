// Tipos que reflejan los DTOs del backend (Fase 1).

export type MemberStatus = 1 | 2; // Active = 1, Inactive = 2
export type MembershipStatus = 1 | 2 | 3 | 4; // Active, Frozen, Expired, Overdue
export type PaymentMethod = 1; // Cash = 1
export type PaymentStatus = 1 | 2 | 3 | 4; // Pending, Completed, Failed, Refunded
export type CheckInMethod = 1 | 2; // Reception = 1, App = 2
export type StaffRole = 1 | 2 | 3; // Owner = 1, Admin = 2, Reception = 3

export interface LoginResult {
  accessToken: string;
  expiresAtUtc: string;
  userId: string;
  fullName: string;
  role: string;
}

export interface Member {
  id: string;
  fullName: string;
  documentId: string;
  phone?: string | null;
  email?: string | null;
  photoUrl?: string | null;
  status: MemberStatus;
  accessCode?: string | null;
  createdAtUtc: string;
}

export interface Plan {
  id: string;
  name: string;
  price: number;
  durationDays: number;
  monthlyAccesses?: number | null;
  isActive: boolean;
}

export interface Membership {
  id: string;
  memberId: string;
  planId: string;
  priceAtPurchase: number;
  startDate: string;
  endDate: string;
  status: MembershipStatus;
  frozenFrom?: string | null;
  frozenUntil?: string | null;
}

export interface Payment {
  id: string;
  memberId: string;
  membershipId?: string | null;
  amount: number;
  method: PaymentMethod;
  status: PaymentStatus;
  gatewayReference?: string | null;
  failureReason?: string | null;
  paidAtUtc?: string | null;
  notes?: string | null;
  createdAtUtc: string;
}

export interface CheckIn {
  id: string;
  memberId: string;
  memberName: string;
  method: CheckInMethod;
  occurredAtUtc: string;
  isValid: boolean;
  reason?: string | null;
}

export interface CheckInResult {
  checkIn: CheckIn;
  occupancy: number;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Staff: usuarios del panel (recepción/admin/owner).
export interface StaffUser {
  id: string;
  fullName: string;
  email: string;
  role: StaffRole;
  isActive: boolean;
  lastLoginAtUtc?: string | null;
  createdAtUtc: string;
}

export const STAFF_ROLE_LABEL: Record<number, string> = {
  1: 'Dueño',
  2: 'Administrador',
  3: 'Recepción',
};

// Vencimientos: membresías por vencer (o ya vencidas) para la lista de avisos.
export interface ExpiringMembership {
  membershipId: string;
  memberId: string;
  fullName: string;
  phone?: string | null;
  planName: string;
  endDate: string; // yyyy-MM-dd
  status: MembershipStatus;
  daysToExpiry: number; // negativo si ya venció
}

// Reportes / dashboard (Fase 5). Reflejan los DTOs de ReportService.
export interface ReportRange {
  from: string; // yyyy-MM-dd
  to: string;
}

export interface RevenuePoint {
  date: string;
  amount: number;
  count: number;
}

export interface RevenueByMethod {
  method: PaymentMethod;
  amount: number;
  count: number;
}

export interface MembershipStatusCount {
  status: MembershipStatus;
  count: number;
}

export interface OccupancyByHour {
  hour: number;
  count: number;
}

export interface Dashboard {
  range: ReportRange;
  revenueTotal: number;
  paymentsCount: number;
  averageTicket: number;
  overdueMemberships: number;
  overdueAmount: number;
  totalMembers: number;
  activeMembers: number;
  newMembers: number;
  activeMemberships: number;
  churnRate: number;
  retentionRate: number;
  revenueByDay: RevenuePoint[];
  revenueByMethod: RevenueByMethod[];
  membershipsByStatus: MembershipStatusCount[];
  occupancyByHour: OccupancyByHour[];
}

// Indexables por number para usarlos cómodamente desde plantillas.
export const MEMBER_STATUS_LABEL: Record<number, string> = {
  1: 'Activo',
  2: 'Inactivo',
};

export const MEMBERSHIP_STATUS_LABEL: Record<number, string> = {
  1: 'Activa',
  2: 'Congelada',
  3: 'Vencida',
  4: 'Morosa',
};

export const PAYMENT_METHOD_LABEL: Record<number, string> = {
  1: 'Efectivo',
};

export const PAYMENT_STATUS_LABEL: Record<number, string> = {
  1: 'Pendiente',
  2: 'Completado',
  3: 'Fallido',
  4: 'Reembolsado',
};
