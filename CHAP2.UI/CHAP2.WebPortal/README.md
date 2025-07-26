# CHAP2 Web Portal - System Restart Endpoint

## Overview

The web portal includes a system restart endpoint that can automatically restart all Docker containers and the portal itself. This is useful for deployment and maintenance purposes.

## Endpoint Details

**URL:** `POST /api/restart-system`

**Request Body:**
```json
{
    "confirmation": "RESTART_ALL_SERVICES"
}
```

**Response:**
```json
{
    "message": "System restart initiated. The portal will restart in 5 seconds.",
    "timestamp": "2024-01-01T12:00:00Z",
    "services": ["Qdrant", "Ollama", "LangChain Service", "Web Portal"]
}
```

## Security Features

- **Development Mode Only:** The endpoint only works in development mode (`ASPNETCORE_ENVIRONMENT=Development`)
- **Confirmation Required:** Must provide the exact confirmation code `RESTART_ALL_SERVICES`
- **Logging:** All restart attempts are logged with user agent information

## Usage Examples

### cURL Command
```bash
curl -X POST http://localhost:5000/api/restart-system \
  -H "Content-Type: application/json" \
  -d '{"confirmation": "RESTART_ALL_SERVICES"}'
```

### JavaScript (Frontend)
```javascript
// The restart button is automatically added to the page in development mode
// Or you can call the endpoint directly:
const response = await fetch('/api/restart-system', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
    },
    body: JSON.stringify({
        confirmation: 'RESTART_ALL_SERVICES'
    })
});
```

### PowerShell
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/restart-system" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"confirmation": "RESTART_ALL_SERVICES"}'
```

## Restart Process

The restart process includes the following steps:

1. **Stop Docker Containers:** `docker-compose down`
2. **Detect GPU:** Check for NVIDIA GPU availability
3. **Start Containers:** Start with GPU support if available, otherwise CPU-only
4. **Wait for Services:** 30-second wait for services to be ready
5. **Pull Ollama Models:** Ensure `nomic-embed-text` and `mistral` models are available
6. **Final Wait:** 10-second stabilization period
7. **Portal Restart:** Exit application to trigger portal restart

## Frontend Integration

The system restart functionality is automatically available in the frontend:

- **Restart Button:** Appears in the bottom-right corner in development mode
- **Confirmation Dialog:** Requires typing the exact confirmation code
- **Progress Indicators:** Shows restart progress with modal dialogs
- **Auto-Reload:** Automatically reloads the page after restart

## Error Handling

The endpoint includes comprehensive error handling:

- **Invalid Confirmation:** Returns 400 Bad Request
- **Non-Development Environment:** Returns 403 Forbidden
- **Command Execution Errors:** Logged but don't prevent restart
- **Network Issues:** Graceful timeout handling

## Logging

All restart activities are logged:

```
info: CHAP2.WebPortal.Controllers.HomeController[0]
      System restart requested by: Mozilla/5.0...
info: CHAP2.WebPortal.Controllers.HomeController[0]
      Starting system restart process...
info: CHAP2.WebPortal.Controllers.HomeController[0]
      Step 1: Stopping Docker containers...
info: CHAP2.WebPortal.Controllers.HomeController[0]
      Step 2: Starting Docker containers...
info: CHAP2.WebPortal.Controllers.HomeController[0]
      GPU detected, starting with GPU support...
info: CHAP2.WebPortal.Controllers.HomeController[0]
      Step 3: Waiting for services to be ready...
info: CHAP2.WebPortal.Controllers.HomeController[0]
      Step 4: Ensuring Ollama models are available...
info: CHAP2.WebPortal.Controllers.HomeController[0]
      System restart completed successfully
info: CHAP2.WebPortal.Controllers.HomeController[0]
      Step 6: Restarting web portal...
```

## Safety Considerations

- Only available in development mode
- Requires explicit confirmation code
- All activities are logged
- Graceful error handling
- Timeout protection for hanging commands

## Troubleshooting

If the restart fails:

1. **Check Logs:** Look for error messages in the application logs
2. **Verify Docker:** Ensure Docker Desktop is running
3. **Check Permissions:** Ensure the application can execute Docker commands
4. **Manual Restart:** Use the Windows deployment scripts as fallback

## Related Files

- **Controller:** `HomeController.cs` - Contains the restart endpoint
- **JavaScript:** `system-restart.js` - Frontend integration
- **Layout:** `_Layout.cshtml` - Includes the JavaScript file
- **Deployment Scripts:** `start-windows-gpu-detection.bat/.ps1` - Manual restart scripts 