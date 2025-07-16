# HTTP Test Files

This folder contains HTTP files for testing the CHAP2API endpoints.

## Files

- `health.http` - Tests specifically for the health controller
- `choruses.http` - Tests specifically for the choruses controller (includes CRUD operations and search functionality)
- `slide.http` - Tests for slide controller (placeholder for future implementation)

## How to Use

### Variables
The HTTP files use variables for easy configuration:
- `@apiBase` - Default API base URL (http://localhost:5050)

**Note:** For GET and PUT requests, replace the placeholder GUID with an actual GUID from a POST response.

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
curl -X GET http://localhost:5050/api/health/ping \
  -H "Content-Type: application/json"

# Test choruses endpoint
curl -X GET http://localhost:5050/api/choruses \
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

### Search Results
```json
{
  "query": "grace",
  "searchMode": "Contains",
  "searchIn": "all",
  "count": 2,
  "results": [
    {
      "id": "12345678-1234-1234-1234-123456789abc",
      "name": "Amazing Grace",
      "key": 2,
      "timeSignature": 1,
      "chorusText": "Amazing grace, how sweet the sound",
      "type": 1
    }
  ]
}
```

## Search Endpoints

### Comprehensive Search
`GET /api/choruses/search?q={query}&searchIn={scope}&searchMode={mode}`

**Parameters:**
- `q` (required): Search query
- `searchIn`: "name", "text", or "all" (default: "all")
- `searchMode`: "Exact", "Contains", or "Regex" (default: "Contains")

**Examples:**
```bash
# Search for "grace" in all fields
GET /api/choruses/search?q=grace&searchIn=all&searchMode=Contains

# Search for "Amazing" in names only
GET /api/choruses/search?q=Amazing&searchIn=name&searchMode=Contains

# Search with regex pattern
GET /api/choruses/search?q=gr.*ce&searchIn=all&searchMode=Regex
```

### Exact Name Match
`GET /api/choruses/by-name/{name}`

Returns a single chorus with exact name match (case-insensitive).

## Notes

- Make sure the API is running before testing
- Default port is 5000
- All endpoints are prefixed with `/api`
- Controllers inherit from `ChapControllerAbstractBase` for consistent logging
- Choruses use GUID-based identification for unique file storage
- Enum values default to NotSet (0) when not specified
- Case-insensitive name validation prevents duplicate choruses
- Search is optimized for real-time performance with multiple search modes 