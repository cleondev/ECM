# App Gateway

The edge gateway hosts the reverse proxy, BFF APIs, authentication pipeline, and the SPA frontend. The structure mirrors the high level architecture described in `ARCHITECT.md`:

```
app-gateway/
├── AppGateway.Api/            # ASP.NET Core host (BFF + proxy + auth)
│   ├── Controllers/
│   ├── Middlewares/
│   ├── ReverseProxy/
│   ├── Auth/
│   ├── appsettings.json
│   └── Program.cs
├── AppGateway.Contracts/
├── AppGateway.Infrastructure/
└── ui/                        # SPA UI (React/Next/Vite)
    ├── package.json
    ├── public/
    ├── src/
    │   ├── app/
    │   ├── features/
    │   ├── services/
    │   └── shared/
    └── dist/                  # built UI (served under /app)
```

Populate the folders with implementation code as the gateway evolves.
