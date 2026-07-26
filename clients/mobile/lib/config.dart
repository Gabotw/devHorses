/// Configuración de la app.
///
/// [apiBaseUrl] apunta al backend GymFlow. Por defecto usa el backend desplegado en
/// Render, así la app corre sin configuración extra.
///
/// Para apuntar a un backend local durante el desarrollo, sobrescribe en compilación:
///   flutter run --dart-define=API_BASE_URL=http://localhost:5066/api
///   (emulador Android usa http://10.0.2.2:5066/api; el host desde el emulador)
class AppConfig {
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://gymflow-api-rrtn.onrender.com/api',
  );
}
