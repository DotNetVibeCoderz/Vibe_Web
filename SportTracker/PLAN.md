# SportTracker Development Plan

## 1. Project Setup - DONE
- [x] Add NuGet packages (EF Core SQLite, MudBlazor/Bootstrap for UI, CsvHelper for export, Swagger).
- [x] Configure `appsettings.json` (Database connection, Storage provider).

## 2. Domain Models & Database Context - DONE
- [x] Create `User` model (Id, Username, PasswordHash, Role, Profile details).
- [x] Create `Activity` model (Id, UserId, Type, Distance, Duration, Elevation, RouteCoordinates, Date).
- [x] Create Social models (`Comment`, `Like`, `Club`, `Follow`).
- [x] Create Gamification models (`Goal`, `Achievement`).
- [x] Create `AppDbContext` using Entity Framework Core.
- [x] Implement data seeding (Admin user, Sample users, Sample activities, Clubs).

## 3. Core Services - DONE
- [x] `AuthService`: Handle Login, Register, Password Reset.
- [x] `StorageService`: Abstraction for FileSystem (default), Azure Blob, AWS S3.
- [x] `DataService`: CRUD for activities, analytics calculations.
- [x] `SocialService`: Feed generation, likes, comments, clubs.

## 4. Minimal API & Swagger - DONE
- [x] Configure Swagger in `Program.cs`.
- [x] Map Minimal API endpoints for Activities and Users.

## 5. UI/UX Design (Blazor Server) - DONE
- [x] Setup Brutalism & Sporty CSS theme (Bold typography, high contrast colors).
- [x] Implement Light/Dark theme toggle.
- [x] Layout component with Sidebar/Navbar.

## 6. Authentication Pages - DONE
- [x] Login Page.
- [x] Register Page.
- [x] User Profile & Settings.

## 7. Master Data (CRUD) Pages - DONE
- [x] Activities CRUD page (Search, Sort, Filter, Paging, Export to CSV).
- [x] Users CRUD page (Admin only).
- [x] Clubs CRUD page.

## 8. Feature Pages - DONE
- [x] Activity Feed (Dashboard).
- [x] Activity Tracking/Detail View (Map placeholder).
- [x] Leaderboard & Challenges.
- [x] Personal Goals & Analytics.

## 9. Finalization - DONE
- [x] Compile and Test.
- [x] Create `README.md` (English & Indonesian).
- [x] Send project to user.
