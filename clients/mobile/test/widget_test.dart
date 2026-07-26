import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:gymflow_app/main.dart';
import 'package:gymflow_app/services/auth_service.dart';
import 'package:gymflow_app/services/member_api.dart';

void main() {
  testWidgets('Sin sesión, muestra la pantalla de login', (WidgetTester tester) async {
    SharedPreferences.setMockInitialValues({});
    final prefs = await SharedPreferences.getInstance();
    final auth = AuthService(prefs);

    await tester.pumpWidget(GymFlowApp(auth: auth, api: MemberApi(auth)));

    expect(find.text('GymFlow'), findsOneWidget);
    expect(find.text('Ingresar'), findsOneWidget);
    expect(find.text('Gimnasio'), findsOneWidget);
  });
}
