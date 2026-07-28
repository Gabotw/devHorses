// DTOs del lado del miembro. Reflejan el contrato de la API `/me/*` y `/member-auth`.

class MemberSession {
  const MemberSession({
    required this.token,
    required this.memberId,
    required this.fullName,
  });

  final String token;
  final String memberId;
  final String fullName;

  factory MemberSession.fromLogin(Map<String, dynamic> json) => MemberSession(
        token: json['accessToken'] as String,
        memberId: json['memberId'] as String,
        fullName: json['fullName'] as String? ?? '',
      );

  Map<String, dynamic> toJson() => {
        'token': token,
        'memberId': memberId,
        'fullName': fullName,
      };

  factory MemberSession.fromStored(Map<String, dynamic> json) => MemberSession(
        token: json['token'] as String,
        memberId: json['memberId'] as String,
        fullName: json['fullName'] as String? ?? '',
      );
}

/// Estados de membresía (coinciden con MembershipStatus del backend).
enum MembershipStatus {
  active(1, 'Activa'),
  frozen(2, 'Congelada'),
  expired(3, 'Vencida'),
  overdue(4, 'Morosa'),
  unknown(0, 'Desconocida');

  const MembershipStatus(this.code, this.label);
  final int code;
  final String label;

  static MembershipStatus fromCode(int? code) =>
      MembershipStatus.values.firstWhere((s) => s.code == code, orElse: () => MembershipStatus.unknown);
}

class Membership {
  const Membership({
    required this.id,
    required this.planName,
    required this.price,
    required this.startDate,
    required this.endDate,
    required this.status,
  });

  final String id;
  final String planName;
  final double price;
  final DateTime startDate;
  final DateTime endDate;
  final MembershipStatus status;

  factory Membership.fromJson(Map<String, dynamic> json) => Membership(
        id: json['id'] as String,
        planName: json['planName'] as String? ?? 'Plan',
        price: (json['priceAtPurchase'] as num?)?.toDouble() ?? 0,
        startDate: DateTime.parse(json['startDate'] as String),
        endDate: DateTime.parse(json['endDate'] as String),
        status: MembershipStatus.fromCode(json['status'] as int?),
      );

  int get daysLeft => endDate.difference(DateTime.now()).inDays;
}

class CheckIn {
  const CheckIn({
    required this.id,
    required this.occurredAtUtc,
    required this.isValid,
    this.reason,
  });

  final String id;
  final DateTime occurredAtUtc;
  final bool isValid;
  final String? reason;

  factory CheckIn.fromJson(Map<String, dynamic> json) => CheckIn(
        id: json['id'] as String,
        occurredAtUtc: DateTime.parse(json['occurredAtUtc'] as String).toLocal(),
        isValid: json['isValid'] as bool? ?? false,
        reason: json['reason'] as String?,
      );
}

enum PaymentMethodLabel {
  cash(1, 'Efectivo'),
  culqi(2, 'Culqi'),
  izipay(3, 'Izipay'),
  unknown(0, '—');

  const PaymentMethodLabel(this.code, this.label);
  final int code;
  final String label;

  static PaymentMethodLabel fromCode(int? code) =>
      PaymentMethodLabel.values.firstWhere((m) => m.code == code, orElse: () => PaymentMethodLabel.unknown);
}

/// Estado de la reserva del miembro (coincide con ClassReservationStatus del backend).
enum ReservationStatus {
  booked(1, 'Con cupo'),
  waitlisted(2, 'En espera'),
  cancelled(3, 'Cancelada'),
  attended(4, 'Asistió'),
  none(0, '');

  const ReservationStatus(this.code, this.label);
  final int code;
  final String label;

  static ReservationStatus fromCode(int? code) =>
      ReservationStatus.values.firstWhere((s) => s.code == code, orElse: () => ReservationStatus.none);
}

/// Sesión de clase vista por el miembro: ocupación + su propio estado de reserva.
class MemberClass {
  const MemberClass({
    required this.id,
    required this.name,
    required this.instructorName,
    required this.startsAt,
    required this.durationMinutes,
    required this.capacity,
    required this.bookedCount,
    required this.availableSpots,
    required this.myStatus,
  });

  final String id;
  final String name;
  final String? instructorName;
  final DateTime startsAt;
  final int durationMinutes;
  final int capacity;
  final int bookedCount;
  final int availableSpots;
  final ReservationStatus myStatus;

  bool get isReserved =>
      myStatus == ReservationStatus.booked || myStatus == ReservationStatus.waitlisted;
  bool get isFull => availableSpots <= 0;

  factory MemberClass.fromJson(Map<String, dynamic> json) => MemberClass(
        id: json['id'] as String,
        name: json['name'] as String? ?? 'Clase',
        instructorName: json['instructorName'] as String?,
        startsAt: DateTime.parse(json['startsAtUtc'] as String).toLocal(),
        durationMinutes: json['durationMinutes'] as int? ?? 0,
        capacity: json['capacity'] as int? ?? 0,
        bookedCount: json['bookedCount'] as int? ?? 0,
        availableSpots: json['availableSpots'] as int? ?? 0,
        myStatus: ReservationStatus.fromCode(json['myStatus'] as int?),
      );
}

class Payment {
  const Payment({
    required this.id,
    required this.amount,
    required this.method,
    required this.date,
  });

  final String id;
  final double amount;
  final PaymentMethodLabel method;
  final DateTime date;

  factory Payment.fromJson(Map<String, dynamic> json) => Payment(
        id: json['id'] as String,
        amount: (json['amount'] as num?)?.toDouble() ?? 0,
        method: PaymentMethodLabel.fromCode(json['method'] as int?),
        date: DateTime.parse((json['paidAtUtc'] ?? json['createdAtUtc']) as String).toLocal(),
      );
}
