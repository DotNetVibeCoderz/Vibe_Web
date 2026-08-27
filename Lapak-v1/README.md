# 🛍️ Lapak - Modern E-Commerce Platform

> AI-powered e-commerce platform built with .NET Blazor Server

## ✨ Features

### 🛒 Core E-Commerce
- **Product Management**: CRUD products, categories, sub-categories, attributes, stock, pricing, promos, comments, likes & ratings
- **Store Management**: Registration, profiles, verification, ratings, comments & likes
- **Buyer Features**: Registration, profiles, wishlist, shopping cart, checkout
- **Transactions & Payments**: Midtrans & Xendit payment gateway integration
- **Shipping & Logistics**: JNE, J&T, SiCepat, Pos Indonesia courier integration with real-time tracking
- **Promos & Vouchers**: Discounts, cashback, loyalty points

### 🤖 AI Features
- **Tony Kurus - Shopping Assistant**: AI chatbot for product/store search, recommendations, and shopping help
- **Siti Bohay - Customer Support**: AI chatbot with RAG for policy documents, handover to WhatsApp/Email
- **Multi-LLM Support**: OpenAI, Gemini, Anthropic, Ollama, and OpenAI-compatible providers
- **Automatic Fallback**: If one LLM fails, automatically routes to another provider
- **Chat History**: Persistent per user
- **File Upload**: Support for images and documents in chat

### 🧠 AI Recommendation Engine
- Collaborative filtering based on similar users' purchases
- Content-based recommendations using categories and attributes
- Real-time suggestions on product pages and checkout
- Personalized recommendations based on user profile

### 📊 Customer Scoring
- Score based on transaction count, value, and category diversity
- Customer segmentation: Bronze, Silver, Gold, Platinum
- Targeted promos based on customer tier

### 📈 Dashboard & Reporting
- Modern, responsive design with light/dark theme
- Interactive charts and statistics
- Advanced filters (date, category, store, transaction value)
- Tabular data with export support
- Real-time updates with SignalR

### 💾 Database & Storage
- **Databases**: SQLite, SQL Server, MySQL, PostgreSQL
- **Storage**: File System, MinIO, Amazon S3, Azure Blob
- Flexible configuration via `appsettings.json`

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- SQLite (default) or other supported database

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/lapak.git
cd lapak

# Run the application
dotnet run
```

The application will be available at `https://localhost:5001`

### Configuration

Edit `appsettings.json` to configure:

- **Database**: Change `DatabaseProvider` to `SQLite`, `SqlServer`, `MySql`, or `PostgreSql`
- **AI Providers**: Add your API keys under `AI.Providers`
- **Payment Gateways**: Configure Midtrans/Xendit keys
- **Shipping**: Set RajaOngkir API key
- **Storage**: Configure MinIO, S3, or Azure Blob

## 🏗️ Architecture

```
Lapak/
├── Components/        # Blazor UI Components
│   ├── Layout/        # Main layout, sidebar, navbar
│   ├── Pages/         # Application pages
│   │   ├── Account/   # Login, register, profile
│   │   ├── Chat/      # Tony Kurus & Siti Bohay chat
│   │   ├── Dashboard/ # Analytics dashboard
│   │   └── Products/  # Product listing & detail
│   └── Shared/        # Reusable components
├── Data/              # EF Core DbContext & seed data
├── Hubs/              # SignalR hubs
├── Models/            # Entity models & configurations
├── Services/          # Business logic services
│   ├── AI/            # LLM service abstraction
│   ├── Payment/       # Payment gateway services
│   ├── Shipping/      # Courier & shipping services
│   └── Storage/       # File storage abstraction
└── wwwroot/           # Static assets & CSS
```

## 📚 Documentation

- [Architecture](docs/architecture.md)
- [AI Configuration](docs/ai-config.md)
- [Dashboard & Reporting](docs/dashboard.md)
- [Storage Setup](docs/storage.md)
- [Database Setup](docs/database.md)

## 🛠️ Tech Stack

- **Framework**: .NET 8.0 Blazor Server
- **ORM**: Entity Framework Core
- **Real-time**: SignalR
- **AI/LLM**: Multi-provider abstraction (OpenAI, Gemini, Anthropic, Ollama)
- **Charts**: ChartJs.Blazor.Fork
- **Storage**: MinIO SDK, S3 SDK
- **Resilience**: Polly

## 📄 License

MIT License - see LICENSE file for details

---

Made with ❤️ by Jacky The Code Bender @ Gravicode Studios
