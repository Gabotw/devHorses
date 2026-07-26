import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import '../config.dart';
import '../models.dart';

class ApiException implements Exception {
  ApiException(this.message);
  final String message;
  @override
  String toString() => message;
}

/// Estado de sesión del miembro. Persiste el JWT en SharedPreferences y notifica a la UI.
class AuthService extends ChangeNotifier {
  AuthService(this._prefs) {
    _restore();
  }

  static const _key = 'gymflow.member.session';
  final SharedPreferences _prefs;

  MemberSession? _session;
  MemberSession? get session => _session;
  bool get isAuthenticated => _session != null;
  String? get token => _session?.token;

  void _restore() {
    final raw = _prefs.getString(_key);
    if (raw == null) return;
    try {
      _session = MemberSession.fromStored(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      _session = null;
    }
  }

  /// Inicia sesión con gimnasio (subdominio) + documento (DNI) + contraseña.
  Future<void> login(String subdomain, String documentId, String password) async {
    final uri = Uri.parse('${AppConfig.apiBaseUrl}/member-auth/login');
    late final http.Response res;
    try {
      res = await http.post(
        uri,
        headers: {
          'Content-Type': 'application/json',
          'X-Tenant-Subdomain': subdomain.trim().toLowerCase(),
        },
        body: jsonEncode({'documentId': documentId.trim(), 'password': password}),
      );
    } catch (_) {
      throw ApiException('No se pudo conectar con el servidor.');
    }

    if (res.statusCode == 401 || res.statusCode == 400) {
      throw ApiException('Gimnasio, documento o contraseña incorrectos.');
    }
    if (res.statusCode >= 400) {
      throw ApiException('Error del servidor (${res.statusCode}).');
    }

    final json = jsonDecode(res.body) as Map<String, dynamic>;
    _session = MemberSession.fromLogin(json);
    await _prefs.setString(_key, jsonEncode(_session!.toJson()));
    notifyListeners();
  }

  Future<void> logout() async {
    _session = null;
    await _prefs.remove(_key);
    notifyListeners();
  }
}
