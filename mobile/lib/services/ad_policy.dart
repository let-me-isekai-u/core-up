/// Verified Premium removes ads; unresolved access defers ads without granting Premium.
/// Inject an implementation backed by the authenticated account when available.
abstract interface class PremiumEntitlement {
  bool get hasPremium;
  bool get isKnown;
}

class AdPolicy {
  const AdPolicy({this.entitlement});
  final PremiumEntitlement? entitlement;

  bool shouldShowAds({required bool isGuest}) =>
      isGuest ||
      (entitlement == null ||
          (entitlement!.isKnown && !entitlement!.hasPremium));
}
