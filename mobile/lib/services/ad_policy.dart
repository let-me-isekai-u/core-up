/// Only a verified Premium entitlement removes advertisements.
/// Inject an implementation backed by the authenticated account when available.
abstract interface class PremiumEntitlement {
  bool get hasPremium;
}

class AdPolicy {
  const AdPolicy({this.entitlement});
  final PremiumEntitlement? entitlement;

  bool shouldShowAds({required bool isGuest}) =>
      isGuest || !(entitlement?.hasPremium ?? false);
}
