# UTN Assistant

A modern, full-stack application with a Vite-powered React frontend and a clean ASP.NET Core Web API backend.

## Architecture & Tech Stack

- **Frontend:** React + TypeScript, bundled with Vite.
- **Backend:** ASP.NET Core Minimal API (.NET 8+).
- **Proxy:** Microsoft's `SpaProxy` handles seamless cross-origin requests during development.
- **Linting:** ESLint 9 (Flat Config) + Prettier enforcing strict formatting rules.

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or newer.
- [Node.js](https://nodejs.org/) (v18+ recommended).

### First-Time Setup

1. Open a terminal in the `src/client` directory.
2. Install the frontend dependencies:
   ```bash
   npm install
   ```

### Running the Application (Local Development)

This project is configured to launch both the backend and frontend concurrently via .NET Kestrel.

1. Navigate to the `src/server/UtnAssistant.API` directory (or open the `.slnx` file in Visual Studio / Rider).
2. Run the HTTPS profile:

```bash
dotnet run --launch-profile https

```

3. The .NET API will start on `https://localhost:7083` and automatically boot the Vite dev server on `http://localhost:3000`. The browser will open automatically.
