part of '../core_up_screens.dart';

class WorkoutSessionScreen extends StatefulWidget {
  const WorkoutSessionScreen({super.key});
  @override
  State<WorkoutSessionScreen> createState() => _WorkoutSessionScreenState();
}

class _WorkoutSessionScreenState extends State<WorkoutSessionScreen> {
  var _index = 0;
  var _currentSet = 1;
  var _paused = false;
  var _saving = false;
  Timer? _timer;
  final _elapsed = ValueNotifier(0);
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final value = context.read<WorkoutProvider>().session?['startedAt'];
      if (value is String) {
        _updateTime(value);
        _timer = Timer.periodic(const Duration(seconds: 1), (_) {
          if (!_paused) _elapsed.value++;
        });
      }
    });
  }

  void _updateTime(String startedAt) {
    if (_paused) return;
    final start = DateTime.tryParse(startedAt);
    if (start != null)
      _elapsed.value = DateTime.now()
          .toUtc()
          .difference(start.toUtc())
          .inSeconds;
  }

  @override
  void dispose() {
    _timer?.cancel();
    _elapsed.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final workout = context.watch<WorkoutProvider>();
    if (_saving) {
      return const Scaffold(
        body: CoreUpChibiLoading(message: 'Đang lưu kết quả...'),
      );
    }
    final exercises = (workout.session?['exercises'] as List? ?? [])
        .cast<dynamic>();
    if (exercises.isEmpty) {
      return const Scaffold(
        body: CoreUpChibiLoading(message: 'Đang chuẩn bị buổi tập...'),
      );
    }
    final current = Map<String, dynamic>.from(exercises[_index]);
    final exercise = Map<String, dynamic>.from(current['exercise']);
    final targetSets = (current['targetSets'] as num? ?? 1).toInt();
    final targetValue =
        current['targetReps'] ?? current['targetDurationSeconds'] ?? 0;
    final usesDuration = current['targetDurationSeconds'] != null;
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (_, __) => _confirmExit(),
      child: Scaffold(
        body: SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    IconButton(
                      onPressed: _confirmExit,
                      icon: const Icon(Icons.close_rounded),
                    ),
                    Text(
                      '${_index + 1} / ${exercises.length}',
                      style: const TextStyle(
                        color: blue,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    TextButton.icon(
                      onPressed: () => setState(() => _paused = !_paused),
                      icon: Icon(
                        _paused
                            ? Icons.play_arrow_rounded
                            : Icons.pause_rounded,
                      ),
                      label: Text(_paused ? 'Tiếp tục' : 'Tạm dừng'),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                LinearProgressIndicator(
                  value: (_index + 1) / exercises.length,
                  minHeight: 7,
                ),
                const SizedBox(height: 10),
                ValueListenableBuilder<int>(
                  valueListenable: _elapsed,
                  builder: (_, value, __) => Text(
                    'Thời gian • ' + formatDuration(value),
                    textAlign: TextAlign.center,
                    style: const TextStyle(color: muted),
                  ),
                ),
                const SizedBox(height: 12),
                SizedBox(
                  height: 390,
                  child: AnimatedSwitcher(
                    duration: const Duration(milliseconds: 420),
                    reverseDuration: const Duration(milliseconds: 320),
                    transitionBuilder: (child, animation) => FadeTransition(
                      opacity: animation,
                      child: SlideTransition(
                        position: Tween<Offset>(
                          begin: const Offset(.06, 0),
                          end: Offset.zero,
                        ).animate(animation),
                        child: child,
                      ),
                    ),
                    child: Column(
                      key: ValueKey(current['id']),
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        SizedBox(
                          height: 190,
                          child: Center(
                            child: Container(
                              width: 116,
                              height: 116,
                              decoration: const BoxDecoration(
                                color: AppColors.surfaceElevated,
                                shape: BoxShape.circle,
                              ),
                              child: Icon(
                                exerciseIcon(
                                  exercise['muscleGroup'].toString(),
                                ),
                                size: 58,
                                color: blue,
                              ),
                            ),
                          ),
                        ),
                        Text(
                          exercise['name'],
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                            fontSize: 28,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const SizedBox(height: 10),
                        Text(
                          'HIỆP $_currentSet / $targetSets',
                          style: const TextStyle(
                            color: blue,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 1.1,
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          targetValue.toString(),
                          style: const TextStyle(
                            fontSize: 46,
                            height: 1,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        Text(
                          usesDuration ? 'GIÂY' : 'LẦN',
                          style: const TextStyle(
                            color: muted,
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 1.4,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const Spacer(),
                PrimaryButton(
                  label: _currentSet < targetSets
                      ? 'HOÀN THÀNH HIỆP'
                      : _index == exercises.length - 1
                      ? 'HOÀN THÀNH BUỔI TẬP'
                      : 'XONG & TIẾP THEO',
                  onTap: () async {
                    final rest = current['restSeconds'] as int? ?? 0;
                    if (_currentSet < targetSets) {
                      if (rest > 0) {
                        await Navigator.of(context).push(
                          fadeRoute(RestScreen(seconds: rest, next: current)),
                        );
                      }
                      if (mounted) setState(() => _currentSet++);
                      return;
                    }
                    final authenticated = context
                        .read<AuthProvider>()
                        .isAuthenticated;
                    if (authenticated) setState(() => _saving = true);
                    if (authenticated) {
                      await withMinimumLoading(() async {
                        await workout.complete(current);
                        if (_index == exercises.length - 1) {
                          await workout.end(durationSeconds: _elapsed.value);
                        }
                      });
                    } else {
                      await workout.complete(current);
                      if (_index == exercises.length - 1) {
                        await workout.end(durationSeconds: _elapsed.value);
                      }
                    }
                    if (!mounted) return;
                    if (authenticated) setState(() => _saving = false);
                    if (_index == exercises.length - 1) {
                      if (mounted)
                        Navigator.of(context).pushReplacement(
                          fadeRoute(const WorkoutCompleteScreen()),
                        );
                    } else {
                      if (rest > 0 && mounted) {
                        final next = Map<String, dynamic>.from(
                          exercises[_index + 1],
                        );
                        await Navigator.of(context).push(
                          fadeRoute(RestScreen(seconds: rest, next: next)),
                        );
                      }
                      if (mounted) {
                        setState(() {
                          _index++;
                          _currentSet = 1;
                        });
                      }
                    }
                  },
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _confirmExit() async {
    final leave = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Thoát buổi tập?'),
        content: const Text(
          'Tiến độ của buổi tập này sẽ không được hoàn thành.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('TIẾP TỤC TẬP'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text(
              'THOÁT',
              style: TextStyle(color: Colors.redAccent),
            ),
          ),
        ],
      ),
    );
    if (leave == true && mounted) Navigator.of(context).pop();
  }
}
