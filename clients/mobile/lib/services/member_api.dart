import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config.dart';
import '../models.dart';
import 'auth_service.dart';

/// Cliente de los endpoints del miembro (`/me/*`). El tenant viaja en el claim del JWT,
/// así que estas llamadas solo necesitan el token — no el subdominio.
class MemberApi {
  MemberApi(this._auth);

  final AuthService _auth;

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        if (_auth.token != null) 'Authorization': 'Bearer ${_auth.token}',
      };

  Future<T> _get<T>(String path, T Function(dynamic body) parse) async {
    final res = await http.get(Uri.parse('${AppConfig.apiBaseUrl}$path'), headers: _headers);
    if (res.statusCode == 401) {
      await _auth.logout();
      throw ApiException('Sesión expirada.');
    }
    if (res.statusCode == 204 || res.body.isEmpty) return parse(null);
    if (res.statusCode >= 400) throw ApiException('Error del servidor (${res.statusCode}).');
    return parse(jsonDecode(res.body));
  }

  /// Membresía vigente del miembro, o null si no tiene.
  Future<Membership?> getMembership() =>
      _get('/me/membership', (body) => body == null ? null : Membership.fromJson(body as Map<String, dynamic>));

  Future<List<CheckIn>> getCheckIns() => _get(
        '/me/checkins',
        (body) => ((body as List?) ?? []).map((e) => CheckIn.fromJson(e as Map<String, dynamic>)).toList(),
      );

  Future<List<Payment>> getPayments() => _get(
        '/me/payments',
        (body) => ((body as List?) ?? []).map((e) => Payment.fromJson(e as Map<String, dynamic>)).toList(),
      );

  /// Auto check-in desde la app. Devuelve el aforo resultante.
  Future<CheckInResult> selfCheckIn() async {
    final res = await http.post(Uri.parse('${AppConfig.apiBaseUrl}/me/checkins'), headers: _headers);
    if (res.statusCode == 401) {
      await _auth.logout();
      throw ApiException('Sesión expirada.');
    }
    if (res.statusCode >= 400) throw ApiException('No se pudo registrar el ingreso (${res.statusCode}).');
    final json = jsonDecode(res.body) as Map<String, dynamic>;
    return CheckInResult(
      checkIn: CheckIn.fromJson(json['checkIn'] as Map<String, dynamic>),
      occupancy: json['occupancy'] as int? ?? 0,
    );
  }

  // --- Clases (Fase 7) ---

  /// Próximas clases con cupo y el estado de reserva del propio miembro.
  Future<List<MemberClass>> getClasses() => _get(
        '/me/classes',
        (body) => ((body as List?) ?? []).map((e) => MemberClass.fromJson(e as Map<String, dynamic>)).toList(),
      );

  /// Reserva una clase (o entra a lista de espera si está llena). Devuelve el estado obtenido.
  Future<ReservationStatus> reserveClass(String sessionId) =>
      _postForStatus('/me/classes/$sessionId/reserve');

  /// Cancela la reserva del miembro para una clase.
  Future<void> cancelReservation(String sessionId) =>
      _postForStatus('/me/classes/$sessionId/cancel');

  Future<ReservationStatus> _postForStatus(String path) async {
    final res = await http.post(Uri.parse('${AppConfig.apiBaseUrl}$path'), headers: _headers);
    if (res.statusCode == 401) {
      await _auth.logout();
      throw ApiException('Sesión expirada.');
    }
    if (res.statusCode >= 400) {
      String message = 'No se pudo completar la operación (${res.statusCode}).';
      try {
        final body = jsonDecode(res.body) as Map<String, dynamic>;
        if (body['detail'] is String) message = body['detail'] as String;
      } catch (_) {}
      throw ApiException(message);
    }
    if (res.body.isEmpty) return ReservationStatus.none;
    final json = jsonDecode(res.body) as Map<String, dynamic>;
    // /reserve devuelve { status, session }; /cancel devuelve la sesión (sin status).
    return ReservationStatus.fromCode(json['status'] as int?);
  }
}

class CheckInResult {
  const CheckInResult({required this.checkIn, required this.occupancy});
  final CheckIn checkIn;
  final int occupancy;
}
