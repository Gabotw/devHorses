import 'package:flutter/material.dart';

import '../../services/auth_service.dart';
import '../../services/member_api.dart';

class CheckInTab extends StatefulWidget {
  const CheckInTab({super.key, required this.api});

  final MemberApi api;

  @override
  State<CheckInTab> createState() => _CheckInTabState();
}

class _CheckInTabState extends State<CheckInTab> {
  bool _loading = false;
  CheckInResult? _result;
  String? _error;

  Future<void> _checkIn() async {
    setState(() {
      _loading = true;
      _error = null;
      _result = null;
    });
    try {
      final result = await widget.api.selfCheckIn();
      setState(() => _result = result);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (_) {
      setState(() => _error = 'No se pudo registrar el ingreso.');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            FilledButton.icon(
              onPressed: _loading ? null : _checkIn,
              icon: _loading
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.login),
              label: const Padding(
                padding: EdgeInsets.symmetric(vertical: 16, horizontal: 8),
                child: Text('Registrar mi ingreso', style: TextStyle(fontSize: 16)),
              ),
            ),
            const SizedBox(height: 32),
            if (_error != null)
              _Banner(
                color: theme.colorScheme.error,
                icon: Icons.error_outline,
                title: 'No válido',
                subtitle: _error!,
              ),
            if (_result != null) ...[
              _Banner(
                color: _result!.checkIn.isValid ? Colors.green : theme.colorScheme.error,
                icon: _result!.checkIn.isValid ? Icons.check_circle_outline : Icons.cancel_outlined,
                title: _result!.checkIn.isValid ? '¡Ingreso registrado!' : 'Ingreso no válido',
                subtitle: _result!.checkIn.isValid
                    ? 'Disfruta tu entrenamiento.'
                    : (_result!.checkIn.reason ?? 'Revisa tu membresía.'),
              ),
              const SizedBox(height: 16),
              Text('Personas en el gimnasio ahora: ${_result!.occupancy}',
                  style: theme.textTheme.titleMedium),
            ],
          ],
        ),
      ),
    );
  }
}

class _Banner extends StatelessWidget {
  const _Banner({required this.color, required this.icon, required this.title, required this.subtitle});
  final Color color;
  final IconData icon;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Icon(icon, color: color, size: 32),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: TextStyle(fontWeight: FontWeight.bold, color: color)),
                Text(subtitle),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
