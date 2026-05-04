# FormDezigner Development Plan

## 1. Project Initialization
- [x] Create Blazor Server project
- [x] Add necessary NuGet packages (MudBlazor, BlazorMonaco, EF Core SQLite)
- [x] Setup services in `Program.cs` (MudBlazor, Monaco Editor, DbContext)

## 2. Database Integration
- [x] Create EF Core Models (`FormEntity`, `FormVersion`, `FormTemplate`)
- [x] Create AppDbContext
- [x] Add SQLite connection string
- [x] Setup database initialization logic

## 3. UI/UX Design & Setup
- [x] Setup MainLayout with Sidebar and Topbar
- [x] Implement MudBlazor Theme
- [x] Add Form Designer Page structure (Sidebar components, Canvas, Settings pane)

## 4. Drag & Drop Designer (Designer Mode)
- [x] Implement Drag and Drop functionality (simulated with click-to-add and select logic)
- [x] Create Form Components models (Text, Dropdown, Checkbox, etc.)
- [x] Implement rendering options (Bootstrap / MudBlazor mock rendering)
- [x] Implement Component property editor (label, name, required, etc.)

## 5. Code Mode & Monaco Editor
- [x] Integrate BlazorMonaco
- [x] Add Code Mode toggle
- [x] Support JS/C#/Python snippets
- [x] Snippets library implementation

## 6. Export/Import & Publishing
- [x] Implement Export to JSON
- [x] Publish logic with versioning

## 7. Templates
- [x] Provide initial templates structure on Dashboard

## 8. Finalization
- [x] Test compiling
- [x] Send project to user
