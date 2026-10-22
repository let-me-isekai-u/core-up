import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import 'auth_provider.dart';

class ProfileProvider extends ChangeNotifier {
  ProfileProvider(this.api);
  final AuthProvider api;
  Map<String, dynamic>? data;
  bool loading = false, missing = false;
  String? error, _owner;

  void syncAccount() {
    final owner = '${api.status.name}:${api.token}';
    if (_owner == owner) return;
    _owner = owner;
    data = null;
    missing = false;
    error = null;
    loading = false;
  }

  Future<bool> load() async {
    if (!api.isAuthenticated) return false;
    syncAccount();
    final owner = _owner;
    loading = true;
    missing = false;
    error = null;
    notifyListeners();
    try {
      final response = await api.dio.get('/api/profile');
      if (owner != _owner) return false;
      data = Map<String, dynamic>.from(response.data);
      return true;
    } catch (e) {
      if (owner != _owner) return false;
      missing = e is DioException && e.response?.statusCode == 404;
      if (missing) data = null;
      error = missing ? null : _message(e);
      return false;
    } finally {
      if (owner == _owner) {
        loading = false;
        notifyListeners();
      }
    }
  }

  String _message(Object error) {
    if (error is DioException && error.response?.data is Map) {
      final body = error.response!.data as Map;
      if (body['detail'] is String) return body['detail'] as String;
      if (body['errors'] is Map) return 'Vui lòng kiểm tra câu trả lời, mức tập và số đo đã nhập.';
    }
    return api.message(error);
  }

  Future<Map<String, dynamic>?> recommend(List<int> answers) async {
    error = null;
    try {
      final response = await api.dio.post('/api/profile/fitness-assessment/preview',
        data: {'answers': answers});
      return Map<String, dynamic>.from(response.data);
    } catch (e) {
      error = _message(e);
      notifyListeners();
      return null;
    }
  }

  Future<bool> saveAssessment(Map<String, dynamic> body) =>
      _save('/api/profile/fitness-assessment', body);

  Future<bool> saveMeasurements(Map<String, dynamic> body) =>
      _save('/api/profile/measurements', body, put: true);

  Future<bool> save(Map<String, dynamic> body) => _save('/api/profile', body);

  Future<bool> _save(String path, Map<String, dynamic> body, {bool put = false}) async {
    syncAccount();
    final owner = _owner;
    loading = true;
    error = null;
    notifyListeners();
    try {
      final response = put ? await api.dio.put(path, data: body) : await api.dio.post(path, data: body);
      if (owner != _owner) return false;
      data = Map<String, dynamic>.from(response.data);
      missing = false;
      api.needsProfile = false;
      return true;
    } catch (e) {
      if (owner == _owner) error = _message(e);
      return false;
    } finally {
      if (owner == _owner) {
        loading = false;
        notifyListeners();
      }
    }
  }
}
