# InventoryERP v1.0 (Enterprise Edition)

![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20WPF-blue)
![Framework](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)
![Status](https://img.shields.io/badge/Status-Stable-brightgreen)

**InventoryERP** is a professional, enterprise-grade ERP solution designed for modern businesses. Built with high-performance **.NET 8** and **WPF**, it offers a robust architecture for managing Inventory, Sales, Purchasing, and Accounting operations with a seamless, responsive user interface.

## 🚀 Key Features

### 🛒 Sales & Purchase Management
- **Full Workflow:** Quote -> Order -> Dispatch Note -> Invoice.
- **Traceability:** Automatic status tracking across the entire document chain.
- **Dynamic Pricing:** Customer-specific price lists and discount tiers.

### 🏭 Advanced Inventory Control
- **Multi-Warehouse:** Real-time tracking across unlimited locations.
- **Stock Movements:** Comprehensive history of every item movement with "Ghost Product" protection for data integrity.
- **Cost Tracking:** FIFO-based cost calculation.
- **Stock Grid:** "Soft Selection" UI for high-contrast visibility of financial data.

### 💰 Integrated Accounting
- **Partner Ledger (Cari Hesap):** Automatic debit/credit entries generated from invoices.
- **Financial Integrity:** Double-entry bookkeeping logic ensuring data consistency.
- **Reporting:** Real-time balance and transaction history for all partners.

### 🎨 Modern UI/UX
- **MVVM Architecture:** Clean separation of concerns for maintainability.
- **Responsive Design:** Soft color palettes, ergonomic inputs, and intuitive navigation.
- **Active/Passive Lifecycle:** Clean data views with optional historical data recovery.

## 🛠️ Tech Stack

- **Core:** C# 12, .NET 8
- **UI:** WPF (Windows Presentation Foundation)
- **Architecture:** MVVM (Model-View-ViewModel) with MediatR
- **Data Access:** Entity Framework Core 8 (EF Core)
- **Database:** SQLite (Embedded, Zero-configuration)
- **Logging & Resilience:** Robust error handling and diagnostic layers.

## 🏁 Getting Started

### Prerequisites
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (or newer)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/InventoryERP.git
   ```

2. **Navigate to the project directory:**
   ```bash
   cd InventoryERP
   ```

3. **Restore Dependencies:**
   ```bash
   dotnet restore
   ```

4. **Build the Application:**
   ```bash
   dotnet build --configuration Release
   ```

5. **Run:**
   - Launch `Presentation/bin/Release/net8.0-windows/InventoryERP.exe`
   - *Note: The local SQLite database (`inventory.db`) will be automatically created on first run.*

## 🔒 Security & Privacy
This repository is configured for open source safety:
- **Zero Secrets:** No API keys or passwords are hardcoded.
- **Data Protection:** `inventory.db` and logs are strictly excluded via `.gitignore`.
- **Packaging:** Build artifacts are ignored to keep the repository lightweight.

## 📄 License
This project is licensed under the [MIT License](LICENSE) - see the LICENSE file for details.

---
*Built with ❤️ by the InventoryERP Team.*
