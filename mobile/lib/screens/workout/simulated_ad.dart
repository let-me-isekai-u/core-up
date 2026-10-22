part of '../core_up_screens.dart';

class SimulatedAd extends StatefulWidget {
  const SimulatedAd({super.key, required this.paused, required this.onClosed});
  final bool paused;
  final VoidCallback onClosed;

  @override
  State<SimulatedAd> createState() => _SimulatedAdState();
}

class _SimulatedAdState extends State<SimulatedAd> {
  Timer? _timer;
  int _seconds = 0;
  bool _closed = false;

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted || widget.paused || _closed) return;
      setState(() => _seconds++);
      if (_seconds >= 15) _close();
    });
  }

  void _close() {
    if (_closed || widget.paused || _seconds < 5) return;
    _closed = true;
    _timer?.cancel();
    widget.onClosed();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: surface,
    body: SafeArea(
      child: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Text('Quảng cáo mô phỏng', style: TextStyle(fontSize: 26, fontWeight: FontWeight.bold)),
              const SizedBox(height: 24),
              const Icon(Icons.campaign_outlined, size: 80, color: blue),
              const SizedBox(height: 24),
              const Text('Nội dung quảng cáo mẫu của Core-Up'),
              const SizedBox(height: 16),
              Text('Tự đóng sau ${15 - _seconds} giây'),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: _seconds >= 5 && !widget.paused ? _close : null,
                child: Text(_seconds < 5 ? 'Đóng sau ${5 - _seconds} giây' : 'ĐÓNG QUẢNG CÁO'),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}
