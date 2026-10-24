part of '../core_up_screens.dart';

bool _premiumEligible(BuildContext context, {required bool manual}) {
  final auth = context.read<AuthProvider>();
  final profile = context.read<ProfileProvider>();
  final workout = context.read<WorkoutProvider>();
  final session = workout.session;
  return auth.isAuthenticated &&
      !auth.submitting &&
      !(auth.verificationRequired && auth.needsEmailVerification) &&
      profile.data?['id'] != null &&
      !profile.loading &&
      profile.error == null &&
      !(workout.adPolicy.entitlement?.hasPremium ?? false) &&
      (session == null ||
          session['status'] == 'Completed' ||
          session['status'] == 'Cancelled') &&
      (manual ||
          (profile.data?['assessment'] != null &&
              workout.plan != null &&
              !workout.loading &&
              workout.error == null)) &&
      (ModalRoute.of(context)?.isCurrent ?? false);
}

Future<void> _openPremiumPromotion(
  BuildContext context, {
  bool manual = false,
  bool Function()? homeActive,
}) async {
  final policy = context.read<PremiumPromotionPolicy>();
  if (policy.presenting || !_premiumEligible(context, manual: manual)) return;
  final account = context.read<ProfileProvider>().data!['id'].toString();
  final token = context.read<AuthProvider>().token;
  if (!manual) {
    try {
      if (!await policy.isDue(account)) return;
    } catch (_) {
      // Fail closed for automatic promotion when persistence is unavailable.
      return;
    }
  }
  if (!context.mounted ||
      policy.presenting ||
      (!manual && homeActive?.call() != true) ||
      context.read<AuthProvider>().token != token ||
      context.read<ProfileProvider>().data?['id'].toString() != account ||
      !_premiumEligible(context, manual: manual))
    return;
  policy.presenting = true;
  try {
    // Reserve before navigation to prevent duplicate callbacks/rebuilds.
    try {
      await policy.record(account);
    } catch (_) {
      if (!manual) return;
    }
    if (!context.mounted ||
        context.read<AuthProvider>().token != token ||
        (!manual && homeActive?.call() != true) ||
        !_premiumEligible(context, manual: manual))
      return;
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => const PremiumPaywallScreen(),
        fullscreenDialog: true,
      ),
    );
    // X, Free, and platform back all record dismissal without delaying pop.
    try {
      await policy.record(account);
    } catch (_) {
      /* Keep in-memory cap. */
    }
  } finally {
    policy.presenting = false;
  }
}

/// Mounted only around the main shell; no timers and no workout/ad hooks.
class PremiumPromotionGate extends StatefulWidget {
  const PremiumPromotionGate({
    super.key,
    required this.homeActive,
    required this.child,
  });
  final bool homeActive;
  final Widget child;
  @override
  State<PremiumPromotionGate> createState() => _PremiumPromotionGateState();
}

class _PremiumPromotionGateState extends State<PremiumPromotionGate>
    with RouteAware {
  ModalRoute<dynamic>? _route;
  bool _queued = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final route = ModalRoute.of(context);
    if (_route != route) {
      exerciseVisualRouteObserver.unsubscribe(this);
      _route = route;
      if (route != null) exerciseVisualRouteObserver.subscribe(this, route);
    }
  }

  void _schedule() {
    if (_queued || !widget.homeActive) return;
    _queued = true;
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      if (mounted && widget.homeActive) {
        await _openPremiumPromotion(
          context,
          homeActive: () => mounted && widget.homeActive,
        );
      }
      _queued = false;
    });
  }

  @override
  void didPopNext() => _schedule();

  @override
  void dispose() {
    exerciseVisualRouteObserver.unsubscribe(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    context.watch<AuthProvider>();
    context.watch<ProfileProvider>();
    context.watch<WorkoutProvider>();
    _schedule();
    return widget.child;
  }
}
