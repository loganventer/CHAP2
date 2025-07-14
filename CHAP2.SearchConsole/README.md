# CHAP2.SearchConsole

A console application for the CHAP2API with two modes: interactive search and slide conversion.

## Features

### Search Mode (Default)
- **Real-time search**: Type and see results as you type
- **Interactive interface**: Key-by-key input with immediate feedback
- **Configurable delays**: Adjust search delay to minimize API calls
- **Comprehensive results**: Shows chorus names and preview text
- **Error handling**: Graceful handling of API errors and network issues

### Convert Mode
- Reads .ppsx files from the file system
- Sends binary data to the slide conversion endpoint
- Searches for the created chorus after upload
- Configurable API base URL
- Command-line argument support

## Usage

### Search Mode (Default)
```bash
# Start interactive search
dotnet run

# Or explicitly specify search mode
dotnet run search
```

### Convert Mode
```bash
# Convert a specific file
dotnet run convert path/to/your/file.ppsx

# Convert using configured default file
dotnet run convert
```

## Configuration

Edit `appsettings.json` to configure:

- `ApiBaseUrl`: The base URL of the CHAP2API (default: http://localhost:5000)
- `SearchDelayMs`: Delay before triggering search (default: 300ms)
- `MinSearchLength`: Minimum characters before searching (default: 2)
- `DefaultPpsxFilePath`: Default file path for convert mode

## Interactive Search Controls

- **Type**: Enter search terms character by character
- **Backspace**: Remove characters from search term
- **Escape**: Clear the current search term
- **Enter**: Select when exactly one result is found
- **Ctrl+C**: Exit the application

## Example Search Session

```
CHAP2 Search Console - Interactive Search Mode
=============================================
API Base URL: http://localhost:5000
Search Delay: 300ms
Minimum Search Length: 2

Testing API connectivity...
Health check status: OK
API is accessible. Starting interactive search...

Type to search choruses. Search triggers automatically.
Press Enter to select, Escape to clear, Ctrl+C to exit.

Search: heer
Search: heer (2 results)
  1. Hy is Heer G
  2. Heer is my Herder
Search: heer g
Search: heer g (1 results)

=== SINGLE CHORUS FOUND ===
  Id: 12345678-1234-1234-1234-123456789abc
  Name: Hy is Heer G
  Key: NotSet
  TimeSignature: NotSet
  Type: NotSet
  ChorusText:
  Hy is Heer, Hy is Heer
  Hy is Heer, Hy is Heer
  Hy is Heer, Hy is Heer
  ...
```

## Example Convert Session

```
Reading file: /path/to/sample.ppsx
File size: 12345 bytes
Filename: sample.ppsx
Testing API connectivity...
Health check status: OK
API is accessible, proceeding with file upload...
Calling API...
Response Status: 201
Success!

Chorus 'sample' was created. Would you like to search for it? (y/n)
y

Searching for the created chorus...
Found the created chorus:
  Id: 12345678-1234-1234-1234-123456789abc
  Name: sample
  Key: NotSet
  TimeSignature: NotSet
  Type: NotSet
  ChorusText:
  [Extracted text from PowerPoint slides]
```

## Configuration Examples

### Production (Conservative)
```json
{
  "ApiBaseUrl": "http://localhost:5000",
  "SearchDelayMs": 500,
  "MinSearchLength": 3,
  "DefaultPpsxFilePath": "./default.ppsx"
}
```

### Development (Responsive)
```json
{
  "ApiBaseUrl": "http://localhost:5000",
  "SearchDelayMs": 200,
  "MinSearchLength": 1,
  "DefaultPpsxFilePath": "./test.ppsx"
}
```

## Prerequisites

1. Make sure the CHAP2API is running on the configured URL
2. For convert mode: Ensure your .ppsx file exists and is accessible
3. The API must have the search endpoint available (`/api/choruses/search`)

## Performance

- **Debounced searches**: Reduces API calls with configurable delay
- **Cancellation support**: Previous searches are cancelled when new input arrives
- **Minimum search length**: Prevents unnecessary API calls for short terms
- **Efficient display**: Shows relevant information without overwhelming output 