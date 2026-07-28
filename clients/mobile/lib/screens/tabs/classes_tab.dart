import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models.dart';
import '../../services/auth_service.dart';
import '../../services/member_api.dart';

/// Clases (Fase 7): próximas sesiones con cupo. El miembro reserva o entra a lista de espera;
/// si ya tiene reserva, puede cancelarla. Al cancelar un cupo, el backend promueve la espera.
class ClassesTab extends StatefulWidget {
  const ClassesTab({super.key, required this.api});

  final MemberApi api;

  @override
  State<ClassesTab> createState() => _ClassesTabState();
}

class _ClassesTabState extends State<ClassesTab> {
  late Future<List<MemberClass>> _future;
  String? _busyId;

  @override
  void initState() {
    super.initState();
    _future = widget.api.getClasses();
  }

  Future<void> _reload() async {
    setState(() => _future = widget.api.getClasses());
    await _future;
  }

  Future<void> _reserve(MemberClass c) async {
    setState(() => _busyId = c.id);
    try {
      final status = await widget.api.reserveClass(c.id);
      if (mounted) {
        _toast(status == ReservationStatus.waitlisted
            ? 'Estás en lista de espera.'
            : '¡Reserva confirmada!');
      }
      await _reload();
    } on ApiException catch (e) {
      if (mounted) _toast(e.message, error: true);
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  Future<void> _cancel(MemberClass c) async {
    setState(() => _busyId = c.id);
    try {
      await widget.api.cancelReservation(c.id);
      if (mounted) _toast('Reserva cancelada.');
      await _reload();
    } on ApiException catch (e) {
      if (mounted) _toast(e.message, error: true);
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  void _toast(String message, {bool error = false}) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(message),
      backgroundColor: error ? Theme.of(context).colorScheme.error : null,
    ));
  }

  @override
  Widget build(BuildContext context) {
    final fmt = DateFormat('EEE dd MMM · HH:mm', 'es');
    return RefreshIndicator(
      onRefresh: _reload,
      child: FutureBuilder<List<MemberClass>>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return _messageList('No se pudieron cargar las clases.');
          }
          final classes = snapshot.data ?? [];
          if (classes.isEmpty) {
            return _messageList('No hay clases programadas por ahora.');
          }
          return ListView.separated(
            padding: const EdgeInsets.all(16),
            physics: const AlwaysScrollableScrollPhysics(),
            itemCount: classes.length,
            separatorBuilder: (_, _) => const SizedBox(height: 12),
            itemBuilder: (context, i) => _ClassCard(
              item: classes[i],
              subtitle: fmt.format(classes[i].startsAt),
              busy: _busyId == classes[i].id,
              onReserve: () => _reserve(classes[i]),
              onCancel: () => _cancel(classes[i]),
            ),
          );
        },
      ),
    );
  }

  Widget _messageList(String message) => ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          Padding(
            padding: const EdgeInsets.only(top: 80),
            child: Center(child: Text(message)),
          ),
        ],
      );
}

class _ClassCard extends StatelessWidget {
  const _ClassCard({
    required this.item,
    required this.subtitle,
    required this.busy,
    required this.onReserve,
    required this.onCancel,
  });

  final MemberClass item;
  final String subtitle;
  final bool busy;
  final VoidCallback onReserve;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item.name, style: theme.textTheme.titleMedium),
                      const SizedBox(height: 2),
                      Text(
                        item.instructorName == null ? subtitle : '$subtitle · ${item.instructorName}',
                        style: theme.textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
                _spots(theme),
              ],
            ),
            const SizedBox(height: 12),
            _action(theme),
          ],
        ),
      ),
    );
  }

  Widget _spots(ThemeData theme) {
    final full = item.isFull && !item.isReserved;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Text('${item.bookedCount}/${item.capacity}',
            style: theme.textTheme.titleMedium?.copyWith(
              color: full ? theme.colorScheme.error : null,
            )),
        Text('cupos', style: theme.textTheme.bodySmall),
      ],
    );
  }

  Widget _action(ThemeData theme) {
    if (busy) {
      return const SizedBox(
        height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2));
    }

    if (item.isReserved) {
      final color = item.myStatus == ReservationStatus.waitlisted ? Colors.orange : Colors.green;
      return Row(
        children: [
          Icon(
            item.myStatus == ReservationStatus.waitlisted ? Icons.hourglass_bottom : Icons.check_circle,
            color: color, size: 20,
          ),
          const SizedBox(width: 6),
          Text(item.myStatus.label, style: TextStyle(color: color, fontWeight: FontWeight.w600)),
          const Spacer(),
          TextButton(onPressed: onCancel, child: const Text('Cancelar')),
        ],
      );
    }

    return SizedBox(
      width: double.infinity,
      child: FilledButton.tonal(
        onPressed: onReserve,
        child: Text(item.isFull ? 'Entrar a lista de espera' : 'Reservar'),
      ),
    );
  }
}
