import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models.dart';
import '../../services/member_api.dart';

class MembershipTab extends StatefulWidget {
  const MembershipTab({super.key, required this.api});

  final MemberApi api;

  @override
  State<MembershipTab> createState() => _MembershipTabState();
}

class _MembershipTabState extends State<MembershipTab> {
  late Future<Membership?> _future;

  @override
  void initState() {
    super.initState();
    _future = widget.api.getMembership();
  }

  Future<void> _reload() async {
    setState(() => _future = widget.api.getMembership());
    await _future;
  }

  Color _statusColor(BuildContext context, MembershipStatus status) {
    return switch (status) {
      MembershipStatus.active => Colors.green,
      MembershipStatus.frozen => Colors.blueGrey,
      MembershipStatus.expired => Colors.red,
      MembershipStatus.overdue => Colors.orange,
      MembershipStatus.unknown => Theme.of(context).hintColor,
    };
  }

  @override
  Widget build(BuildContext context) {
    final dateFmt = DateFormat('dd MMM yyyy', 'es');
    return RefreshIndicator(
      onRefresh: _reload,
      child: FutureBuilder<Membership?>(
        future: _future,
        builder: (context, snap) {
          if (snap.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) {
            return _MessageList(icon: Icons.error_outline, text: '${snap.error}');
          }
          final m = snap.data;
          if (m == null) {
            return const _MessageList(
              icon: Icons.card_membership_outlined,
              text: 'No tienes una membresía vigente.\nAcércate a recepción para activarla.',
            );
          }

          final theme = Theme.of(context);
          final color = _statusColor(context, m.status);
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(20),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Expanded(child: Text(m.planName, style: theme.textTheme.headlineSmall)),
                          Chip(
                            label: Text(m.status.label, style: const TextStyle(color: Colors.white)),
                            backgroundColor: color,
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      _row(context, 'Inicio', dateFmt.format(m.startDate)),
                      _row(context, 'Vence', dateFmt.format(m.endDate)),
                      _row(context, 'Precio', 'S/ ${m.price.toStringAsFixed(2)}'),
                      const Divider(height: 28),
                      if (m.status == MembershipStatus.active && m.daysLeft >= 0)
                        Text(
                          m.daysLeft == 0
                              ? 'Vence hoy'
                              : 'Te quedan ${m.daysLeft} día${m.daysLeft == 1 ? '' : 's'}',
                          style: theme.textTheme.titleMedium?.copyWith(color: color),
                        )
                      else if (m.status == MembershipStatus.overdue)
                        Text('Membresía morosa: regulariza tu pago.',
                            style: theme.textTheme.titleMedium?.copyWith(color: color)),
                    ],
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _row(BuildContext context, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Theme.of(context).hintColor)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}

class _MessageList extends StatelessWidget {
  const _MessageList({required this.icon, required this.text});
  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    // ListView para que RefreshIndicator siga funcionando aunque no haya datos.
    return ListView(
      children: [
        const SizedBox(height: 120),
        Icon(icon, size: 56, color: Theme.of(context).hintColor),
        const SizedBox(height: 16),
        Text(text, textAlign: TextAlign.center, style: Theme.of(context).textTheme.bodyLarge),
      ],
    );
  }
}
