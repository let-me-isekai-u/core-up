part of '../core_up_screens.dart';

class RestScreen extends StatefulWidget {
  const RestScreen({super.key, required this.seconds, required this.next});
  final int seconds;
  final Map<String, dynamic> next;
  @override
  State<RestScreen> createState() => _RestScreenState();
}

class _RestScreenState extends State<RestScreen> {
  late int remaining;
  Timer? timer;
  @override
  void initState() {
    super.initState();
    remaining = widget.seconds;
    timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted) return;
      if (remaining <= 1) {
        timer?.cancel();
        Navigator.of(context).pop();
      } else {
        setState(() => remaining--);
      }
    });
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final exercise = Map<String, dynamic>.from(widget.next['exercise']);
    final pulse = remaining <= 3 ? 1.06 : 1.0;
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            children: [
              const SizedBox(height: 24),
              const Text(
                'NGHỈ',
                style: TextStyle(
                  color: blue,
                  letterSpacing: 3,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const Spacer(),
              TweenAnimationBuilder<double>(
                tween: Tween(end: pulse),
                duration: const Duration(milliseconds: 300),
                builder: (_, scale, child) =>
                    Transform.scale(scale: scale, child: child),
                child: SizedBox(
                  width: 210,
                  height: 210,
                  child: Stack(
                    alignment: Alignment.center,
                    children: [
                      SizedBox.expand(
                        child: CircularProgressIndicator(
                          value: remaining / widget.seconds,
                          strokeWidth: 10,
                          backgroundColor: Colors.white12,
                        ),
                      ),
                      Text(
                        formatDuration(remaining),
                        style: const TextStyle(
                          fontSize: 52,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 28),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => setState(() => remaining += 10),
                      child: const Text('+10 GIÂY'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton(
                      onPressed: () => Navigator.of(context).pop(),
                      child: const Text('BỎ QUA'),
                    ),
                  ),
                ],
              ),
              const Spacer(),
              const Align(
                alignment: Alignment.centerLeft,
                child: SectionLabel('TIẾP THEO'),
              ),
              SurfaceCard(
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: CircleAvatar(
                    backgroundColor: AppColors.surfaceElevated,
                    child: Icon(
                      exerciseIcon(exercise['muscleGroup'].toString()),
                      color: blue,
                    ),
                  ),
                  title: Text(
                    exercise['name'].toString(),
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  subtitle: Text(
                    '${widget.next['targetSets']} hiệp × ${widget.next['targetReps'] ?? widget.next['targetDurationSeconds']} ${widget.next['targetDurationSeconds'] == null ? 'lần' : 'giây'}',
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
