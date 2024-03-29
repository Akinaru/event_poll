import 'package:flutter/material.dart';

class PollsState extends ChangeNotifier {
  Poll? _currentPoll;

  // Méthode pour obtenir l'événement actuel
  Poll? get currentPoll => _currentPoll;

  // Méthode pour définir l'événement actuel
  void setCurrentPoll(Poll? poll) {
    _currentPoll = poll;
    notifyListeners(); // Notifie les écouteurs que l'état a changé
  }

  // Ajoutez ici d'autres méthodes nécessaires pour obtenir ou modifier les événements via des appels au serveur
}
