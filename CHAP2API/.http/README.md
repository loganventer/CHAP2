# HTTP Test Files

This folder contains HTTP files for testing the CHAP2API endpoints.

## Files

- `health.http` - Tests specifically for the health controller
- `choruses.http` - Tests specifically for the choruses controller (includes CRUD operations)
- `slide.http` - Tests for slide controller (placeholder for future implementation)

## How to Use

### Variables
The HTTP files use variables for easy configuration:
- `@apiBase` - Default API base URL (http://localhost:5000)

### With VS Code
1. Install the "REST Client" extension
2. Open any `.http` file
3. Click "Send Request" above each request
4. Variables are automatically substituted in requests

### With IntelliJ IDEA
1. Open any `.http` file
2. Click the green play button next to each request

### With curl
You can also run these requests manually using curl:

```bash
# Test health ping
curl -X GET http://localhost:5000/api/health/ping \
  -H "Content-Type: application/json"

# Test choruses endpoint
curl -X GET http://localhost:5000/api/choruses \
  -H "Content-Type: application/json"
```

## Expected Responses

### Health Ping
```json
{
  "status": "OK",
  "message": "Service is running",
  "timestamp": "2024-01-01T12:00:00.000Z"
}
```

### Choruses
```json
[
  {
    "id": "12345678-1234-1234-1234-123456789abc",
    "name": "Amazing Grace",
    "key": 2,
    "timeSignature": 1,
    "chorusText": "Amazing grace, how sweet the sound",
    "type": 1
  },
  {
    "id": "87654321-4321-4321-4321-cba987654321",
    "name": "New Chorus",
    "key": 0,
    "timeSignature": 0,
    "chorusText": "This is a new chorus text",
    "type": 0
  }
]
```

## Notes

- Make sure the API is running before testing
- Default port is 5000
- All endpoints are prefixed with `/api`
- Controllers inherit from `ChapControllerAbstractBase` for consistent logging
- Choruses use GUID-based identification for unique file storage
- Enum values default to NotSet (0) when not specified
- Case-insensitive name validation prevents duplicate choruses 