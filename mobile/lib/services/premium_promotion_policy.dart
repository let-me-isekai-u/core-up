import '../core/storage/local_storage_service.dart';

/// UI promotion history only. Never stores subscription or entitlement state.
class PremiumPromotionPolicy {
  PremiumPromotionPolicy(this.storage);
  final LocalStorageService storage;
  final Set<String> _shownThisSession = {};
  bool presenting = false;
  static const cooldown = Duration(days: 7);

  Future<bool> isDue(String account) async {
    if (presenting || _shownThisSession.contains(account)) return false;
    final last = await storage.premiumPromotionTime(account);
    if (last != null) return DateTime.now().difference(last) >= cooldown;
    // Existing questionnaire users start a cooldown; login is not a first
    // completion event. Only a successful new assessment sets this marker.
    if (await storage.hasPendingPremiumIntroduction(account)) return true;
    await storage.savePremiumPromotionTime(account, DateTime.now());
    return false;
  }

  Future<void> record(String account) async {
    _shownThisSession.add(account);
    await storage.savePremiumPromotionTime(account, DateTime.now());
    await storage.clearPendingPremiumIntroduction(account);
  }
}
