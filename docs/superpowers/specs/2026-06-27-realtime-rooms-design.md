# Design — SketchRoom: Camere de colaborare în timp real

> Sistem de „camere" (rooms) în care un user deschide o cameră și alți useri intră
> și desenează împreună în timp real — în LAN sau online, ca un server de jocuri.
> Data: 2026-06-27. Proiect: `SketchRoom` (C# WPF, .NET 8, Prism 9 + DryIoc).

---

## 1. Context & problemă

SketchRoom e azi un whiteboard funcțional single-player. Colaborarea multiplayer e construită
parțial și apoi **deconectată**:
- Există un client SignalR (`SketchRoom.Services/WhiteboardHubClient.cs`) cu URL hardcodat
  `http://localhost:5000/whiteboardhub`, dar **serverul (hub-ul) nu există în repo**.
- Cablajul din `WhiteBoardModule/Views/WhiteBoardView.xaml.cs` (`WhiteBoardView_Loaded`) e comentat.
- Navigarea la Lobby/Participation din `UsersInteractionsModule/ViewModels/UsersInteractionsViewModel.cs`
  e comentată — sesiunile sunt inaccesibile din UI.
- `WhiteBoard.Core/Colaboration/ColaborationService.cs` are guard `if (!_isHost) return` → doar host-ul desenează.
- Se sincronizează doar liniile freehand (`DrawLineDto`/`LiveDrawPointDto`); formele/BPMN/text/imagini — deloc.
- `CursorPositionDto.HostImageBase64` trimite avatarul întreg la fiecare mișcare de cursor (~3 MB/s/participant).

Scopul acestui proiect: **un sistem de camere realtime funcțional cap-coadă**.

## 2. Decizii (validate în brainstorming)

| Temă | Decizie |
|---|---|
| Transport | **SignalR + MessagePack** (nu gRPC — camerele = Groups native, broadcast, reconnect, presence) |
| Găzduire online | **Server central** (ASP.NET Core), mereu pornit |
| Găzduire LAN | **Server inclus în app** (Kestrel embedat în desktopul host); același cod de hub |
| Identitate | **Cod cameră + nume/avatar**, fără conturi |
| Roluri | **Toți desenează** + **host moderator** (kick, clear, lock) |
| Stare | **Relay + snapshot autoritativ în memorie**, cameră **efemeră**; late-join primește snapshot; host poate salva local |
| Ce se sincronizează | **TOATE** elementele (freehand, forme, conectori, text, imagini), cursoare, prezență |
| Scop spec | DOAR sistemul de camere realtime (fără conturi/chat/persistență/AI cleanup) |

## 3. Arhitectură — proiecte

- **`SketchRoom.Realtime.Contracts`** (nou; net8.0) — DTO-uri + numele metodelor hub (client↔server),
  partajate de client și server. Mută aici și event-urile partajate care azi stau în `WhiteBoardModule/Events/`
  (repară izolarea ruptă: `FooterModule`/`UsersInteractionsModule` nu mai referențiază `WhiteBoardModule`).
- **`SketchRoom.Realtime.Server`** (nou; ASP.NET Core, SignalR) — `WhiteboardHub` + `RoomManager`
  (camere în memorie). MessagePack protocol. Rulează online (container/VPS) și se poate **embeda** în desktop pt LAN.
- **`SketchRoom.Realtime.Client`** (refactor din `WhiteboardHubClient`) — URL configurabil, connect/reconnect,
  API tipizat, ridică event-uri consumate de `WhiteBoardModule`. Înlocuiește logica host-only din `ColaborationService`.
- **`WhiteBoard.Core` / `WhiteBoardModule`** — cablează colaborarea: operațiile locale pe canvas → hub;
  operațiile primite → canvas. `ElementDto` unificat (toate tipurile). Overlay cursoare live.
- **`LobbyHostingModule` / `ParticipationModule`** — re-activate: creează cameră (Online/LAN), intră cu cod,
  listă participanți, controale host. Re-activează navigarea din `UsersInteractionsViewModel`.
- **`SketchRoom.Realtime.Tests`** (nou; xUnit) — primul proiect de teste din soluție.

## 4. Modelul camerei

- Cameră identificată prin **cod** de 6 caractere (alfanumeric, fără caractere ambigue).
- Serverul ține per cameră (în memorie): `HostConnectionId`, lista participanților, **lista autoritativă de elemente**,
  setarea `IsLocked`.
- **Online:** clientul se conectează la URL-ul serverului central → se alătură grupului SignalR = codul camerei.
- **LAN:** appul host pornește un Kestrel embedat cu același `WhiteboardHub` pe un port (ex. 5000);
  partajează `IP_LAN:port + cod`. Descoperire: **manuală în v1** (host afișează IP+cod; joiner le introduce).
  (Descoperire automată mDNS/UDP — follow-up.)

## 5. Sincronizare (relay + snapshot autoritativ)

Operații la nivel de element, **last-write-wins pe `ElementId`** (fiecare element e un obiect independent → nu e nevoie de CRDT/OT):

- `ElementAdded(roomCode, ElementDto)` / `ElementUpdated(roomCode, ElementDto)` / `ElementRemoved(roomCode, elementId)` / `BoardCleared(roomCode)`
- `LivePoint(roomCode, elementId, point)` — pentru linia freehand în curs (fluiditate); finalizată apoi prin `ElementAdded`
- `CursorMoved(roomCode, userId, x, y)` — **fără** bytes de avatar (avatarul vine o dată la `UserJoined`, referit prin `userId`)
- La intrare: serverul trimite `Snapshot(elements[])` din starea autoritativă
- Serverul aplică fiecare operație și pe starea sa autoritativă (ca să poată servi snapshot + clear consistent)

### `ElementDto` (unificat, serializabil — toate tipurile)
Câmpuri: `Id` (Guid), `Type` (enum: Freehand, Rectangle, Ellipse, Line, Connector, Text, Image, … extensibil),
`Geometry` (puncte sau bounds, după tip), `Style` (culoare stroke/fill, grosime), `Text` (pt text),
`ImageRef` (pt imagini), `ZOrder`, `OwnerUserId`. Mapare bidirecțională cu modelul WPF din `WhiteBoard.Core`.

## 6. Prezență, moderare, fiabilitate

- Prezență: `UserJoined(user)`, `UserLeft(userId)`, `RoomState(participants[], isLocked)`
- Moderare host (validată **pe server**, doar dacă apelantul == host): `KickUser(userId)`, `ClearBoard()`, `SetLock(bool)`
  - Cât e `IsLocked`, serverul respinge operațiile de desen de la non-host.
- Reconectare automată (SignalR `WithAutomaticReconnect`) → la reconnect: re-join cameră + re-request `Snapshot` pentru reconciliere.
- **Host pleacă:** serverul promovează automat cel mai vechi participant rămas ca host; dacă nu mai e nimeni, camera dispare (efemeră).
- Aplicare locală optimistă; serverul e sursa de adevăr la conflict (snapshot la reconnect reconciliază).

## 7. Limite & siguranță (fără conturi)

- Codul camerei = cheia de acces (random, ne-ghicibil ușor).
- Validare server-side a acțiunilor de host.
- Max participanți/cameră: **12** (configurabil).
- Anti-flood pe mesaje (rate-limit per conexiune) + plafon dimensiune mesaj MessagePack.
- Fără secrete hardcodate noi. (Cheia AES hardcodată existentă din `SecureStorage` — în afara scopului, dar de reparat separat.)

## 8. Testare

Proiect nou `SketchRoom.Realtime.Tests` (xUnit) — soluția nu are azi niciun test:
- `RoomManager`: join/leave, promovare host, snapshot, moderare (kick/clear/lock + respingere non-host), limită participanți.
- Hub: teste de integrare cu `TestServer` in-memory (join → broadcast → snapshot pe late-join).
- Client: `IWhiteboardClient` testabil prin interfață (fără conexiune reală).

## 9. Fluxuri principale

1. **Creează cameră (online):** host → „Creează" → client cere serverului central un cod → intră în grup → UI cameră (cod afișat).
2. **Creează cameră (LAN):** host → „Creează (LAN)" → app pornește Kestrel embedat + hub → afișează `IP:port + cod`.
3. **Intră în cameră:** user → „Intră" → nume/avatar + cod (+ IP pt LAN) → `JoinRoom` → primește `Snapshot` + `RoomState`.
4. **Desen colaborativ:** operație locală → optimistic pe canvas local → trimite la hub → broadcast la restul → aplică la ei.
5. **Moderare:** host → kick/clear/lock → server validează + execută + notifică toți.
6. **Plecare/deconectare:** `UserLeft` → ceilalți actualizează prezența; host pleacă → promovare automată.

## 10. Criterii de succes (Definition of Done)

- 2+ instanțe ale appului pot intra în aceeași cameră (online prin server central; LAN prin host) și **toți desenează simultan**, vizibil la toți.
- Toate tipurile de element (nu doar freehand) se propagă corect.
- Cel care intră târziu vede tabla completă (snapshot).
- Cursoarele celorlalți sunt vizibile, **fără** trafic de avatar per mișcare.
- Host: kick / clear / lock funcționează și sunt impuse pe server.
- Reconectare după pică rețeaua, cu reconciliere prin snapshot.
- Build verde pe toată soluția; testele `Realtime.Tests` trec.

## 11. În afara scopului (follow-up separat)

Conturi/login, camere persistente pe server, chat text/voce, listă camere publice, descoperire LAN automată (mDNS),
ștergerea proiectelor AI moarte, bug-urile de desen ne-legate de sync (FreeDraw double-add, Snap, undo freehand),
DPAPI pentru `SecureStorage`, fix manifest MSIX. (Toate documentate în analiza din `scratchpad/sketchroom-analysis/`.)
