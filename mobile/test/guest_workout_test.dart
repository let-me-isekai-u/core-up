import 'package:core_up_mobile/core/storage/local_storage_service.dart';
import 'package:core_up_mobile/core/utils/minimum_loading_duration.dart';
import 'package:core_up_mobile/providers/auth_provider.dart';
import 'package:core_up_mobile/providers/workout_provider.dart';
import 'package:core_up_mobile/screens/core_up_screens.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

void main() {
  test(
    'minimum loading starts the action immediately and only waits remainder',
    () async {
      var actionStarted = false;
      final stopwatch = Stopwatch()..start();

      final result = await withMinimumLoading(
        () async {
          actionStarted = true;
          return 42;
        },
        minimumMilliseconds: 40,
        maximumMilliseconds: 40,
      );

      expect(actionStarted, isTrue);
      expect(result, 42);
      expect(stopwatch.elapsedMilliseconds, greaterThanOrEqualTo(35));
      expect(stopwatch.elapsedMilliseconds, lessThan(250));
    },
  );

  test('guest workout runs locally without an authenticated token', () async {
    final auth = AuthProvider(LocalStorageService())..status = AuthStatus.guest;
    final workout = WorkoutProvider(auth);

    await workout.loadExercises();
    workout.startGuestWorkout();

    expect(workout.exercises, isNotEmpty);
    expect(workout.session?['isGuest'], isTrue);
    expect(workout.session?['exercises'], hasLength(6));

    final first = Map<String, dynamic>.from(
      (workout.session!['exercises'] as List).first,
    );
    await workout.complete(first);
    await workout.end(durationSeconds: 61);

    expect(workout.session?['completedExercises'], 1);
    expect(workout.session?['durationSeconds'], 61);
    expect(auth.token, isNull);
  });

  testWidgets('guest workout player shows set and repetition guidance', (
    tester,
  ) async {
    final auth = AuthProvider(LocalStorageService())..status = AuthStatus.guest;
    final workout = WorkoutProvider(auth)..startGuestWorkout();

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider.value(value: auth),
          ChangeNotifierProvider.value(value: workout),
        ],
        child: const MaterialApp(home: WorkoutSessionScreen()),
      ),
    );
    await tester.pump();

    expect(find.text('1 / 6'), findsOneWidget);
    expect(find.text('HIỆP 1 / 2'), findsOneWidget);
    expect(find.text('12'), findsOneWidget);
    expect(find.text('LẦN'), findsOneWidget);
    expect(find.text('HOÀN THÀNH HIỆP'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
