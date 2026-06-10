# 🏗️ Arsitektur Sistem - Lapak

## Overview

Lapak dibangun dengan arsitektur **Clean Architecture** menggunakan **Blazor Server** .NET. Aplikasi menggunakan rendering sisi server dengan komunikasi real-time via **SignalR**.

## Layer Arsitektur

```
┌─────────────────────────────────────────────┐
│           Presentation Layer                 │
│  ┌──────────────────────────────────────┐   │
│  │    Blazor Server Components          │   │
│  │    (Razor Pages, Layout, Shared)     │   │
│  └──────────────────────────────────────┘   │
├─────────────────────────────────────────────┤
│           Application Layer                 │
│  ┌──────────────────────────────────────┐   │
│  │    Services (Business Logic)         │   │
│  │    - RecommendationService           │   │
│  │    - CustomerScoringService          │   │
│  │    - LlmService                      │   │
│  │    - StorageService                  │   │
│  └──────────────────────────────────────┘   │
├─────────────────────────────────────────────┤
│           Domain Layer                      │
│  ┌──────────────────────────────────────┐   │
│  │    Entity Models                     │   │
│  │    Configuration Models              │   │
│  └──────────────────────────────────────┘   │
├─────────────────────────────────────────────┤
│           Infrastructure Layer              │
│  ┌──────────────────────────────────────┐   │
│  │    EF Core DbContext                 │   │
│  │    SignalR Hubs                      │   │
│  │    External APIs (LLM, Payment, etc) │   │
│  └──────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

## Komponen Utama

### 1. Blazor Server Components
- **MainLayout**: Layout utama dengan sidebar, navbar, dan theme support
- **Pages**: Halaman-halaman aplikasi (Home, Products, Chat, Dashboard, dll)
- **Shared Components**: Komponen reusable

### 2. Services
- **LlmService**: Abstraksi multi-provider LLM dengan fallback otomatis
- **RecommendationService**: Hybrid recommendation (collaborative + content-based)
- **CustomerScoringService**: Scoring dan segmentasi pelanggan
- **StorageService**: Abstraksi multi-backend storage

### 3. Entity Framework Core
- **LapakDbContext**: Context utama dengan konfigurasi lengkap
- Mendukung: SQLite, SQL Server, MySQL, PostgreSQL
- Identity integration untuk autentikasi

### 4. SignalR Hubs
- **ChatHub**: Komunikasi real-time untuk AI chatbots
- **NotificationHub**: Notifikasi real-time ke pengguna
- **DashboardHub**: Update data dashboard real-time

## Data Flow

### AI Chat Flow
```
User Input → Blazor Component → SignalR Hub → LlmService
    → Primary LLM Provider (OpenAI/Gemini/Anthropic/Ollama)
    → [Jika gagal] → Fallback Provider
    → Response → SignalR Hub → Blazor Component → User
```

### Recommendation Flow
```
User Request → RecommendationService
    → Collaborative Filtering (user purchase history)
    → Content-Based (category matching)
    → Merge with weights → Ranked Products
```

### Transaction Flow
```
Checkout → Order Creation → Payment Gateway → Payment Confirmation
    → Order Status Update → Shipping Request → Tracking Updates
    → Delivery Confirmation → Customer Scoring Update
```
