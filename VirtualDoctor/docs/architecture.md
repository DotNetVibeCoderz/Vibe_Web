# 🏗️ Arsitektur VirtualDoctor

## Overview

VirtualDoctor menggunakan arsitektur **Blazor Server** dengan **.NET 10**, mengandalkan **SignalR** untuk komunikasi real-time. Aplikasi ini dirancang dengan **clean architecture** yang memisahkan UI, service, dan data layer.

## Diagram Arsitektur

```
┌─────────────────────────────────────────────────────────────┐
│                      Browser Client                         │
│  (Blazor Server - SignalR WebSocket connection)            │
├─────────────────────────────────────────────────────────────┤
│                    ASP.NET Core 10                          │
│  ┌──────────┬──────────┬──────────┬────────────────────┐  │
│  │  Razor   │  SignalR │  Auth    │   Static Files     │  │
│  │Components│   Hubs   │(Cookie)  │   (wwwroot)        │  │
│  └──────────┴──────────┴──────────┴────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                   Service Layer (DI)                        │
│  ┌─────────────┬──────────────┬────────────────────────┐   │
│  │ Core        │ AI Services  │ Infrastructure         │   │
│  │ Services    │              │ Services               │   │
│  ├─────────────┼──────────────┼────────────────────────┤   │
│  │ AuthService │ AiChatService│ FileStorageService     │   │
│  │ UserService │ LlmProvider  │ LocationService        │   │
│  │ DoctorSvc   │ Factory      │ SearchService          │   │
│  │ MedicineSvc │ KernelFunc   │ VectorStoreService     │   │
│  │ HospitalSvc │ Service      │ DocIndexingService     │   │
│  │ OrderSvc    │ RagQuerySvc  │ PdfIndexingWorker      │   │
│  │ AppointSvc  │              │                        │   │
│  │ HomecareSvc │              │                        │   │
│  │ InsuranceSvc│              │                        │   │
│  └─────────────┴──────────────┴────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│                     Data Layer                              │
│  ┌──────────────────┬─────────────────────────────────┐    │
│  │   EF Core DbContext│   Vector Store (InMemory)     │    │
│  │   (SQLite/SQLSrv/  │   - Document Embeddings       │    │
│  │    PostgreSQL/MySQL)│   - Cosine Similarity Search  │    │
│  └──────────────────┴─────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## Integrasi AI & RAG

```
User Input → AiChatService
    ├── KernelFunctionService (tool detection)
    │   ├── SearchInternet (Tavily/Perplexity)
    │   ├── CheckDate
    │   ├── MathCalc
    │   ├── ReadFileFromUrl
    │   ├── DescribeImage
    │   ├── ScrapWebPage
    │   ├── AskDoctor
    │   ├── OrderMedicine
    │   ├── ScheduleDoctor
    │   ├── FindHospital
    │   └── QueryHealthDocs → RagQueryService
    │       ├── VectorStoreService.Search()
    │       └── LlmProviderFactory (context + question)
    └── LlmProviderFactory
        ├── OpenAI (GPT-4o)
        ├── Gemini (gemini-2.0-flash)
        ├── Anthropic (Claude 3.5 Sonnet)
        ├── Ollama (llama3.1)
        └── OpenAI Compatible
```

## Database Schema

```
ApplicationUser 1──* Consultation
ApplicationUser 1──* Order
ApplicationUser 1──* Appointment
ApplicationUser 1──* ChatHistory

Doctor 1──* Consultation
Doctor 1──* Appointment
Doctor 1──* DoctorSchedule

Hospital 1──* Appointment
Hospital 1──* Order (as Pharmacy)

Order 1──* OrderItem
OrderItem *──1 Medicine

ChatHistory 1──* ChatMessage
Consultation 1──* ConsultationMessage
```

## Storage Architecture

- **Default**: File System (`wwwroot/uploads/`)
- **Alternatives**: MinIO, AWS S3, Azure Blob
- File upload API → Storage Service → URL returned
- Images & Documents → Storage URL → Included in AI Chat context

## Vector Database (RAG)

- **Default**: InMemory (ConcurrentDictionary)
- **Alternatives**: SQLite, Qdrant, Azure AI Search
- Embeddings: Simple hash-based (production: use real embedding model)
- Search: Cosine similarity
- PDF Worker: Background service, 30-min interval
