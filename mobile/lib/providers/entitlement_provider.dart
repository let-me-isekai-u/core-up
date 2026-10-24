import 'package:flutter/foundation.dart';
import '../services/ad_policy.dart';
import 'auth_provider.dart';

class EntitlementProvider extends ChangeNotifier implements PremiumEntitlement {
  EntitlementProvider(this.auth);
  final AuthProvider auth;
  String? _owner;
  bool _premium = false;
  DateTime? _expiresAt;
  Future<void>? _pending;
  bool loading = false;
  bool loaded = false;
  String status = 'Unknown';
  String? source;
  String? error;

  @override
  bool get isKnown => loaded && !loading && _owner == auth.token;

  @override
  bool get hasPremium =>
      auth.isAuthenticated &&
      _owner == auth.token &&
      _premium &&
      (_expiresAt == null || DateTime.now().toUtc().isBefore(_expiresAt!));

  void syncAccount() {
    final owner = auth.isAuthenticated ? auth.token : null;
    if (_owner == owner) return;
    _owner = owner;
    _premium = false;
    _expiresAt = null;
    loaded = false;
    status = 'Unknown';
    source = null;
    error = null;
    _pending = null;
    loading = false;
    // ProxyProvider updates happen during build; defer notifications/network.
    Future.microtask(refresh);
  }

  Future<void> refresh() {
    if (!auth.isAuthenticated || _owner == null) return Future.value();
    return _pending ??= _load(_owner!);
  }

  Future<void> _load(String owner) async {
    loading = true;
    notifyListeners();
    try {
      final response = await auth.dio.get('/api/account/entitlement');
      if (_owner != owner || auth.token != owner) return;
      final data = Map<String, dynamic>.from(response.data);
      _premium = data['hasPremium'] == true;
      _expiresAt = DateTime.tryParse(data['expiresAt']?.toString() ?? '');
      status = data['status']?.toString() ?? 'Unknown';
      source = data['source']?.toString();
      // A malformed expiry must not become a non-expiring grant.
      if (data['expiresAt'] != null && _expiresAt == null) _premium = false;
      loaded = true;
      error = null;
    } catch (_) {
      if (_owner != owner) return;
      _premium = false;
      loaded = false;
      status = 'Unknown';
      error = 'Chưa xác minh được quyền Premium.';
    } finally {
      if (_owner == owner) {
        loading = false;
        _pending = null;
        notifyListeners();
      }
    }
  }
}
