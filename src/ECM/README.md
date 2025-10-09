# ECM core

The modular monolith for ECM lives here. `ECM.Host` bootstraps the application and loads domain modules from the `Modules` folder. Each module is self-contained with `Domain`, `Application`, `Infrastructure`, and (optionally) `Api` projects.

Current layout:

```
ECM/
├── ECM.Host/                  # Host (IModule loader)
├── ECM.BuildingBlocks/        # Shared kernel, abstractions, outbox, events
└── Modules/
    ├── Document/{Domain,Application,Infrastructure,Api}
    ├── File/{Domain,Application,Infrastructure,Api}
    ├── Workflow/{Domain,Application,Infrastructure,Api}
    ├── Signature/{Domain,Application,Infrastructure,Api}
    └── SearchRead/{Application,Infrastructure,Api}
```

Add new module implementations under the appropriate folder and wire them up through `ECM.Host`.
