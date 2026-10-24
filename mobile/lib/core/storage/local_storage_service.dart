import 'dart:io' show pid;

import 'package:shared_preferences/shared_preferences.dart';

class LocalStorageService {
  static const _onboardingKey = 'has_seen_onboarding';
  static const _sessionModeKey = 'app_session_mode';
  static const _debugProcessKey = 'debug_process_id';

  Future<DateTime?> premiumPromotionTime(String account) async {
    final preferences = await SharedPreferences.getInstance();
    return DateTime.tryParse(preferences.getString('premium_promotion_at_$account') ?? '');
  }

  Future<void> savePremiumPromotionTime(String account, DateTime time) async {
    final preferences = await SharedPreferences.getInstance();
    if (!await preferences.setString('premium_promotion_at_$account', time.toIso8601String())) {
      throw StateError('Could not save promotion history.');
    }
  }

  Future<bool> hasPendingPremiumIntroduction(String account) async {
    final preferences = await SharedPreferences.getInstance();
    return preferences.getBool('premium_introduction_pending_$account') ?? false;
  }

  Future<void> markPendingPremiumIntroduction(String account) async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.setBool('premium_introduction_pending_$account', true);
  }

  Future<void> clearPendingPremiumIntroduction(String account) async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.remove('premium_introduction_pending_$account');
  }

  Future<bool> hasSeenOnboarding() async {
    final preferences = await SharedPreferences.getInstance();
    return preferences.getBool(_onboardingKey) ?? false;
  }

  Future<void> markOnboardingSeen() async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.setBool(_onboardingKey, true);
  }

  Future<String?> sessionMode() async {
    final preferences = await SharedPreferences.getInstance();
    return preferences.getString(_sessionModeKey);
  }

  Future<void> saveSessionMode(String mode) async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.setString(_sessionModeKey, mode);
  }

  Future<void> clearSessionMode() async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.remove(_sessionModeKey);
  }

  Future<bool> isMeasurementReviewDeferred(String profileId) async {
    final preferences = await SharedPreferences.getInstance();
    final until = DateTime.tryParse(preferences.getString('measurement_review_after_$profileId') ?? '');
    return until != null && DateTime.now().isBefore(until);
  }

  Future<void> deferMeasurementReview(String profileId) async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.setString('measurement_review_after_$profileId',
      DateTime.now().add(const Duration(days: 30)).toIso8601String());
  }

  Future<bool> isNewDebugProcess() async {
    final preferences = await SharedPreferences.getInstance();
    final previousProcess = preferences.getInt(_debugProcessKey);
    await preferences.setInt(_debugProcessKey, pid);
    return previousProcess != pid;
  }
}
