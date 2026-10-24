/// Proposed demo prices only; these are not store product identifiers.
class PremiumPlan {
  const PremiumPlan(this.months, this.price, {this.badge});
  final int months;
  final String price;
  final String? badge;

  static const options = [
    PremiumPlan(1, '144.000đ'),
    PremiumPlan(6, '600.000đ', badge: 'Gợi ý phổ biến'),
    PremiumPlan(12, '1.000.000đ', badge: 'Tiết kiệm nhất'),
  ];
}
