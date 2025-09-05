# Milk & Bakery Management System

![Milk & Bakery Logo](/wwwroot/img/Picture1.png)

## Overview

Milk & Bakery Management System is a comprehensive enterprise solution designed for dairy and bakery businesses to efficiently manage their operations. The system provides end-to-end functionality for customer management, order processing, inventory tracking, employee management, and reporting.

## Features

### 🏢 Master Data Management
- **Customer Management**: Maintain detailed customer information including contact details, routes, and account types
- **Dealer Management**: Manage dealer information with integrated order processing capabilities
- **Employee Management**: Track employee details, designations, departments, and grades
- **Product Management**: Comprehensive material master with categories, pricing, and inventory tracking
- **Route Management**: Organize customers and dealers by delivery routes

### 🛒 Order Processing
- **Purchase Orders**: Create and manage customer purchase orders
- **Order Verification**: Multi-step order verification and processing workflow
- **File Generation**: Automated file generation for order processing
- **Order Repeat**: Easily repeat previous orders for regular customers

### 📊 Reporting & Analytics
- **Route Reports**: Track delivery performance by route
- **Visit Reports**: Monitor sales representative visits
- **Outstanding Reports**: Track pending payments and balances
- **Bill Summary**: Comprehensive billing summaries
- **Dashboard Analytics**: Visual charts and metrics for business insights

### 🔐 User Management
- **Role-Based Access**: Different access levels for Admin, Manager, Sales, and Customer roles
- **Authentication**: Secure login system with session management
- **Password Management**: User-friendly password change functionality

## Technology Stack

### Backend
- **Language**: C# (.NET Core)
- **Framework**: ASP.NET Core MVC
- **ORM**: Entity Framework Core
- **Database**: SQL Server

### Frontend
- **HTML5/CSS3**: Modern markup and styling
- **Bootstrap**: Responsive design framework
- **jQuery**: Interactive UI components
- **DataTables**: Advanced table functionality
- **Chart.js**: Data visualization
- **FontAwesome**: Icon library

### Additional Libraries
- **Select2**: Enhanced dropdowns
- **SweetAlert**: Beautiful alert dialogs
- **JSZip**: File compression utilities

## Getting Started

### Prerequisites
- .NET Core SDK 6.0 or higher
- SQL Server (Express or full version)
- Visual Studio 2022 or Visual Studio Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://your-repo-url/milk-bakery.git
   cd milk-bakery
   ```

2. **Configure Database Connection**
   Update the connection string in `appsettings.json`:
   ```json

3. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

4. **Apply Database Migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

6. **Access the Application**
   Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## Project Structure

```
Milk&Bakery/
├── Controllers/          # MVC Controllers
├── Models/              # Data models and entities
├── Views/               # Razor views
├── Data/                # Database context
├── Migrations/          # EF Core migrations
├── Services/            # Business logic services
├── DTOs/                # Data Transfer Objects
├── wwwroot/             # Static files (CSS, JS, images)
└── Program.cs           # Application entry point
```

## Key Modules

### 1. Dashboard
![Dashboard](/wwwroot/img/download.png)
The dashboard provides an overview of key business metrics including pending orders, processed orders, and financial summaries.

### 2. Master Management
- Customer Master
- Dealer Master
- Employee Master
- Material Master
- Route Master
- Company Master

### 3. Operations
- Order Creation
- Order Verification
- File Generation
- Order Processing

### 4. Reports
- Route Summary
- Visit Tracking
- Bill Summaries
- Financial Reports

## User Roles

| Role | Access Level | Description |
|------|-------------|-------------|
| **Admin** | Full Access | Complete system access with all privileges |
| **Manager** | High Access | Manage operations, view reports, manage users |
| **Sales** | Medium Access | Create orders, view customer data, track visits |
| **Customer** | Limited Access | View own orders and account information |

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is proprietary and confidential. Unauthorized copying or distribution is prohibited.

## Support

For support, contact the development team or refer to the documentation.

---
*Milk & Bakery Management System - Streamlining Dairy & Bakery Operations*