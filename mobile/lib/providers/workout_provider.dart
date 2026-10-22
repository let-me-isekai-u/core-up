import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import 'auth_provider.dart';

import '../models/workout/guest_catalog.dart';
import '../services/ad_policy.dart';

export '../models/workout/guest_catalog.dart';

class WorkoutProvider extends ChangeNotifier {
  WorkoutProvider(this.api, {this.adPolicy = const AdPolicy()});
  final AdPolicy adPolicy;
  bool get shouldShowAds => adPolicy.shouldShowAds(isGuest: session?['isGuest'] == true);
  final AuthProvider api;
  List<Map<String, dynamic>> exercises = [], weights = [], history = [];
  Map<String, dynamic>? plan, session, progress;
  bool loading = false;
  String? error;
  String? _owner;
  void syncAccount() {
    final owner = '${api.status.name}:${api.token}';
    if (_owner == owner) return;
    _owner = owner;
    exercises = []; weights = []; history = [];
    plan = null; session = null; progress = null;
    error = null; loading = false;
  }

  String _message(Object error) {
    if (error is DioException && error.response?.data is Map) {
      final detail = (error.response!.data as Map)['detail'];
      if (detail is String) return detail;
    }
    return api.message(error);
  }

  Future<void> home() async {
    if (!api.isAuthenticated) return;
    final owner = _owner;
    loading = true;
    error = null;
    notifyListeners();
    try {
      final response = await api.dio.get('/api/workout-plans/active');
      if (owner == _owner) plan = Map<String, dynamic>.from(response.data);
    } catch (e) {
      if (owner == _owner) {
        if (e is DioException && e.response?.statusCode == 404) {
          plan = null;
        } else {
          error = _message(e);
        }
      }
    } finally {
      if (owner == _owner) { loading = false; notifyListeners(); }
    }
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
      error = _message(e);
    }
    loading = false;
    notifyListeners();
  }

  Future<void> loadProgress() async {
    if (!api.isAuthenticated) return;
    final owner = _owner;
    loading = true;
    notifyListeners();
    try {
      final r = await Future.wait([
        api.dio.get('/api/progress/summary'),
        api.dio.get('/api/weight-records'),
        api.dio.get('/api/workout-sessions/history'),
      ]);
      if (owner != _owner) return;
      progress = Map<String, dynamic>.from(r[0].data);
      weights = (r[1].data as List)
          .map((x) => Map<String, dynamic>.from(x))
          .toList();
      weights.sort((a, b) => b['recordedAt'].toString().compareTo(a['recordedAt'].toString()));
      history = (r[2].data as List)
          .map((x) => Map<String, dynamic>.from(x))
          .toList();
    } catch (e) {
      if (owner == _owner) error = _message(e);
    } finally {
      if (owner == _owner) { loading = false; notifyListeners(); }
    }
  }

  Future<bool> generate() async {
    final owner = _owner;
    error = null;
    loading = true;
    notifyListeners();
    try {
      final response = await api.dio.post('/api/workout-plans/generate');
      if (owner != _owner) return false;
      plan = Map<String, dynamic>.from(response.data);
      return true;
    } catch (e) {
      if (owner == _owner) error = _message(e);
      return false;
    } finally {
      if (owner == _owner) { loading = false; notifyListeners(); }
    }
  }

  Future<bool> start() async {
    final owner = _owner;
    error = null;
    try {
      final response = await api.dio.post('/api/workout-sessions/start',
        data: {'workoutPlanId': plan!['id']});
      if (owner != _owner) return false;
      session = Map<String, dynamic>.from(response.data);
      notifyListeners();
      return true;
    } catch (e) {
      if (owner == _owner) error = _message(e);
      notifyListeners();
      return false;
    }
  }

  void startGuestWorkout() {
    error = null;
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
              'targetSets': exercise['defaultSets'],
              'targetReps': exercise['defaultReps'],
              'targetDurationSeconds': exercise['defaultDurationSeconds'],
              'restSeconds': exercise['defaultRestSeconds'],
              'isCompleted': false,
            },
          )
          .toList(),
    };
    notifyListeners();
  }

  // Keep Guest ownership on the session, independent of later auth changes.
  Future<void> complete(Map<String, dynamic> exercise) async {
    final active = session!;
    final rows = active['exercises'] as List;
    if (rows.any((row) => row['id'] == exercise['id'] && row['isCompleted'] == true)) return;
    error = null;
    try {
      if (active['isGuest'] == true) {
        final row = rows.firstWhere((row) => row['id'] == exercise['id']);
        row['isCompleted'] = true;
        row['completedSets'] = exercise['targetSets'];
        active['completedExercises'] = rows.where((row) => row['isCompleted'] == true).length;
      } else {
        session = Map<String, dynamic>.from((await api.dio.put(
          '/api/workout-sessions/${active['id']}/exercises/${exercise['id']}',
          data: {
            'completedSets': exercise['targetSets'],
            'completedReps': (exercise['targetReps'] ?? 0) * exercise['targetSets'],
            'completedDurationSeconds': (exercise['targetDurationSeconds'] ?? 0) * exercise['targetSets'],
            'isCompleted': true,
          },
        )).data);
      }
    } catch (e) {
      error = _message(e);
      rethrow;
    } finally {
      notifyListeners();
    }
  }

  Future<void> end({int? durationSeconds}) async {
    final active = session!;
    if ((active['exercises'] as List).any((row) => row['isCompleted'] != true)) {
      throw StateError('Chưa hoàn thành tất cả bài tập.');
    }
    error = null;
    try {
      if (active['isGuest'] == true) {
        active['durationSeconds'] = durationSeconds ?? 0;
        active['status'] = 'Completed';
      } else {
        // Reconcile a lost response before retrying the non-idempotent complete endpoint.
        final latest = Map<String, dynamic>.from(
          (await api.dio.get('/api/workout-sessions/${active['id']}')).data,
        );
        session = latest['status'] == 'Completed' ? latest : Map<String, dynamic>.from(
          (await api.dio.post('/api/workout-sessions/${active['id']}/complete')).data,
        );
      }
    } catch (e) {
      error = _message(e);
      rethrow;
    } finally {
      notifyListeners();
    }
  }

  Future<void> cancel() async {
    final active = session;
    if (active == null) return;
    error = null;
    try {
      if (active['isGuest'] != true) {
        final latest = Map<String, dynamic>.from(
          (await api.dio.get('/api/workout-sessions/${active['id']}')).data,
        );
        if (latest['status'] == 'InProgress') {
          await api.dio.post('/api/workout-sessions/${active['id']}/cancel');
        } else if (latest['status'] != 'Cancelled') {
          throw StateError('Buổi tập đã kết thúc. Hãy thử lưu lại để đồng bộ kết quả.');
        }
      }
      session = null;
    } catch (e) {
      error = _message(e);
      rethrow;
    } finally {
      notifyListeners();
    }
  }

  Future<bool> weight(double kg) async {
    if (api.isGuest) return false;
    try {
      await api.dio.post('/api/weight-records', data: {'weightKg': kg});
      await loadProgress();
      return true;
    } catch (e) {
      error = _message(e);
      notifyListeners();
      return false;
    }
  }
}
