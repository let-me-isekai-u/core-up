import 'package:flutter/material.dart';

import 'exercise_artwork.dart';
export 'exercise_artwork.dart';

/// Also observes dialogs, so a covered preview stops until its route is visible.
final exerciseVisualRouteObserver = RouteObserver<ModalRoute<dynamic>>();

class ExerciseVisual extends StatefulWidget {
  const ExerciseVisual({
    super.key,
    required this.slug,
    this.frame,
    this.playing = true,
  });
  final String slug;
  /// Supply an index for a still preview; null enables illustrative playback.
  final int? frame;
  final bool playing;

  @override
  State<ExerciseVisual> createState() => _ExerciseVisualState();
}

class _ExerciseVisualState extends State<ExerciseVisual>
    with SingleTickerProviderStateMixin, WidgetsBindingObserver, RouteAware {
  late final AnimationController _controller;
  ModalRoute<dynamic>? _route;
  bool _visible = true;
  bool _foreground = true;
  int _step = 0;

  ExerciseArtwork? get _artwork => coreArtwork[widget.slug];

  @override
  void initState() {
    super.initState();
    _foreground = WidgetsBinding.instance.lifecycleState == null ||
        WidgetsBinding.instance.lifecycleState == AppLifecycleState.resumed;
    WidgetsBinding.instance.addObserver(this);
    _controller = AnimationController(vsync: this)..addListener(_onTick);
  }

  void _onTick() {
    final length = _artwork?.sequence.length ?? 1;
    final next = (_controller.value * length).floor() % length;
    if (next != _step) setState(() => _step = next);
  }

  void _syncPlayback() {
    final artwork = _artwork;
    final enabled = artwork != null && artwork.frames.length > 1 &&
        widget.frame == null && widget.playing && _visible && _foreground &&
        TickerMode.of(context) && !MediaQuery.disableAnimationsOf(context);
    if (!enabled) {
      _controller.stop();
    } else if (!_controller.isAnimating) {
      // Demonstration cadence only; never controls reps, workout or rest clocks.
      _controller.duration = Duration(milliseconds: 750 * artwork!.sequence.length);
      _controller.repeat();
    }
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final route = ModalRoute.of(context);
    if (_route != route) {
      exerciseVisualRouteObserver.unsubscribe(this);
      _route = route;
      if (route != null) exerciseVisualRouteObserver.subscribe(this, route);
    }
    _visible = route?.isCurrent ?? true;
    _syncPlayback();
  }

  @override
  void didUpdateWidget(covariant ExerciseVisual oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.slug != widget.slug || oldWidget.frame != widget.frame) {
      _controller.stop();
      _controller.value = 0;
      _step = 0;
    }
    _syncPlayback();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    _foreground = state == AppLifecycleState.resumed;
    _syncPlayback();
  }

  @override
  void didPushNext() {
    _visible = false;
    _syncPlayback();
  }

  @override
  void didPopNext() {
    _visible = true;
    _syncPlayback();
  }

  @override
  void dispose() {
    exerciseVisualRouteObserver.unsubscribe(this);
    WidgetsBinding.instance.removeObserver(this);
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final artwork = _artwork;
    final path = artwork == null || artwork.frames.isEmpty ? null
        : artwork.frames[(widget.frame ?? artwork.sequence[_step % artwork.sequence.length]) % artwork.frames.length];
    final placeholder = Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Icon(Icons.accessibility_new_rounded, size: 64,
            color: Theme.of(context).colorScheme.primary),
        const SizedBox(height: 8),
        const Text('Hình minh họa đang được chuẩn bị'),
      ],
    );
    return Container(
      height: 220,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(24),
      ),
      child: path == null ? placeholder : Semantics(
        label: 'Minh họa tư thế. Thực hiện theo hướng dẫn, tự đếm số lần.',
        image: true,
        // A fixed frame size and gapless playback avoid flashes during decoding.
        // AssetImage uses Flutter's shared image cache for repeated frames.
        child: Image.asset(path, key: ValueKey(widget.slug), width: 220, height: 220,
          fit: BoxFit.contain, gaplessPlayback: true, excludeFromSemantics: true,
          errorBuilder: (_, __, ___) => placeholder),
      ),
    );
  }
}

/// Catalog thumbnail: no controller, ticker or slideshow is created per row.
class ExerciseThumbnail extends StatelessWidget {
  const ExerciseThumbnail({super.key, required this.slug});
  final String slug;

  @override
  Widget build(BuildContext context) {
    final artwork = coreArtwork[slug];
    final path = artwork == null || artwork.frames.isEmpty ? null
        : artwork.frames[artwork.isStatic ? 0 : 1];
    final fallback = Icon(Icons.accessibility_new_rounded,
        color: Theme.of(context).colorScheme.primary);
    return SizedBox(
      width: 80,
      height: 64,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(12),
        ),
        child: path == null ? fallback : Image.asset(path,
          key: ValueKey(slug), fit: BoxFit.contain, excludeFromSemantics: true,
          errorBuilder: (_, __, ___) => fallback),
      ),
    );
  }
}
