// Tipos que reflejan los DTOs del backend (Fase 1).

export type MemberStatus = 1 | 2; // Active = 1, Inactive = 2
export type MembershipStatus = 1 | 2 | 3 | 4; // Active, Frozen, Expired, Overdue

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

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
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
