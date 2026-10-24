part of '../core_up_screens.dart';

enum _WorkoutPhase { preview, active, savingExercise, afterExercise, ad, rest, finishing, finishError }

class WorkoutSessionScreen extends StatefulWidget {
  const WorkoutSessionScreen({super.key});
  @override
  State<WorkoutSessionScreen> createState() => _WorkoutSessionScreenState();
}

class _WorkoutSessionScreenState extends State<WorkoutSessionScreen>
    with WidgetsBindingObserver {
  int _index = 0, _currentSet = 1, _remaining = 0, _restRemaining = 0, _elapsed = 0;
  bool _paused = false, _foreground = true, _exiting = false, _left = false;
  bool _betweenSets = false, _setFinished = false;
  String? _error;
  Timer? _timer;
  _WorkoutPhase _phase = _WorkoutPhase.preview;

  bool get _blocked => _paused || !_foreground || _exiting || _left;
  bool get _saving => _phase == _WorkoutPhase.savingExercise || _phase == _WorkoutPhase.finishing;
  WorkoutProvider get _workout => context.read<WorkoutProvider>();
  List get _exercises => _workout.session?['exercises'] as List? ?? [];
  Map<String, dynamic> get _current => Map<String, dynamic>.from(_exercises[_index]);

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted || _blocked) return;
      if (_phase == _WorkoutPhase.active && !_setFinished) {
        setState(() {
          final timed = _current['targetDurationSeconds'] != null;
          if (!timed || _remaining > 0) _elapsed++;
          if (timed && _remaining > 0) _remaining--;
        });
        // Require explicit confirmation after the hold, so no ad interrupts posture recovery.
      } else if (_phase == _WorkoutPhase.rest) {
        if (_restRemaining > 0) setState(() => _restRemaining--);
        if (_restRemaining == 0) _startAfterRest();
      } else if (_phase == _WorkoutPhase.afterExercise) {
        _advanceCompletedExercise();
      }
    });
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (!mounted) return;
    setState(() {
      _foreground = state == AppLifecycleState.resumed;
      // Never resume a hold or rest countdown automatically after backgrounding.
      if (!_foreground && _phase != _WorkoutPhase.ad) _paused = true;
    });
  }

  @override
  void dispose() {
    _left = true;
    _timer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  void _startSet() {
    if (_blocked) return;
    setState(() {
      _remaining = (_current['targetDurationSeconds'] as num? ?? 0).toInt();
      _setFinished = false;
      _error = null;
      _phase = _WorkoutPhase.active;
    });
  }

  void _startAfterRest() {
    if (_blocked || _phase != _WorkoutPhase.rest) return;
    if (_betweenSets) {
      _currentSet++;
    } else {
      _index++;
      _currentSet = 1;
    }
    _startSet();
  }

  void _rest({required bool betweenSets}) {
    setState(() {
      _betweenSets = betweenSets;
      _restRemaining = (_current['restSeconds'] as num? ?? 0).toInt();
      _phase = _WorkoutPhase.rest;
    });
  }

  Future<void> _finishSet() async {
    if (_blocked || _phase != _WorkoutPhase.active) return;
    final current = _current;
    if (current['targetDurationSeconds'] != null && _remaining > 0) return;
    if (_currentSet < (current['targetSets'] as num).toInt()) {
      _rest(betweenSets: true);
      return;
    }
    setState(() {
      _setFinished = true;
      _phase = _WorkoutPhase.savingExercise;
      _error = null;
    });
    try {
      await _workout.complete(current);
      if (!mounted || _left) return;
      await context.read<EntitlementProvider>().refresh();
      if (!mounted || _left) return;
      setState(() => _phase = _WorkoutPhase.afterExercise);
      _advanceCompletedExercise();
    } catch (_) {
      if (!mounted || _left) return;
      setState(() {
        _phase = _WorkoutPhase.active;
        _error = _workout.error ?? 'Chưa lưu được bài tập. Vui lòng thử lại.';
      });
    }
  }

  void _advanceCompletedExercise() {
    if (_blocked || _phase != _WorkoutPhase.afterExercise) return;
    if (_workout.shouldShowAds) {
      setState(() => _phase = _WorkoutPhase.ad);
    } else {
      _afterAd();
    }
  }

  void _afterAd() {
    if (_blocked || (_phase != _WorkoutPhase.ad && _phase != _WorkoutPhase.afterExercise)) return;
    if (_index == _exercises.length - 1) {
      _finishWorkout();
    } else {
      _rest(betweenSets: false);
    }
  }

  Future<void> _finishWorkout() async {
    if (_blocked || _phase == _WorkoutPhase.finishing) return;
    setState(() {
      _phase = _WorkoutPhase.finishing;
      _error = null;
    });
    try {
      await _workout.end(durationSeconds: _elapsed);
      if (!mounted || _left) return;
      _left = true;
      _timer?.cancel();
      Navigator.of(context).pushReplacement(fadeRoute(const WorkoutCompleteScreen()));
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _phase = _WorkoutPhase.finishError;
        _error = _workout.error ?? 'Chưa lưu được buổi tập. Vui lòng thử lại.';
      });
    }
  }

  Future<void> _confirmExit() async {
    if (_exiting || _left || _saving || _phase == _WorkoutPhase.ad) return;
    setState(() => _exiting = true);
    final leave = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Thoát buổi tập?'),
        content: const Text('Buổi tập sẽ không được ghi nhận hoàn thành.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialogContext, false), child: const Text('Ở LẠI')),
          TextButton(onPressed: () => Navigator.pop(dialogContext, true), child: const Text('THOÁT')),
        ],
      ),
    );
    if (!mounted) return;
    if (leave == true) {
      try {
        await _workout.cancel();
        if (!mounted) return;
        _left = true;
        _timer?.cancel();
        Navigator.of(context).pop();
      } catch (_) {
        if (!mounted) return;
        setState(() {
          _exiting = false;
          _paused = true;
          _error = _workout.error ?? 'Chưa hủy được buổi tập. Vui lòng thử lại.';
        });
      }
    } else {
      setState(() {
        _exiting = false;
        _paused = true;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final workout = context.watch<WorkoutProvider>();
    final exercises = workout.session?['exercises'] as List? ?? [];
    Widget body;
    if (_phase == _WorkoutPhase.ad) {
      body = SimulatedAd(paused: _blocked, onClosed: _afterAd);
    } else if (_saving) {
      body = const Scaffold(body: CoreUpChibiLoading(message: 'Đang lưu kết quả...'));
    } else if (exercises.isEmpty) {
      body = const Scaffold(body: Center(child: Text('Không có buổi tập đang hoạt động.')));
    } else {
      final current = Map<String, dynamic>.from(exercises[_index]);
      final exercise = Map<String, dynamic>.from(current['exercise']);
      final duration = current['targetDurationSeconds'] != null;
      final preview = _phase == _WorkoutPhase.preview;
      body = Scaffold(body: SafeArea(child: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Row(children: [
            IconButton(onPressed: _exiting ? null : _confirmExit, icon: const Icon(Icons.close_rounded)),
            Expanded(child: Text('Bài ${_index + 1} / ${exercises.length}')),
            TextButton.icon(
              onPressed: _exiting ? null : () => setState(() => _paused = !_paused),
              icon: Icon(_paused ? Icons.play_arrow_rounded : Icons.pause_rounded),
              label: Text(_paused ? 'Tiếp tục' : 'Tạm dừng'),
            ),
          ]),
          LinearProgressIndicator(value: _index / exercises.length),
          const SizedBox(height: 12),
          Text('Thời gian vận động • ${formatDuration(_elapsed)}', textAlign: TextAlign.center),
          if (_paused) const Padding(padding: EdgeInsets.all(12), child: Text('Đang tạm dừng', textAlign: TextAlign.center)),
          if (_error != null) Padding(padding: const EdgeInsets.all(12), child: Text(_error!, style: const TextStyle(color: Colors.redAccent))),
          const SizedBox(height: 16),
          if (_phase == _WorkoutPhase.rest)
            RestScreen(
              remaining: _restRemaining,
              next: Map<String, dynamic>.from(exercises[_betweenSets ? _index : _index + 1]),
              paused: _blocked,
              onStart: _startAfterRest,
              onAddTime: () => setState(() => _restRemaining += 5),
            )
          else ...[
            ExerciseVisual(
              slug: exercise['slug'].toString(),
              playing: !_blocked && (preview ||
                  (_phase == _WorkoutPhase.active && !_setFinished)),
            ),
            const SizedBox(height: 20),
            Text(exercise['name'].toString(), textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w800)),
            const SizedBox(height: 12),
            Text(localizeValue(exercise['difficulty']?.toString() ?? 'Beginner'), textAlign: TextAlign.center),
            Text('${current['targetSets']} hiệp × ${current['targetReps'] ?? current['targetDurationSeconds']} ${duration ? 'giây' : 'lần'}', textAlign: TextAlign.center),
            const SizedBox(height: 16),
            if (_phase == _WorkoutPhase.active) ...[
              Text('HIỆP $_currentSet / ${current['targetSets']}', textAlign: TextAlign.center),
              Text(duration ? formatDuration(_remaining) : '${current['targetReps']} LẦN', textAlign: TextAlign.center,
                  style: const TextStyle(fontSize: 46, fontWeight: FontWeight.w800)),
              Text(duration ? 'Hạ tư thế khi cần. Xác nhận sau khi kết thúc thời gian giữ.' : 'Tự đếm số lần và xác nhận khi hoàn thành hiệp.', textAlign: TextAlign.center),
            ],
            const SizedBox(height: 12),
            Text(_preparation(exercise), style: const TextStyle(color: muted, height: 1.5)),
            if ((exercise['instructions']?.toString() ?? '').isNotEmpty)
              ExpansionTile(title: const Text('Hướng dẫn đầy đủ'), children: [Text(exercise['instructions'].toString())]),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _blocked ? null : preview ? _startSet
                  : _phase == _WorkoutPhase.finishError ? _finishWorkout
                  : _phase == _WorkoutPhase.active && (!duration || _remaining == 0) ? _finishSet : null,
              child: Text(preview ? 'BẮT ĐẦU' : _phase == _WorkoutPhase.finishError ? 'THỬ LƯU BUỔI TẬP LẠI'
                  : _setFinished ? 'THỬ LƯU BÀI TẬP LẠI' : 'HOÀN THÀNH HIỆP'),
            ),
          ],
        ],
      )));
    }
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) { if (!didPop) _confirmExit(); },
      child: body,
    );
  }
}
