# SketchRoom Realtime Server

Central SignalR hub that brokers Online (internet) rooms between SketchRoom clients.

## Run locally

```bash
dotnet run --project SketchRoom.Realtime.Server
```

By default the server listens on `http://localhost:5000` (Kestrel's default when no
`applicationUrl` is set in launchSettings or environment).  
To pick a specific port:

```bash
dotnet run --project SketchRoom.Realtime.Server --urls "http://0.0.0.0:5000"
```

## Endpoints

| Route | Purpose |
|---|---|
| `/whiteboardhub` | SignalR hub (MessagePack protocol) — all client traffic goes here |
| `/health` | Returns `ok` — use for load-balancer / container health probes |

## Point the desktop app at this server

In the SketchRoom app, open the **Online** tab when creating or joining a room.  
The **Central server URL** field is pre-filled from app settings (`%APPDATA%\SketchRoom\settings.dat`).  
Change it to the address where this server is reachable (e.g. `http://192.168.1.5:5000` for a
local network server, or `https://sketchroom.example.com` for a public deployment).  
The value is persisted automatically; all subsequent sessions reuse it.

## Deploy (any ASP.NET Core host)

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish SketchRoom.Realtime.Server/SketchRoom.Realtime.Server.csproj \
    -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SketchRoom.Realtime.Server.dll"]
```

Build and run:

```bash
docker build -t sketchroom-server .
docker run -p 5000:8080 sketchroom-server
```

### Systemd / any Linux host

Publish a self-contained binary and register it as a service, or run behind a reverse proxy
(Nginx / Caddy) that terminates TLS and forwards to the Kestrel port.

```bash
dotnet publish SketchRoom.Realtime.Server/SketchRoom.Realtime.Server.csproj \
    -c Release -r linux-x64 --self-contained -o /opt/sketchroom-server
```

The server keeps all room state in memory; a single instance handles all connected clients.
There is no persistent storage requirement beyond the process lifetime.
