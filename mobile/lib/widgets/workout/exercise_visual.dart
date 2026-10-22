import 'package:flutter/material.dart';

/// PNG slots keyed by backend slug; empty paths intentionally render a placeholder.
/// Static holds use one slot, movements use two. No simulated posture animation.
/// Add PNGs under assets/ (already bundled), then replace a mapping with e.g.
/// ExerciseArtwork(['assets/pelvic-tilt-1.png', 'assets/pelvic-tilt-2.png']).
/// Pass frame 0/1 to display the supplied poses; playback is intentionally deferred.
class ExerciseArtwork {
  const ExerciseArtwork.hold() : frames = const [null];
  const ExerciseArtwork.movement() : frames = const [null, null];
  const ExerciseArtwork(this.frames);
  final List<String?> frames;
}

const coreArtwork = <String, ExerciseArtwork>{
  'pelvic-tilt': ExerciseArtwork.movement(),
  'supine-heel-slide': ExerciseArtwork.movement(),
  'bent-knee-fallout': ExerciseArtwork.movement(),
  'modified-knee-plank': ExerciseArtwork.hold(),
  'bird-dog': ExerciseArtwork.movement(),
  'dead-bug': ExerciseArtwork.movement(),
  'plank': ExerciseArtwork.hold(),
  'side-plank': ExerciseArtwork.hold(),
  'crunch': ExerciseArtwork.movement(),
  'bicycle-crunch': ExerciseArtwork.movement(),
  'reverse-crunch': ExerciseArtwork.movement(),
  'leg-raise': ExerciseArtwork.movement(),
  'hollow-body-hold': ExerciseArtwork.hold(),
  'single-leg-abdominal-press': ExerciseArtwork.movement(),
  'modified-side-plank': ExerciseArtwork.hold(),
  'double-leg-abdominal-press': ExerciseArtwork.movement(),
};

class ExerciseVisual extends StatelessWidget {
  const ExerciseVisual({super.key, required this.slug, this.frame = 0});
  final String slug;
  // Future frame playback can drive this index without changing the screen layout.
  final int frame;

  @override
  Widget build(BuildContext context) {
    final frames = coreArtwork[slug]?.frames;
    final path = frames == null || frames.isEmpty ? null : frames[frame % frames.length];
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
      height: 190,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(24),
      ),
      child: path == null ? placeholder : Image.asset(path, fit: BoxFit.contain,
          errorBuilder: (_, __, ___) => placeholder),
    );
  }
}
