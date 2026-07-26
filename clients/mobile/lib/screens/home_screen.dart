import 'package:flutter/material.dart';

import '../services/auth_service.dart';
import '../services/member_api.dart';
import 'tabs/membership_tab.dart';
import 'tabs/checkin_tab.dart';
import 'tabs/history_tab.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.auth, required this.api});

  final AuthService auth;
  final MemberApi api;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _index = 0;

  @override
  Widget build(BuildContext context) {
    final firstName = widget.auth.session?.fullName.split(' ').first ?? '';
    final tabs = [
      MembershipTab(api: widget.api),
      CheckInTab(api: widget.api),
      HistoryTab(api: widget.api),
    ];
    final titles = ['Hola, $firstName', 'Check-in', 'Historial'];

    return Scaffold(
      appBar: AppBar(
        title: Text(titles[_index]),
        actions: [
          IconButton(
            tooltip: 'Salir',
            icon: const Icon(Icons.logout),
            onPressed: () => widget.auth.logout(),
          ),
        ],
      ),
      body: IndexedStack(index: _index, children: tabs),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (i) => setState(() => _index = i),
        destinations: const [
          NavigationDestination(icon: Icon(Icons.card_membership_outlined), selectedIcon: Icon(Icons.card_membership), label: 'Mi plan'),
          NavigationDestination(icon: Icon(Icons.login_outlined), selectedIcon: Icon(Icons.login), label: 'Check-in'),
          NavigationDestination(icon: Icon(Icons.history), label: 'Historial'),
        ],
      ),
    );
  }
}
