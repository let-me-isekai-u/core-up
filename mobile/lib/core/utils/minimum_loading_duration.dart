import 'dart:math';

Future<T> withMinimumLoading<T>(
  Future<T> Function() action, {
  int minimumMilliseconds = 1000,
  int maximumMilliseconds = 1500,
}) async {
  final stopwatch = Stopwatch()..start();
  final random = Random();
  final range = maximumMilliseconds - minimumMilliseconds;
  final targetMilliseconds =
      minimumMilliseconds + (range > 0 ? random.nextInt(range + 1) : 0);

  try {
    return await action();
  } finally {
    final remaining = targetMilliseconds - stopwatch.elapsedMilliseconds;
    if (remaining > 0) {
      await Future<void>.delayed(Duration(milliseconds: remaining));
    }
  }
}
