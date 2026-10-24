# Google Sign-In — iOS / Android configuration

Source: IMPLEMENTED — NOT YET VERIFIED. Dependency resolution completed with
`google_sign_in 7.2.0` (Android 7.2.11, iOS 6.3.8), compatible with this repository's
Flutter 3.41.5 / Dart 3.11.3. Backend uses `Google.Apis.Auth 1.77.0`.
No Google account authentication was performed. No OAuth IDs were invented.

## Google Cloud

Create/configure the OAuth consent screen and authorized test users as needed.
Create three appropriate OAuth client configurations in the same project:

- iOS: actual bundle identifier `com.coreup.coreUpMobile`.
- Android: actual application ID `com.coreup.core_up_mobile`; register the real
  development/release signing SHA-1/SHA-256 fingerprints for the intended builds.
  The owner can inspect the chosen keystore with `keytool -list -v -keystore PATH`.
- Web/server: use its client ID as the audience for backend ID tokens.

The ID-token flow does not require a client secret in Flutter. Keep all private
credentials outside Git. Do not configure macOS Desktop; the service rejects
non-iOS/Android platforms. Transitive web/macOS plugin packages are SDK packaging,
not additional Core-Up platform support.

## iOS

After obtaining actual values, the owner can configure Info.plist with:

```sh
python3 tools/auth/configure_google_ios.py \
  --ios-client-id "$GOOGLE_IOS_CLIENT_ID" \
  --web-client-id "$GOOGLE_SERVER_CLIENT_ID"
```

This script merges GIDClientID, GIDServerClientID and the reversed iOS client ID
into CFBundleURLTypes, preserving other URL schemes. It was not run because real
IDs are missing. Review its diff before committing configuration. Do not paste a
sample ID into the app. Initialize Flutter with matching compile-time defines:
`GOOGLE_IOS_CLIENT_ID`, `GOOGLE_SERVER_CLIENT_ID`, and
`GOOGLE_IOS_URL_SCHEME_CONFIGURED=true` only after configuring the real scheme.
Without these, the app reports a configuration error before calling native auth.

## Android

Register package/signing certificates in Google Cloud and pass the real Web
client ID via `GOOGLE_SERVER_CLIENT_ID`. This integration uses explicit
serverClientId initialization, so a fabricated google-services.json is unnecessary.
Check the actual signing flavor; a matching package name with a wrong certificate
will not authenticate. Native CocoaPods/Gradle resolution and device build remain
owner verification steps; no Simulator/Emulator/build ran here.

## Backend

Configure the explicit audience allowlist, e.g. environment variable
`Google__ClientIds__0` with the real Web OAuth client ID used by Flutter. Add more
indices only for independently trusted client IDs that actually issue this app's
tokens. Empty configuration returns 503. Startup does not invent credentials.
Production API_BASE_URL must use HTTPS; Flutter only allows HTTP for loopback /
Android host bridge in debug builds.

`POST /api/auth/google` takes `{idToken}`. The Google library verifies signed token,
issuer, audience and expiry with Google's public certificates. Additional checks
require subject, valid verified email and accepted issuer. No tokeninfo debug
endpoint or unverified Base64 claims are used.

Identity mapping uses unique (Provider, ProviderSubject), and unique
(UserId, Provider). An existing subject resolves the original Core-Up account even
if Google's email changes; no IDs/history are replaced. For a new identity without
email conflict, create a normal User with a random valid `g_…` username and NULL
PasswordHash. Password login rejects null/empty hashes. Google's verified Gmail /
Workspace email satisfies email verification; third-party Google email addresses
still use Core-Up email ownership verification because Google may not remain
mailbox-authoritative. No fake password or Premium/Admin grant is created.

Email collisions return a clear 409 and never merge. Authenticated users may use
Profile → Liên kết Google: current Core-Up password + verified Core-Up email +
verified Google ID token with the same email are required by
`POST /api/auth/google/link`. Unique constraints and TokenVersion concurrency guard
linking races. Successful linking invalidates sessions; sign in again. No unlink
endpoint exists, so it cannot strand an account without credentials. Google-only
accounts can use the existing email reset flow to set their own password.

Flutter initializes the v7 singleton once per successful initialization, observes
authentication events, uses user-initiated authenticate and reads ID token. Only
the resulting Core-Up JWT is saved in secure storage. Google cancellation returns
to auth without creating a session; SDK/network/config/token/conflict errors allow
retry. Existing Guest, password registration/login, recovery and questionnaire
routes are retained. A backend failure never becomes fake Google success.

## Schema and manual checks

Requires `20261024120000_AdminPremiumGoogleAccess` after existing migrations;
see [admin setup](../admin/README.md). Migration is hand-authored and unverified,
not applied. Confirm model drift and upgrade SQL on a disposable DB copy first.

Manual checks: cancellation, SDK configuration failure, wrong audience/signature/
issuer/expiry, new Gmail and third-party email, repeat subject, email collision,
explicit link and concurrent password reset/link, account switch/logout/restore,
Google-only password rejection/recovery, and unchanged Free history/entitlement.
No automated tests were added or run. Native Google auth is NOT YET VERIFIED.

Official references consulted 24/10/2026:
- https://pub.dev/packages/google_sign_in
- https://pub.dev/packages/google_sign_in_ios
- https://pub.dev/packages/google_sign_in_android
- https://developers.google.com/identity/sign-in/ios/backend-auth
- https://developers.google.com/identity/openid-connect/openid-connect
- https://docs.cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
- https://www.nuget.org/packages/Google.Apis.Auth/1.77.0
