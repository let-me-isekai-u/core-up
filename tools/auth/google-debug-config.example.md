# Owner-provided OAuth configuration

No Client Secret is needed. Obtain real Web and iOS OAuth client IDs from the same
Google Cloud project. iOS bundle: `com.coreup.coreUpMobile`. Configure consent screen
Audience and test users if in Testing. Do not substitute Android IDs for the Web ID.

Run from repository root after setting real values in the shell:

```sh
export GOOGLE_SERVER_CLIENT_ID='REPLACE_WITH_REAL_WEB_CLIENT_ID'
export GOOGLE_IOS_CLIENT_ID='REPLACE_WITH_REAL_IOS_CLIENT_ID'
python3 tools/auth/configure_google_ios.py \
  --ios-client-id "$GOOGLE_IOS_CLIENT_ID" \
  --web-client-id "$GOOGLE_SERVER_CLIENT_ID"
```

Review Info.plist: GIDClientID, GIDServerClientID and CFBundleURLTypes must match.
The script derives the reversed scheme from the actual iOS ID. The boolean below
only acknowledges this manual native setup; it cannot install a callback scheme.

Owner-run backend environment, in the process that starts the API:

```sh
export Google__ClientIds__0="$GOOGLE_SERVER_CLIENT_ID"
```

Owner-run Flutter debug command from `mobile/`, selecting the actual iOS Simulator:

```sh
flutter run -d "$IOS_SIMULATOR_ID" \
  --dart-define=API_BASE_URL=http://localhost:5027 \
  --dart-define=GOOGLE_SERVER_CLIENT_ID="$GOOGLE_SERVER_CLIENT_ID" \
  --dart-define=GOOGLE_IOS_CLIENT_ID="$GOOGLE_IOS_CLIENT_ID" \
  --dart-define=GOOGLE_IOS_URL_SCHEME_CONFIGURED=true
```

Confirm API_BASE_URL against `mobile/lib/providers/auth_provider.dart` before launch.
A full restart is required after changing defines/native configuration. These commands
are instructions only; no runtime/build/Simulator was launched during implementation.

Diagnostics: missing define errors occur before SDK launch; native configuration
errors identify client/callback setup; consent rejection requires Cloud configuration;
backend 503 means no allowlist or Google verification network; 401 means token validation
failed (audience failures are classified separately); 409 email collision requires
existing Core-Up login and explicit password-proven linking, never automatic merge.
Never log ID tokens or paste them into diagnostic tools.
