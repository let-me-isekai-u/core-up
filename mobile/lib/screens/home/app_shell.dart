part of '../core_up_screens.dart';

class ShellScreen extends StatefulWidget {
  const ShellScreen({super.key, this.initialTab = 0});
  final int initialTab;
  @override
  State<ShellScreen> createState() => _ShellScreenState();
}

class _ShellScreenState extends State<ShellScreen> {
  late var _tab = widget.initialTab;
  final _pages = const [HomeScreen(), ProgressScreen(), ProfileScreen()];
  @override
  Widget build(BuildContext context) => Scaffold(
    body: IndexedStack(index: _tab, children: _pages),
    bottomNavigationBar: Theme(
      data: Theme.of(context).copyWith(
        navigationBarTheme: const NavigationBarThemeData(
          height: 68,
          indicatorColor: Color(0x33147BFF),
          iconTheme: WidgetStatePropertyAll(IconThemeData(color: blue)),
          labelTextStyle: WidgetStatePropertyAll(TextStyle(fontSize: 11)),
        ),
      ),
      child: NavigationBar(
        selectedIndex: _tab,
        backgroundColor: const Color(0xff0d1522),
        onDestinationSelected: (value) => setState(() => _tab = value),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.home_outlined),
            selectedIcon: Icon(Icons.home),
            label: 'Trang chủ',
          ),
          NavigationDestination(icon: Icon(Icons.show_chart), label: 'Tiến độ'),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            label: 'Cá nhân',
          ),
        ],
      ),
    ),
  );
}
