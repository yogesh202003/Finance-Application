# Finance Management Application

A complete full-stack finance management system: expense submission, approval workflows, receipts, notifications, transactions, reports, and audit logging.

## Architecture

```
React Native (Expo) Mobile App
        │  REST + JWT
        ▼
ASP.NET Core Web API (.NET 9)
        │  EF Core
        ▼
MySQL (FinanceManagementDB)
```

The mobile app never connects directly to MySQL.

## Technology stack

| Layer | Tech |
|-------|------|
| Mobile | React Native (Expo 57), TypeScript, React Navigation, Axios, React Hook Form, Zod, AsyncStorage, react-native-chart-kit, expo-document-picker |
| Backend | ASP.NET Core 9 Web API, EF Core, Pomelo MySQL, JWT, BCrypt, Swagger, QuestPDF, CsvHelper |
| Database | MySQL 8 |
| Tests | xUnit + WebApplicationFactory |

## Project structure

```
FinanceManagementApp/
├── FinanceManagementApp.API/          # ASP.NET Core Web API
├── FinanceManagementApp.API.Tests/    # Integration tests
├── FinanceManagementApp.Mobile/       # Expo React Native app
├── database/                          # SQL helpers
├── docs/                              # Extra documentation
├── uploads/                           # Receipt storage (local)
├── README.md
└── .gitignore
```

## Prerequisites

- Node.js 20+ / npm
- .NET SDK 9
- MySQL 8
- Android Studio / emulator or Expo Go on a device
- Git

## Development credentials

> Seed data for local development only. Never use in production.

| Role | Email | Password | Notes |
|------|-------|----------|-------|
| Admin | admin@financeapp.com | Admin@123 | Full admin access |
| Employee | employee@financeapp.com | Employee@123 | Employee code `EMP001` |

Additional sample employees: `EMP002`–`EMP004` (password `Employee@123`).

## Database setup

```bash
# MySQL
mysql -u root -e "CREATE DATABASE IF NOT EXISTS FinanceManagementDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
```

Default connection (update if your MySQL has a password):

```
Server=localhost;Port=3306;Database=FinanceManagementDB;User=root;Password=;
```

Copy `FinanceManagementApp.API/appsettings.Example.json` when configuring a new environment. Prefer User Secrets / environment variables for real credentials.

## Backend setup

```bash
cd FinanceManagementApp.API
dotnet restore
dotnet ef database update
dotnet run --launch-profile http
```

- API base URL: **http://localhost:5280**
- Swagger UI: **http://localhost:5280/swagger** (JWT Authorize supported)

Migrations and seed data apply automatically on startup (`DbSeeder`).

### Run API tests

```bash
cd FinanceManagementApp.API.Tests
dotnet test
```

## Mobile setup

```bash
cd FinanceManagementApp.Mobile
npm install
cp .env.example .env
npx expo start
```

Then press `a` for Android emulator, or scan the QR code with Expo Go.

### Environment configuration

| Target | `EXPO_PUBLIC_API_BASE_URL` |
|--------|----------------------------|
| Android emulator | `http://10.0.2.2:5280` |
| iOS simulator | `http://localhost:5280` |
| Physical device | `http://<YOUR_PC_LAN_IP>:5280` |

### Physical Android device

1. Run `ipconfig` and note your IPv4 address.
2. Set that IP in `.env`.
3. Allow inbound TCP 5280 in Windows Firewall.
4. Ensure phone and PC share the same Wi-Fi network.
5. Start the API, then open Expo Go and scan the project QR.

### Build APK

```bash
npm install -g eas-cli
eas login
cd FinanceManagementApp.Mobile
eas build -p android --profile preview
```

Alternatively: `npx expo prebuild` then build with Android Studio.

## Key features

- JWT login (email / username / employee code), change & forgot password
- Role-based API authorization (`ADMIN` / `EMPLOYEE`) and role navigators
- Admin dashboard with live charts, pending approvals, recent transactions
- Employee dashboard with submit shortcut and recent expenses
- Employee CRUD, search, activate/deactivate, password reset
- Departments & expense categories
- Expense create/update with receipt upload (JPG/PNG/PDF)
- Approve → transaction + notification + audit log
- Reject (comment required) → notification + audit log
- Notifications (unread count, mark read / read all)
- Reports (monthly, employee, department, category) with CSV/PDF export
- Audit logs (admin only)
- Server-side pagination, search, and filtering
- Global exception middleware and structured API errors

## Swagger quick test

1. Open http://localhost:5280/swagger
2. `POST /api/auth/login` with admin credentials
3. Click **Authorize** → `Bearer {token}`
4. Exercise employee, expense, approval, and report endpoints

## Troubleshooting

See [docs/SETUP.md](docs/SETUP.md) for detailed troubleshooting.

Common issues:

- Emulator cannot reach API → use `10.0.2.2`, not `localhost`
- MySQL auth fails → update connection string password
- 403 on admin routes as employee → expected server-side RBAC
- Token expired → log in again

## License

For educational / development use.
