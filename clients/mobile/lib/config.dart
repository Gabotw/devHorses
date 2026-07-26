/// Configuración de la app.
///
/// [apiBaseUrl] apunta al backend GymFlow. Notas por plataforma en desarrollo:
///  - Web / Windows / iOS Simulator: http://localhost:5066/api
///  - Emulador Android: http://10.0.2.2:5066/api  (10.0.2.2 = host desde el emulador)
///  - Dispositivo físico: http://`IP-LAN-de-tu-PC`:5066/api
///
/// Se puede sobrescribir en tiempo de compilación:
///   flutter run --dart-define=API_BASE_URL=https://api.gymflow.pe/api
class AppConfig {
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5066/api',
  );
}
