# Core exercise illustrations

Original joint geometry and drawing code authored for Core-Up. No photographs,
third-party media, fonts embedded in PNGs, model API calls, or paid resources.
Usage and redistribution follow this repository's terms; no third-party media
license is required for these generated shapes. Reference exercise instructions
remain in `ExerciseSeed.cs` and `CoreExerciseSeed.cs`; this is not clinical approval.

Requires Python 3 and Pillow (rendered with Pillow 11.3.0). From the repository:

```sh
python3 tools/exercise_artwork/generate.py --review-dir /Users/nhatanhly/core-up-docs/assets-review
python3 tools/exercise_artwork/inspect_assets.py
```

The generator writes 33 transparent 768×768 RGBA PNGs, SHA-256 manifest and the
Dart registry. It uses shared anatomy, palette, line widths and per-joint poses;
front/oblique overhead view for fallout, side view for the other exercises.
Camera, mat and canvas remain fixed within each exercise. Menlo is used only
for local contact sheet labels, never inside the exercise assets. Omit
`--review-dir` to render on a machine without that macOS contact sheet font.

The five holds have one frame. Crunch, reverse crunch, leg raise, pelvic tilt
and double-leg abdominal press have two. Bird dog, bicycle crunch, dead bug,
heel slide, fallout and single-leg abdominal press have three. Alternating
movements play neutral → side A → neutral → side B; the other movements play
start → action. All phases last 750 ms. These are illustrative key poses, not
rep counting, a prescribed pace or a complete motion capture sequence. Abdominal
press instructions still govern the three deep breaths; hold a pose as directed.

`ExerciseVisual` uses a single disposable animation controller, route visibility,
app lifecycle, TickerMode, reduced-motion preference and the session pause state.
Static art never starts the controller. Missing art falls back to a placeholder.
Flutter image caching and gapless playback preserve decoded frames.

Static inspection checks catalog coverage/types, file names, PNG metadata,
transparency, margins, unique frames, hashes, pubspec directories and Dart paths/
sequences. It is **not application testing**. No Flutter or backend commands are
needed. The contact sheets were visually inspected during authoring. Owner and
iOS Simulator review are still required, particularly the small pelvic tilt,
rotational bicycle crunch, depth of fallout and hand/knee pressure in presses.
Build/test/analyze/run: NOT RUN at the owner's request.
