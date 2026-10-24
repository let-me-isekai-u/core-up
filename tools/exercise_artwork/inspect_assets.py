#!/usr/bin/env python3
"""Static asset inventory only: does not execute Flutter or backend code."""
from pathlib import Path
import hashlib
import json
import re
from PIL import Image

root = Path(__file__).resolve().parents[2]
manifest = json.loads((Path(__file__).parent/'manifest.json').read_text())
seed = (root/'src/CoreUp.Infrastructure/Persistence/ExerciseSeed.cs').read_text()
extra = (root/'src/CoreUp.Infrastructure/Persistence/CoreExerciseSeed.cs').read_text()
expected = {m[1]: m[0] == 'Timed' for m in re.findall(
    r'(Reps|Timed)\(\d+, "[^"]+", "([^"]+)", "Core"', seed)}
expected['hollow-body-hold'] = True
expected.update({slug: kind == 'Duration' for slug, kind in re.findall(
    r'Create\(\d+, "[^"]+", "([^"]+)", FitnessLevel\.\w+, ExerciseType\.(\w+),\s*\d+, (?:\d+|null), (?:\d+|null), true', extra)})
assert set(expected) == set(manifest), (expected, manifest.keys())
registry = (root/'mobile/lib/widgets/workout/exercise_artwork.dart').read_text()
pubspec = (root/'mobile/pubspec.yaml').read_text()
actual_paths = set()
for slug, item in manifest.items():
    frames = item['frames']
    static = expected[slug]
    assert item['type'] == ('static' if static else 'dynamic')
    assert len(frames) == 1 if static else 2 <= len(frames) <= 3
    sequence = item['sequence']
    assert set(sequence) == set(range(len(frames)))
    assert sequence == ([0] if static else [0, 1] if len(frames) == 2 else [0, 1, 0, 2])
    block = re.search(r"'"+re.escape(slug)+r"': ExerciseArtwork\((.*?)\n  \),", registry, re.S)[1]
    assert re.findall(r"'(assets/[^']+)'", block) == frames
    assert f'sequence: {sequence}' in block
    assert f'    - assets/exercises/{slug}/\n' in pubspec
    assert len(frames) == len(item['sha256'])
    for index, (path, digest) in enumerate(zip(frames, item['sha256'])):
        file = root/'mobile'/path
        assert file.is_file() and file.stat().st_size > 0
        assert file.name == ('static.png' if static else f'frame_{index+1}.png')
        assert hashlib.sha256(file.read_bytes()).hexdigest() == digest
        with Image.open(file) as im:
            im.verify()
        with Image.open(file) as im:
            assert im.format == 'PNG' and im.mode == 'RGBA' and im.size == (768, 768)
            alpha = im.getchannel('A')
            assert alpha.getextrema() == (0, 255)
            left, top, right, bottom = alpha.getbbox()
            assert min(left, top) > 0 and max(right, bottom) < 768
        actual_paths.add(file)
    assert len(set(item['sha256'])) == len(frames), slug
assert actual_paths == set((root/'mobile/assets/exercises').glob('*/*.png'))
print(f'Static inspection: {len(expected)} catalog slugs; {len(actual_paths)} valid RGBA PNGs; paths, types, frames, playback sequences, hashes and pubspec match.')
print(f'Total PNG bytes: {sum(p.stat().st_size for p in actual_paths):,}')
print('Flutter/backend tests, build, analyze, run: NOT RUN.')
