# CoreUp mobile

Flutter MVP client for the CoreUp ASP.NET API. It uses Provider/ChangeNotifier,
Dio, secure token storage, and the supplied local image assets.

## Requirements

- Flutter 3.41.5 (Dart 3.11.3)
- Xcode and an iOS Simulator
- Docker Desktop and .NET SDK 10 for the backend

## Run the backend

    cd .. # từ mobile/ về root repository
    docker compose up -d
    dotnet tool restore
    dotnet tool run dotnet-ef database update --project src/CoreUp.Infrastructure --startup-project src/CoreUp.Api
    dotnet run --project src/CoreUp.Api --launch-profile http

Verify it at http://127.0.0.1:5027/health; Swagger is at
http://127.0.0.1:5027/swagger.

## Run CoreUp

    cd mobile # từ root repository
    flutter pub get
    flutter run --dart-define=API_BASE_URL=http://127.0.0.1:5027

The iOS Info.plist contains a narrow development exception for local
127.0.0.1 HTTP. Assets are copied under assets/ without alteration.
Register a new account in the app; no demo account is seeded by the backend.
