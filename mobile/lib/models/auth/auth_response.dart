class AuthResponse {
  const AuthResponse({
    required this.userId,
    required this.email,
    required this.accessToken,
    required this.needsProfile,
  });

  final String userId;
  final String email;
  final String accessToken;
  final bool needsProfile;

  factory AuthResponse.fromJson(Map<String, dynamic> json) => AuthResponse(
    userId: json['userId'].toString(),
    email: json['email'].toString(),
    accessToken: json['accessToken'].toString(),
    needsProfile: json['needsProfile'] == true,
  );
}
