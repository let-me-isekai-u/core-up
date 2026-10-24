part of '../core_up_screens.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});
  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (context.read<AuthProvider>().isAuthenticated) {
        context.read<ProfileProvider>().load();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final guest = context.watch<AuthProvider>().isGuest;
    return SafeArea(
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const Text(
            'HỒ SƠ',
            style: TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 22),
          if (guest) ..._guest(context) else ..._authenticated(context),
        ],
      ),
    );
  }

  List<Widget> _guest(BuildContext context) => [
    SurfaceCard(
      child: Column(
        children: [
          const CircleAvatar(
            radius: 32,
            backgroundColor: AppColors.surfaceElevated,
            child: Icon(Icons.person_outline_rounded, size: 34, color: blue),
          ),
          const SizedBox(height: 14),
          const Text(
            'Bạn đang dùng chế độ khách',
            style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          const Text(
            'Tập nhanh thoải mái, không cần tạo tài khoản.',
            textAlign: TextAlign.center,
            style: TextStyle(color: muted),
          ),
          const SizedBox(height: 18),
          PrimaryButton(label: 'ĐĂNG NHẬP', onTap: () => openLogin(context)),
        ],
      ),
    ),
    const SizedBox(height: 14),
    const SectionLabel('KHI ĐĂNG NHẬP BẠN CÓ THỂ'),
    const SurfaceCard(
      child: Column(
        children: [
          BenefitLine('Giáo trình chuẩn theo mức'),
          BenefitLine('Lưu lịch sử tập'),
          BenefitLine('Theo dõi cân nặng'),
          BenefitLine('Xem tiến độ qua từng tuần'),
        ],
      ),
    ),
    const SizedBox(height: 14),
    const SectionLabel('CÀI ĐẶT'),
    const SurfaceCard(
      child: Column(
        children: [
          ListTile(
            contentPadding: EdgeInsets.zero,
            leading: Icon(Icons.dark_mode_outlined),
            title: Text('Chủ đề'),
            trailing: Icon(Icons.chevron_right_rounded),
          ),
          Divider(),
          ListTile(
            contentPadding: EdgeInsets.zero,
            leading: Icon(Icons.info_outline_rounded),
            title: Text('Giới thiệu'),
            trailing: Icon(Icons.chevron_right_rounded),
          ),
        ],
      ),
    ),
  ];

  List<Widget> _authenticated(BuildContext context) {
    final profile = context.watch<ProfileProvider>().data;
    return [
      SurfaceCard(
        child: Row(
          children: [
            const CircleAvatar(
              radius: 30,
              backgroundColor: blue,
              child: Icon(Icons.person_rounded, size: 30, color: Colors.white),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    formatDisplayName(
                      profile?['fullName']?.toString() ?? 'Đang tải…',
                    ),
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(
                    '${localizeValue((profile?['goal'] ?? '').toString())} • ${localizeValue((profile?['fitnessLevel'] ?? '').toString())}',
                    style: const TextStyle(color: muted),
                  ),
                ],
              ),
            ),
            TextButton(onPressed: () => Navigator.of(context).push(fadeRoute(const ProfileSetupScreen(review: true))), child: const Text('Mức tập')),
          ],
        ),
      ),
      const SizedBox(height: 14),
      ListTile(
        leading: const Icon(Icons.workspace_premium_outlined),
        title: const Text('Khám phá Premium'),
        subtitle: const Text('Giới thiệu quyền lợi và gói giá đề xuất'),
        trailing: const Icon(Icons.chevron_right_rounded),
        onTap: () => _openPremiumPromotion(context, manual: true),
      ),
      ListTile(
        leading: const Icon(Icons.link_rounded),
        title: const Text('Liên kết Google'),
        subtitle: const Text('Xác nhận mật khẩu Core-Up và Google cùng email'),
        onTap: () => _linkGoogleAccount(context),
      ),
      const SectionLabel('CHỈ SỐ TÙY CHỌN'),
      Row(
        children: [
          StatCard('BMI', profile?['bmi']?.toString() ?? 'Chưa cung cấp'),
          const SizedBox(width: 10),
          StatCard(
            'CÂN NẶNG',
            profile?['weightKg'] == null ? 'Chưa cung cấp' : '${profile!['weightKg']} kg',
          ),
        ],
      ),
      const SizedBox(height: 14),
      const SectionLabel('MỤC TIÊU'),
      SurfaceCard(
        child: InfoLine(
          'Cân nặng mục tiêu',
          profile?['targetWeightKg'] == null
              ? '—'
              : '${profile!['targetWeightKg']} kg',
        ),
      ),
      const SizedBox(height: 14),
      const SectionLabel('CÀI ĐẶT'),
      SurfaceCard(
        child: Column(
          children: [
            ListTile(
              leading: const Icon(Icons.security_outlined),
              title: const Text('Bảo mật'),
              subtitle: const Text('Đổi mật khẩu'),
              onTap: () => Navigator.of(
                context,
              ).push(fadeRoute(const ChangePasswordScreen())),
            ),
            if (context.watch<AuthProvider>().needsEmailVerification)
              ListTile(
                title: const Text('Xác minh email'),
                onTap: () => Navigator.of(
                  context,
                ).push(fadeRoute(const EmailVerificationScreen())),
              ),
          ],
        ),
      ),

      SurfaceCard(
        child: ListTile(
          contentPadding: EdgeInsets.zero,
          leading: const Icon(Icons.manage_accounts_outlined),
          title: const Text('Cập nhật số đo (tùy chọn)'),
          subtitle: const Text('Mỗi tháng hoặc để sau; không đổi giáo trình'),
          trailing: const Icon(Icons.chevron_right_rounded),
          onTap: () => Navigator.of(context).push(fadeRoute(
            profile == null ? const ProfileSetupScreen(review: true) : const MeasurementsScreen(),
          )),
        ),
      ),
      TextButton.icon(
        onPressed: () async {
          await context.read<AuthProvider>().logout();
          if (!context.mounted) return;
          Navigator.of(
            context,
          ).pushAndRemoveUntil(fadeRoute(const AuthScreen()), (_) => false);
        },
        icon: const Icon(Icons.logout_rounded, color: Colors.redAccent),
        label: const Text(
          'Đăng xuất',
          style: TextStyle(color: Colors.redAccent),
        ),
      ),
    ];
  }
}

class BenefitLine extends StatelessWidget {
  const BenefitLine(this.text, {super.key});
  final String text;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 7),
    child: Row(
      children: [
        const Icon(
          Icons.check_circle_rounded,
          size: 20,
          color: AppColors.accent,
        ),
        const SizedBox(width: 10),
        Text(text),
      ],
    ),
  );
}
