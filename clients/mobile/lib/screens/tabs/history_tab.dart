import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models.dart';
import '../../services/member_api.dart';

class HistoryTab extends StatelessWidget {
  const HistoryTab({super.key, required this.api});

  final MemberApi api;

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Column(
        children: [
          const TabBar(
            tabs: [
              Tab(text: 'Asistencia', icon: Icon(Icons.event_available_outlined)),
              Tab(text: 'Pagos', icon: Icon(Icons.receipt_long_outlined)),
            ],
          ),
          Expanded(
            child: TabBarView(
              children: [
                _AttendanceList(api: api),
                _PaymentsList(api: api),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _AttendanceList extends StatefulWidget {
  const _AttendanceList({required this.api});
  final MemberApi api;

  @override
  State<_AttendanceList> createState() => _AttendanceListState();
}

class _AttendanceListState extends State<_AttendanceList> {
  late Future<List<CheckIn>> _future;

  @override
  void initState() {
    super.initState();
    _future = widget.api.getCheckIns();
  }

  @override
  Widget build(BuildContext context) {
    final fmt = DateFormat('EEE dd MMM · HH:mm', 'es');
    return RefreshIndicator(
      onRefresh: () async {
        setState(() => _future = widget.api.getCheckIns());
        await _future;
      },
      child: FutureBuilder<List<CheckIn>>(
        future: _future,
        builder: (context, snap) {
          if (snap.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) return _error(context, '${snap.error}');
          final items = snap.data ?? [];
          if (items.isEmpty) return _empty(context, 'Sin asistencias todavía.');
          return ListView.separated(
            itemCount: items.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (context, i) {
              final c = items[i];
              return ListTile(
                leading: Icon(
                  c.isValid ? Icons.check_circle : Icons.cancel,
                  color: c.isValid ? Colors.green : Theme.of(context).colorScheme.error,
                ),
                title: Text(fmt.format(c.occurredAtUtc)),
                subtitle: c.isValid ? null : Text(c.reason ?? 'No válido'),
              );
            },
          );
        },
      ),
    );
  }
}

class _PaymentsList extends StatefulWidget {
  const _PaymentsList({required this.api});
  final MemberApi api;

  @override
  State<_PaymentsList> createState() => _PaymentsListState();
}

class _PaymentsListState extends State<_PaymentsList> {
  late Future<List<Payment>> _future;

  @override
  void initState() {
    super.initState();
    _future = widget.api.getPayments();
  }

  @override
  Widget build(BuildContext context) {
    final fmt = DateFormat('dd MMM yyyy', 'es');
    return RefreshIndicator(
      onRefresh: () async {
        setState(() => _future = widget.api.getPayments());
        await _future;
      },
      child: FutureBuilder<List<Payment>>(
        future: _future,
        builder: (context, snap) {
          if (snap.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) return _error(context, '${snap.error}');
          final items = snap.data ?? [];
          if (items.isEmpty) return _empty(context, 'Sin pagos registrados.');
          return ListView.separated(
            itemCount: items.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (context, i) {
              final p = items[i];
              return ListTile(
                leading: const Icon(Icons.payments_outlined),
                title: Text('S/ ${p.amount.toStringAsFixed(2)}'),
                subtitle: Text(p.method.label),
                trailing: Text(fmt.format(p.date)),
              );
            },
          );
        },
      ),
    );
  }
}

Widget _empty(BuildContext context, String text) => ListView(
      children: [
        const SizedBox(height: 120),
        Icon(Icons.inbox_outlined, size: 48, color: Theme.of(context).hintColor),
        const SizedBox(height: 12),
        Text(text, textAlign: TextAlign.center),
      ],
    );

Widget _error(BuildContext context, String text) => ListView(
      children: [
        const SizedBox(height: 120),
        Icon(Icons.error_outline, size: 48, color: Theme.of(context).colorScheme.error),
        const SizedBox(height: 12),
        Text(text, textAlign: TextAlign.center),
      ],
    );
