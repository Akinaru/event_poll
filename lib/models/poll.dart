import 'user.dart';

class Poll {
  Poll({
    required this.name,
    required this.description,
    required this.eventDate,
    required this.user,
  });
  final String name;
  final String description;
  final DateTime eventDate;
  final User user;
}
