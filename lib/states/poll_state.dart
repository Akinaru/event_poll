import 'package:flutter/material.dart';

import '../models/poll.dart';

class PollsState extends ChangeNotifier {
  Poll? _currentPoll;
  String? _token;

  Poll? get currentPoll => _currentPoll;

  void setCurrentPoll(Poll? poll) {
    _currentPoll = poll;
    notifyListeners();
  }

  setAuthToken(String? token) {
    _token = token;
    notifyListeners();
  }
}
