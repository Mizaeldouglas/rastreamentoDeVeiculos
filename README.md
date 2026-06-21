# Sistema de Rastreamento Veicular

Rastreamento veicular: cadastro de veículos, ingestão de GPS real via protocolo GT06 (TCP), motor de eventos (geofence + velocidade) e mapa em tempo real. Veículos sem dispositivo físico continuam recebendo posições simuladas, para fins de demonstração.

## Roadmap (baseado na arquitetura discutida)

```
Rastreador GPS → TCP Listener → Processamento → Motor de eventos → WebSocket/API REST → Painel web / App mobile / Relatórios
                                       ↓
                                  PostgreSQL (PostGIS + TimescaleDB)
```

### Ingestão de dados
- [x] Rastreador GPS real via protocolo binário TCP (**GT06**, equivalente aos pacotes Teltonika/Suntech do diagrama original)
- [x] TCP Listener em C#/.NET 8 com parsing do protocolo (login + localização, CRC16/X25)
- [x] Processamento: validação (checksum) e normalização antes de salvar
- [x] Persistência no PostgreSQL

### Banco de dados
- [x] PostgreSQL
- [x] Extensão **PostGIS** (geofences como polígonos geoespaciais)
- [x] Extensão **TimescaleDB** (`Positions` é uma hypertable particionada por tempo)

### Motor de eventos
- [x] Alerta de geofence (entrada/saída de área)
- [x] Alerta de excesso de velocidade
- [x] Alerta de ignição (pacote de status GT06 `0x13`, dispara `IgnitionOn`/`IgnitionOff` só na transição de estado)
- [x] Push notification real (**Web Push/VAPID** — notificação do sistema operacional mesmo com a aba em segundo plano ou fechada, sem precisar de app mobile)

### WebSocket + API REST
- [x] Tempo real (SignalR — `PositionUpdated` e `AlertTriggered`)
- [x] API REST (veículos, geofences, alertas)
- [x] Histórico de rotas / playback (`GET /api/vehicles/{id}/history?from=&to=` + player no painel com play/pause, slider e velocidade)

### Consumidores
- [x] Painel web (React + mapa Leaflet, CRUD de veículos, geofences e alertas)
- [ ] App mobile (React Native)
- [ ] Relatórios (PDF/Excel)

### Outros (fora do diagrama original, mas relevantes)
- [x] Autenticação e multi-tenant (login por empresa via JWT, dados isolados por `CompanyId`)
- [ ] Testes automatizados

## Arquitetura

- **backend/Rastreador.Api** — ASP.NET Core 8 Web API + EF Core (PostgreSQL + PostGIS + TimescaleDB) + SignalR
  - `GpsTcpListenerService`: `TcpListener` na porta `5023` que aceita conexões de rastreadores reais (protocolo **GT06**, comum em rastreadores baratos no Brasil), faz login/ACK, persiste posições e processa pacotes de status (ignição/ACC)
  - `GpsSimulatorService`: gera posições simuladas a cada ~3s, apenas para veículos **sem IMEI** cadastrado (não conflita com dispositivos reais)
  - `PositionIngestService`: ponto único de persistência + broadcast + motor de eventos, usado tanto pelo simulador quanto pela ingestão real
  - Motor de eventos: dispara `Alert` quando o veículo excede o `SpeedLimitKmh` configurado, entra/sai de uma `Geofence` (polígono PostGIS, checado com `NetTopologySuite`), ou liga/desliga a ignição (`Vehicle.IgnitionOn`, atualizado só na transição de estado para não duplicar alertas)
  - `PositionHub` (`/hubs/positions`): transmite posições (`PositionUpdated`) e alertas (`AlertTriggered`) em tempo real via SignalR
  - `VehiclesController` (`/api/vehicles`): CRUD de veículos (`Imei`, `SpeedLimitKmh` opcionais)
  - `GeofencesController` (`/api/geofences`): CRUD de geofences (polígono via lista de pontos `{lat,lng}`)
  - `AlertsController` (`/api/alerts`): lista alertas (`?vehicleId=`) e permite reconhecer (`POST /{id}/ack`)
  - `GET /api/vehicles/{id}/history?from=&to=`: histórico de posições por período (default últimas 24h, limite de 5000 pontos)
  - `Positions` é uma **hypertable do TimescaleDB**, particionada por `Timestamp`, para suportar volume alto de séries temporais
  - **Multi-tenant**: ASP.NET Core Identity + JWT. `AuthController` (`/api/auth/register`, `/api/auth/login`) cria/autentica usuários vinculados a uma `Company`; todos os outros controllers e o `PositionHub` (via grupos SignalR) filtram automaticamente pelo `CompanyId` da claim do token — cada empresa só vê seus próprios veículos, geofences e alertas
  - **Push notifications**: `PushController` (`/api/push/vapid-public-key`, `/api/push/subscribe`) + `PushNotificationService` (biblioteca `WebPush`, protocolo VAPID). Toda vez que um alerta é disparado, o `PositionIngestService` também envia uma notificação push para os navegadores inscritos da empresa — falha de entrega (ex. subscription expirada) é capturada e nunca derruba o fluxo principal do alerta
- **backend/Rastreador.DeviceSimulator** — console app que simula um rastreador físico de verdade, falando o protocolo GT06 via TCP (login + posições periódicas + pacotes de status que alternam a ignição a cada ~18s). Útil para testar o `GpsTcpListenerService` sem hardware.
- **frontend/rastreador-web** — React + Vite + TypeScript
  - Cadastro/listagem de veículos (placa, modelo, motorista, IMEI e limite de velocidade opcionais), com badge de estado de ignição (Ligado/Desligado/—)
  - Mapa (Leaflet/OpenStreetMap) com marcadores em tempo real e geofences desenhadas como polígonos
  - Painel de alertas em tempo real (SignalR) com reconhecimento
  - Gerenciador de geofences (criação via bounding box, listagem, remoção)
  - Histórico de rotas: busca por veículo/período, com playback animado no mapa (rota desenhada + marcador percorrendo o trajeto)
  - Tela de login/cadastro de empresa; sessão (JWT) guardada no navegador, com logout
  - Botão "Ativar notificações" — registra um service worker (`public/sw.js`) e assina o navegador para receber Web Push

## Pré-requisitos

- .NET 8 SDK
- Node.js 18+
- Docker (para PostgreSQL + PostGIS + TimescaleDB)

## Como rodar

### 1. Banco de dados

```bash
docker compose up -d
```

Sobe a imagem `timescale/timescaledb-ha:pg16` (PostGIS + TimescaleDB já inclusos) na porta **5433** do host (a 5432 pode já estar ocupada por uma instalação nativa do PostgreSQL no Windows).

A connection string padrão está em `backend/Rastreador.Api/appsettings.json`:
```
Host=localhost;Port=5433;Database=rastreador;Username=postgres;Password=postgres
```

### 2. Backend

```bash
cd backend/Rastreador.Api
dotnet tool install --global dotnet-ef   # se ainda não tiver
dotnet ef database update                # aplica as migrations
dotnet run
```

A API sobe em `http://localhost:5000`. Swagger disponível em `/swagger`.

### 3. Frontend

```bash
cd frontend/rastreador-web
npm install
npm run dev
```

Abre em `http://localhost:5173`.

## Uso

### Primeiro acesso

1. Abra o frontend e use a tela "Cadastre sua empresa" (nome da empresa + e-mail + senha) — isso cria a empresa e seu primeiro usuário.
2. Faça login normalmente nas próximas vezes. O token JWT expira em 8h; expirado, a tela de login volta a aparecer automaticamente.
3. **Atenção**: o `Jwt:Secret` e o par de chaves `Vapid` em `appsettings.json` são valores de desenvolvimento — troque por segredos fortes (variável de ambiente) antes de qualquer deploy real.

### Notificações push

1. Clique em "🔔 Ativar notificações" no topo do painel e aceite a permissão do navegador.
2. A partir daí, qualquer alerta da sua empresa (geofence, velocidade, ignição) chega como notificação do sistema operacional — mesmo com a aba em segundo plano ou fechada.

### Veículo simulado (sem hardware)

1. Cadastre um veículo pelo formulário, **sem preencher o IMEI**.
2. O `GpsSimulatorService` gera posições simuladas automaticamente (a cada ~3s).
3. O mapa atualiza os marcadores em tempo real conforme os eventos chegam via SignalR.

### Veículo com rastreador real (ou simulador de dispositivo)

1. Cadastre um veículo informando o **IMEI** do dispositivo (ex.: `123456789012345`).
2. Se não tiver hardware físico, rode o simulador de dispositivo em outro terminal:
   ```bash
   cd backend/Rastreador.DeviceSimulator
   dotnet run -- 123456789012345 localhost 5023
   ```
   Ele se conecta via TCP, faz login (recebe ACK do servidor) e envia posições reais no protocolo GT06.
3. Posições chegam pelo `GpsTcpListenerService`, são persistidas e aparecem no mapa em tempo real, junto com os veículos simulados.

### Geofences e alertas

1. Cadastre uma geofence pelo formulário "Geofences" no painel (ou via `POST /api/geofences`), informando nome, veículo (opcional) e a caixa delimitadora (norte/sul/leste/oeste).
2. Defina opcionalmente um limite de velocidade no veículo. Sem isso, usa o limite padrão em `Alerts:DefaultSpeedLimitKmh` (appsettings.json, default 100).
3. Conforme posições chegam (simuladas ou reais), o motor de eventos compara com a posição anterior e dispara um alerta quando o veículo entra/sai de uma geofence ou excede o limite de velocidade.
4. Os alertas aparecem em tempo real no painel "Alertas recentes", com opção de reconhecer.
