# Setup & Operations Guide

## Database setup

1. Ensure MySQL 8 is running.
2. Create database (already done if you followed README):

```sql
CREATE DATABASE IF NOT EXISTS FinanceManagementDB
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

3. Connection string (Development):

```
Server=localhost;Port=3306;Database=FinanceManagementDB;User=root;Password=;
```

Update `FinanceManagementApp.API/appsettings.Development.json` or User Secrets for your password.

4. Apply migrations:

```bash
cd FinanceManagementApp.API
dotnet ef database update
```

Seed data (admin/employee accounts, departments, categories) runs automatically on API startup.

## Backend setup

```bash
cd FinanceManagementApp.API
dotnet restore
dotnet build
dotnet run --launch-profile http
```

- API: http://localhost:5280
- Swagger: http://localhost:5280/swagger

## Mobile setup

```bash
cd FinanceManagementApp.Mobile
npm install
cp .env.example .env
npx expo start
```

### Android emulator

Use `EXPO_PUBLIC_API_BASE_URL=http://10.0.2.2:5280` (maps emulator localhost to host machine).

### Physical Android device

1. Find your PC LAN IP (`ipconfig`).
2. Set `EXPO_PUBLIC_API_BASE_URL=http://YOUR_LAN_IP:5280`.
3. Ensure phone and PC are on the same Wi-Fi and Windows Firewall allows port 5280.
4. Scan the Expo QR code with Expo Go.

## Build APK (Expo)

```bash
cd FinanceManagementApp.Mobile
npx expo install expo-dev-client
# For production builds, use EAS:
npm install -g eas-cli
eas build -p android --profile preview
```

Or export a local build after `npx expo prebuild` with Android Studio.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| API won't start / empty upload path | Ensure Upload:Directory is blank or a valid path |
| MySQL connection refused | Start MySQL service; verify port 3306 |
| Login works in Swagger but not on emulator | Use `10.0.2.2` not `localhost` |
| 401 after some time | JWT expired; log in again |
| Receipt upload fails | Only JPG/JPEG/PNG/PDF under 5MB; content must match type |
| Employee gets 403 on /api/employees | Expected — admin-only endpoint |

## Test credentials (development only)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@financeapp.com | Admin@123 |
| Employee | employee@financeapp.com | Employee@123 |
