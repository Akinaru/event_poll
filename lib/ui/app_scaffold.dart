import 'package:event_poll/states/auth_state.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class AppScaffold extends StatelessWidget {
  const AppScaffold({
    this.title,
    this.body,
    super.key,
  });

  final String? title;
  final Widget? body;

  @override
  Widget build(BuildContext context) {
    final authState = context.read<AuthState>();

    return Scaffold(
      appBar: AppBar(
        title: Text(title ?? 'Event Poll'),
        centerTitle: true,
      ),
      endDrawer: Drawer(
        child: ListView(
          padding: EdgeInsets.zero,
          children: <Widget>[
            DrawerHeader(
              child: authState.isLoggedIn
                  ? Text(
                      'Bienvenue ${authState.currentUser!.username} !\nVous êtes ${authState.currentUser!.role}')
                  : const Text(
                      'Connectez-vous pour vous inscrire à un événement !'),
            ),
            ListTile(
              leading: const Icon(Icons.event),
              title: const Text('Événements'),
              onTap: () {
                Navigator.pushNamedAndRemoveUntil(
                    context, '/polls', (_) => false);
              },
            ),
            !authState.isLoggedIn
                ? ListTile(
                    leading: const Icon(Icons.login),
                    title: const Text('Connexion'),
                    onTap: () {
                      Navigator.pushNamedAndRemoveUntil(
                          context, '/login', (_) => false);
                    },
                  )
                : const SizedBox(),
            !authState.isLoggedIn
                ? ListTile(
                    leading: const Icon(Icons.save_alt),
                    title: const Text('Inscription'),
                    onTap: () {
                      Navigator.pushNamedAndRemoveUntil(
                          context, '/signup', (_) => false);
                    },
                  )
                : const SizedBox(),
            authState.isLoggedIn
                ? ListTile(
                    leading: const Icon(Icons.logout),
                    title: const Text('Déconnexion'),
                    onTap: () {
                      authState.logout();
                      Navigator.pushNamedAndRemoveUntil(
                          context, '/polls', (_) => false);
                    },
                  )
                : const SizedBox(),
          ],
        ),
      ),
      body: SizedBox.expand(
        child: body,
      ),
    );
  }
}
