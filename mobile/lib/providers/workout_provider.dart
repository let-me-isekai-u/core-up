import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import 'auth_provider.dart';

const guestExercises = <Map<String, dynamic>>[
  {
    'id': 'g1',
    'slug': 'bodyweight-squat',
    'name': 'Squat không tạ',
    'muscleGroup': 'Legs',
    'difficulty': 'Beginner',
    'description':
        'Hạ hông có kiểm soát, giữ lưng thẳng và đầu gối cùng hướng mũi chân.',
  },
  {
    'id': 'g2',
    'slug': 'bicycle-crunch',
    'name': 'Gập bụng đạp xe',
    'muscleGroup': 'Core',
    'difficulty': 'Intermediate',
    'description':
        'Luân phiên đưa khuỷu tay về phía đầu gối đối diện, siết cơ bụng.',
  },
  {
    'id': 'g3',
    'slug': 'push-up',
    'name': 'Chống đẩy',
    'muscleGroup': 'Chest',
    'difficulty': 'Intermediate',
    'description':
        'Giữ thân người thành một đường thẳng và hạ ngực có kiểm soát.',
  },
  {
    'id': 'g4',
    'slug': 'glute-bridge',
    'name': 'Nâng hông',
    'muscleGroup': 'Legs',
    'difficulty': 'Beginner',
    'description': 'Siết mông và nâng hông, không ưỡn quá mức phần lưng dưới.',
  },
  {
    'id': 'g5',
    'slug': 'plank',
    'name': 'Plank',
    'muscleGroup': 'Core',
    'difficulty': 'Beginner',
    'description': 'Giữ vai, hông và gót chân trên một đường thẳng.',
  },
  {
    'id': 'g6',
    'slug': 'mountain-climber',
    'name': 'Leo núi tại chỗ',
    'muscleGroup': 'Full Body',
    'difficulty': 'Advanced',
    'description': 'Kéo gối luân phiên về ngực và giữ thân trên ổn định.',
  },
  {
    'id': 'g7',
    'slug': 'reverse-lunge',
    'name': 'Chùng chân sau',
    'muscleGroup': 'Legs',
    'difficulty': 'Intermediate',
    'description': 'Bước một chân ra sau và hạ gối, giữ trọng tâm cân bằng.',
  },
  {
    'id': 'g8',
    'slug': 'shoulder-tap',
    'name': 'Plank chạm vai',
    'muscleGroup': 'Arms',
    'difficulty': 'Advanced',
    'description':
        'Từ tư thế plank, chạm tay sang vai đối diện và hạn chế xoay hông.',
  },
];

class WorkoutProvider extends ChangeNotifier {
  WorkoutProvider(this.api);
  final AuthProvider api;
  List<Map<String, dynamic>> exercises = [], weights = [], history = [];
  Map<String, dynamic>? plan, session, progress;
  bool loading = false;
  String? error;
  Future<void> home() async {
    if (api.isGuest) return;
    loading = true;
    notifyListeners();
    try {
      final r = await Future.wait([
        api.dio.get('/api/workout-plans/active'),
        api.dio.get('/api/progress/summary'),
      ]);
      plan = Map<String, dynamic>.from(r[0].data);
      progress = Map<String, dynamic>.from(r[1].data);
    } catch (e) {
      if (e is! DioException || e.response?.statusCode != 404)
        error = api.message(e);
    }
    loading = false;
    notifyListeners();
  }

  Future<void> loadExercises() async {
    if (api.isGuest) {
      exercises = guestExercises.map(Map<String, dynamic>.from).toList();
      loading = false;
      notifyListeners();
      return;
    }
    loading = true;
    notifyListeners();
    try {
      exercises = ((await api.dio.get('/api/exercises')).data as List)
          .map((x) => Map<String, dynamic>.from(x))
          .toList();
    } catch (e) {
      error = api.message(e);
    }
    loading = false;
    notifyListeners();
  }

  Future<void> loadProgress() async {
    if (api.isGuest) return;
    loading = true;
    notifyListeners();
    try {
      final r = await Future.wait([
        api.dio.get('/api/progress/summary'),
        api.dio.get('/api/weight-records'),
        api.dio.get('/api/workout-sessions/history'),
      ]);
      progress = Map<String, dynamic>.from(r[0].data);
      weights = (r[1].data as List)
          .map((x) => Map<String, dynamic>.from(x))
          .toList();
      history = (r[2].data as List)
          .map((x) => Map<String, dynamic>.from(x))
          .toList();
    } catch (e) {
      error = api.message(e);
    }
    loading = false;
    notifyListeners();
  }

  Future<bool> generate() async {
    loading = true;
    notifyListeners();
    try {
      plan = Map<String, dynamic>.from(
        (await api.dio.post('/api/workout-plans/generate')).data,
      );
      return true;
    } catch (e) {
      error = api.message(e);
      return false;
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<bool> start() async {
    try {
      session = Map<String, dynamic>.from(
        (await api.dio.post(
          '/api/workout-sessions/start',
          data: {'workoutPlanId': plan!['id']},
        )).data,
      );
      notifyListeners();
      return true;
    } catch (e) {
      error = api.message(e);
      notifyListeners();
      return false;
    }
  }

  void startGuestWorkout() {
    final selected = guestExercises.take(6).toList();
    final startedAt = DateTime.now().toUtc().toIso8601String();
    session = {
      'id': 'guest-$startedAt',
      'startedAt': startedAt,
      'isGuest': true,
      'completedExercises': 0,
      'exercises': selected
          .map(
            (exercise) => {
              'id': 'session-${exercise['id']}',
              'exercise': Map<String, dynamic>.from(exercise),
              'targetSets': 2,
              'targetReps': exercise['slug'] == 'plank' ? null : 12,
              'targetDurationSeconds': exercise['slug'] == 'plank' ? 30 : null,
              'restSeconds': 30,
            },
          )
          .toList(),
    };
    notifyListeners();
  }

  Future<void> complete(Map<String, dynamic> exercise) async {
    if (api.isGuest) {
      session!['completedExercises'] =
          (session!['completedExercises'] as int) + 1;
      notifyListeners();
      return;
    }
    session = Map<String, dynamic>.from(
      (await api.dio.put(
        '/api/workout-sessions/' +
            session!['id'] +
            '/exercises/' +
            exercise['id'],
        data: {
          'completedSets': exercise['targetSets'],
          'completedReps': exercise['targetReps'] ?? 0,
          'completedDurationSeconds': exercise['targetDurationSeconds'] ?? 0,
          'isCompleted': true,
        },
      )).data,
    );
    notifyListeners();
  }

  Future<void> end({int? durationSeconds}) async {
    if (api.isGuest) {
      session!['durationSeconds'] = durationSeconds ?? 0;
      notifyListeners();
      return;
    }
    session = Map<String, dynamic>.from(
      (await api.dio.post(
        '/api/workout-sessions/' + session!['id'] + '/complete',
      )).data,
    );
    notifyListeners();
  }

  Future<bool> weight(double kg) async {
    if (api.isGuest) return false;
    try {
      await api.dio.post('/api/weight-records', data: {'weightKg': kg});
      await loadProgress();
      return true;
    } catch (e) {
      error = api.message(e);
      notifyListeners();
      return false;
    }
  }
}
