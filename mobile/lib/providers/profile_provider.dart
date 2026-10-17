import 'package:flutter/foundation.dart';

import 'auth_provider.dart';

class ProfileProvider extends ChangeNotifier {
  ProfileProvider(this.api);
  final AuthProvider api;
  Map<String, dynamic>? data;
  bool loading = false;
  String? error;
  Future<void> load() async {
    if (api.isGuest) return;
    loading = true;
    notifyListeners();
    try {
      data = Map<String, dynamic>.from(
        (await api.dio.get('/api/profile')).data,
      );
    } catch (e) {
      error = api.message(e);
    }
    loading = false;
    notifyListeners();
  }

  Future<bool> save(Map<String, dynamic> body) async {
    loading = true;
    notifyListeners();
    try {
      data = Map<String, dynamic>.from(
        (await api.dio.post('/api/profile', data: body)).data,
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
}
