part of '../core_up_screens.dart';

/// The session owns the clock, pause state and transition guard.
class RestScreen extends StatelessWidget {
  const RestScreen({super.key, required this.remaining, required this.next,
    required this.paused, required this.onStart, required this.onAddTime});
  final int remaining;
  final Map<String, dynamic> next;
  final bool paused;
  final VoidCallback onStart, onAddTime;

  @override
  Widget build(BuildContext context) {
    final exercise = Map<String, dynamic>.from(next['exercise']);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SectionLabel('NGHỈ'),
        Text(formatDuration(remaining), textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 52, fontWeight: FontWeight.w800)),
        const SizedBox(height: 24),
        const SectionLabel('CHUẨN BỊ TIẾP THEO'),
        Text(exercise['name'].toString(), style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),
        Text(localizeValue(exercise['difficulty']?.toString() ?? 'Beginner')),
        Text('${next['targetSets']} hiệp × ${next['targetReps'] ?? next['targetDurationSeconds']} ${next['targetDurationSeconds'] == null ? 'lần' : 'giây'}'),
        const SizedBox(height: 12),
        Text(_preparation(exercise), style: const TextStyle(color: muted, height: 1.5)),
        const SizedBox(height: 24),
        FilledButton(onPressed: paused ? null : onStart, child: const Text('BẮT ĐẦU NGAY')),
        OutlinedButton(onPressed: onAddTime, child: const Text('+5 GIÂY')),
      ],
    );
  }
}

String _preparation(Map<String, dynamic> exercise) {
  final text = (exercise['instructions'] ?? exercise['description'] ?? '').toString();
  final steps = text.split(RegExp(r'\s+(?=\d+\.\s)'));
  return steps.take(3).map((step) {
    // Keep the rest preview short; the session retains the full instructions.
    final plain = step.replaceFirst(RegExp(r'^\d+\.\s*'), '');
    return plain.split(RegExp(r'(?<=\.)\s+')).first;
  }).join('\n');
}
