class AuthResponse {
  const AuthResponse({
    required this.userId,
    required this.email,
    required this.accessToken,
    required this.needsProfile,
    this.needsEmailVerification = false,
    this.verificationRequired = false,
  });

  final String userId;
  final String email;
  final String accessToken;
  final bool needsProfile;
  final bool needsEmailVerification;
  final bool verificationRequired;

  factory AuthResponse.fromJson(Map<String, dynamic> json) => AuthResponse(
    userId: json['userId'].toString(),
    email: json['email'].toString(),
    accessToken: json['accessToken'].toString(),
    needsProfile: json['needsProfile'] == true,
    needsEmailVerification: json['needsEmailVerification'] == true,
    verificationRequired: json['verificationRequired'] == true,
  );
}
