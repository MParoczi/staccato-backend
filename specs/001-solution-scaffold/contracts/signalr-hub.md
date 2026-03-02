# Contract: SignalR Notification Hub

**Branch**: `001-solution-scaffold` | **Date**: 2026-03-01

---

## Hub Registration

- **Class**: `NotificationHub : Hub<INotificationClient>`
- **Route**: `/hubs/notifications`
- **Authentication**: Hub connections require a valid JWT Bearer token (applied via `[Authorize]` on the hub)

---

## Server → Client Methods (INotificationClient)

### PdfReady

Sent by the server when a PDF export has been generated and is ready to download.

**Signature**: `Task PdfReady(string exportId, string fileName)`

| Parameter | Type | Description |
|---|---|---|
| `exportId` | `string` (GUID) | The export record identifier |
| `fileName` | `string` | Display name of the generated PDF file |

**Trigger**: Called from `PdfExportBackgroundService` (future feature) via `IHubContext<NotificationHub, INotificationClient>` after successfully uploading the PDF to Azure Blob Storage.

**Target**: The specific user connection(s) belonging to the export's owner, identified by the user's ID stored in the JWT claim.

---

## Client → Server Methods

None defined at scaffold time. Future features may add hub methods for real-time collaboration.

---

## Error Contract

If the hub connection is rejected (expired token, missing auth), the SignalR client receives a `401 Unauthorized` response on the negotiate handshake. The client is responsible for re-negotiating with a fresh access token.
