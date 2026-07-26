import 'package:flutter/material.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'services/auth_service.dart';
import 'services/member_api.dart';
import 'screens/login_screen.dart';
import 'screens/home_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initializeDateFormatting('es');
  final prefs = await SharedPreferences.getInstance();
  final auth = AuthService(prefs);
  runApp(GymFlowApp(auth: auth, api: MemberApi(auth)));
}

class GymFlowApp extends StatelessWidget {
  const GymFlowApp({super.key, required this.auth, required this.api});

  final AuthService auth;
  final MemberApi api;

  @override
  Widget build(BuildContext context) {
    final scheme = ColorScheme.fromSeed(
      seedColor: const Color(0xFF10B981),
      brightness: Brightness.light,
    );
    return MaterialApp(
      title: 'GymFlow',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(colorScheme: scheme, useMaterial3: true),
      home: ListenableBuilder(
        listenable: auth,
        builder: (context, _) => auth.isAuthenticated
            ? HomeScreen(auth: auth, api: api)
            : LoginScreen(auth: auth),
      ),
    );
  }
}
