# Medical Store Management System ERP

A comprehensive Medical Store Management System built with ASP.NET Core MVC 8.0, featuring dual role-based authentication, complete e-commerce functionality, and ERP capabilities.

## 🚀 Features

### Customer Features
- **User Authentication**: Secure registration and login system
- **Product Browsing**: Browse medicines by category, search, and filter
- **Shopping Cart**: Add/remove items, update quantities
- **Wishlist**: Save favorite products for later
- **Checkout**: Complete order placement with multiple payment options
- **Order Tracking**: View order history and track shipments
- **5% Discount**: Automatic discount on orders ≥ ₹500

### Admin Features
- **Dashboard**: Real-time overview with charts and statistics
- **Inventory Management**: Full CRUD operations for medicines
- **Category Management**: Organize products into categories
- **Order Management**: Process, ship, and manage orders
- **GRN (Goods Receipt Note)**: Manage stock arrivals from suppliers
- **Supplier Management**: Track suppliers and ledger
- **User Management**: Manage customer accounts
- **Reports**: 
  - Sales Report
  - Inventory Report
  - GST Report
  - Expiry Report
  - Low Stock Report
  - Profit/Loss Analysis

### Technical Features
- GST-compliant invoice generation (PDF)
- Real-time stock management
- Expiry date tracking
- Low stock alerts
- Responsive design (Bootstrap 5.3)
- AdminLTE admin panel
- Chart.js visualizations
- DataTables integration

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core MVC 8.0
- **ORM**: Entity Framework Core 8.0
- **Database**: SQL Server
- **Authentication**: ASP.NET Core Identity + JWT
- **PDF Generation**: iText7
- **Logging**: Serilog
- **Frontend**: Bootstrap 5.3, AdminLTE 3.4, jQuery
- **Charts**: Chart.js

## 📋 Prerequisites

- .NET 8.0 SDK
- SQL Server 2019+ (or SQL Server Express)
- Visual Studio 2022 or VS Code

## 🚀 Getting Started

### 1. Clone the Repository
```bash
git clone <repository-url>
cd MedicalStoreERP
```

### 2. Update Connection String
Edit `appsettings.json` and update the connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=MedicalStoreDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 3. Apply Migrations & Seed Data
```bash
cd MedicalStoreERP
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run the Application
```bash
dotnet run
```

The application will start at:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000

## 👤 Default Users

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@store.com | Admin@123 |
| User | user@store.com | User@123 |

## 📁 Project Structure

```
MedicalStoreERP/
├── Areas/
│   └── Admin/
│       ├── Controllers/     # Admin controllers
│       └── Views/           # Admin views
├── Controllers/             # User-facing controllers
├── Data/
│   ├── ApplicationDbContext.cs
│   └── DbSeeder.cs          # Seed data (50+ medicines)
├── Models/
│   ├── ApplicationUser.cs
│   ├── Medicine.cs
│   ├── Order.cs
│   ├── Category.cs
│   └── ViewModels/          # View models
├── Services/                # Business logic services
├── Views/                   # User-facing views
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _AdminLayout.cshtml
└── wwwroot/                 # Static files
```

## 🔧 Configuration

### App Settings
```json
{
  "AppSettings": {
    "StoreName": "MediCare Store",
    "GSTNumber": "29ABCDE1234F1Z5",
    "GSTPercentage": 12,
    "DiscountThreshold": 500,
    "DiscountPercentage": 5
  }
}
```

### JWT Settings
```json
{
  "JwtSettings": {
    "Secret": "your-256-bit-secret-key",
    "Issuer": "MedicalStoreERP",
    "Audience": "MedicalStoreERPUsers",
    "ExpiryInMinutes": 60
  }
}
```

## 📊 Database Schema

### Main Entities
- **ApplicationUser**: Extended Identity user with address fields
- **Category**: Product categories
- **Medicine**: Products with stock, pricing, expiry tracking
- **Order/OrderItem**: Customer orders
- **CartItem/WishlistItem**: Shopping cart and wishlist
- **Supplier/SupplierLedger**: Supplier management
- **GRN/GRNItem**: Goods Receipt Notes
- **CustomerLedger**: Customer transaction history

## 🔒 Security Features

- Role-based authorization (Admin/User)
- CSRF protection
- Password hashing with Identity
- JWT token authentication for API
- Secure cookie authentication

## 📱 API Endpoints

The application also exposes REST APIs for mobile/frontend integration:

- `POST /Cart/Add` - Add item to cart
- `POST /Cart/Update` - Update cart quantity
- `POST /Cart/Remove` - Remove item from cart
- `GET /Cart/GetCart` - Get current cart
- `POST /Wishlist/Add` - Toggle wishlist item
- `GET /Wishlist/GetCount` - Get wishlist count

## 🐳 Docker Support

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MedicalStoreERP/MedicalStoreERP.csproj", "MedicalStoreERP/"]
RUN dotnet restore "MedicalStoreERP/MedicalStoreERP.csproj"
COPY . .
WORKDIR "/src/MedicalStoreERP"
RUN dotnet build "MedicalStoreERP.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MedicalStoreERP.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MedicalStoreERP.dll"]
```

## 📝 License

This project is licensed under the MIT License.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## 📧 Support

For support, email support@medicare.com or create an issue in the repository.

---

Built with ❤️ using ASP.NET Core MVC 8.0
