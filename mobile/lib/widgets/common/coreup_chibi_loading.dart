part of '../../screens/core_up_screens.dart';

class CoreUpChibiLoading extends StatefulWidget {
  const CoreUpChibiLoading({super.key, this.message, this.width = 120});

  final String? message;
  final double width;

  @override
  State<CoreUpChibiLoading> createState() => _CoreUpChibiLoadingState();
}

class _CoreUpChibiLoadingState extends State<CoreUpChibiLoading> {
  static const _frames = [
    'assets/crunch_chibi_01.png',
    'assets/crunch_chibi_02.png',
    'assets/crunch_chibi_03.png',
    'assets/crunch_chibi_02.png',
  ];
  static const _durations = [120, 120, 160, 120];

  Timer? _timer;
  var _index = 0;
  var _ready = false;
  var _precacheStarted = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_precacheStarted) return;
    _precacheStarted = true;
    _prepareFrames();
  }

  Future<void> _prepareFrames() async {
    await Future.wait(
      _frames.toSet().map((frame) => precacheImage(AssetImage(frame), context)),
    );
    if (!mounted) return;
    setState(() => _ready = true);
    _scheduleNextFrame();
  }

  void _scheduleNextFrame() {
    _timer = Timer(Duration(milliseconds: _durations[_index]), () {
      if (!mounted) return;
      setState(() => _index = (_index + 1) % _frames.length);
      _scheduleNextFrame();
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Center(
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        SizedBox(
          width: widget.width,
          height: widget.width,
          child: _ready
              ? Image.asset(
                  _frames[_index],
                  fit: BoxFit.contain,
                  gaplessPlayback: true,
                )
              : const SizedBox.shrink(),
        ),
        if (widget.message != null) ...[
          const SizedBox(height: 12),
          Text(
            widget.message!,
            textAlign: TextAlign.center,
            style: const TextStyle(color: muted, fontSize: 14),
          ),
        ],
      ],
    ),
  );
}
