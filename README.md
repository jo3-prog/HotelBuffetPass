# Hotel Buffet Pass System

A 5-star hotel digital buffet pass system built with ASP.NET Core MVC (.NET 8).

## Step 1 Complete: Foundation
- Models: ApplicationUser, Event, GuestRegistration, ScanLog
- Identity authentication with 3 roles: Admin, ContactPerson, RestaurantStaff
- AppDbContext with EF Core
- Email service (MailKit)
- QR Code service (QRCoder)
- Layout with Bootstrap 5 + hotel gold theme
- Login page, Home page

## Coming Next
- Step 2: Admin Dashboard (create events, manage guest counts)
- Step 3: Contact Person Dashboard (approve guests, invite by email or link)
- Step 4: Guest Registration + QR Code Display
- Step 5: Restaurant Staff Scanner Page

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB is fine for development)
- Visual Studio 2022 or VS Code

### Setup

1. **Clone / open the project in Visual Studio**

2. **Configure your database connection** in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HotelBuffetPassDb;Trusted_Connection=True;"
   }
   ```

3. **Configure email settings** in `appsettings.json`:
   ```json
   "Email": {
     "SmtpHost": "smtp.gmail.com",
     "SmtpPort": "587",
     "SenderName": "Hotel Events Team",
     "SenderEmail": "events@yourhotel.com",
     "Username": "your-gmail@gmail.com",
     "Password": "your-app-password"
   }
   ```
   > For Gmail, generate an [App Password](https://myaccount.google.com/apppasswords) (requires 2FA enabled)

4. **Run the app** — migrations and seeding run automatically on startup:
   ```bash
   dotnet run
   ```

5. **Default Admin Login:**
   - Email: `admin@hotel.com`
   - Password: `Admin@123`

---

## Project Structure
```
HotelBuffetPass/
├── Controllers/
│   ├── AccountController.cs      ← Login/Logout
│   ├── HomeController.cs
│   ├── AdminController.cs        (Step 2)
│   ├── ContactPersonController.cs (Step 3)
│   ├── GuestController.cs        (Step 4)
│   └── ScannerController.cs      (Step 5)
├── Data/
│   ├── AppDbContext.cs
│   ├── AppRoles.cs
│   └── DbSeeder.cs
├── Models/
│   ├── ApplicationUser.cs
│   ├── Event.cs
│   ├── GuestRegistration.cs
│   └── ScanLog.cs
├── Services/
│   ├── IEmailService.cs
│   ├── EmailService.cs
│   └── QRCodeService.cs
├── ViewModels/
│   └── ViewModels.cs
├── Views/
│   ├── Shared/_Layout.cshtml
│   ├── Account/Login.cshtml
│   └── Home/Index.cshtml
├── Program.cs
├── appsettings.json
└── HotelBuffetPass.csproj
```
