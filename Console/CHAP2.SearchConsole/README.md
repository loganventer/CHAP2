# CHAP2.SearchConsole

A console application for the CHAP2API providing interactive, real-time search for choruses.

## Features

- **Real-time search**: Type and see results as you type
- **Interactive interface**: Key-by-key input with immediate feedback
- **Configurable delays**: Adjust search delay to minimize API calls
- **Minimum length control**: API calls only made when search term meets minimum length
- **Comprehensive results**: Shows chorus names and preview text
- **Error handling**: Graceful handling of API errors and network issues
- **Cancellation support**: Previous searches cancelled when new input arrives
- **Memory cache**: Search results are cached in memory for 10 minutes, reducing redundant API calls and improving responsiveness
- **Observer-based UI**: The search UI uses an observer pattern to keep the prompt at the top and results below, updating only the results area for a smooth, flicker-free experience

## Usage

```bash
# Start interactive search
 dotnet run
```

## Configuration

Edit `appsettings.json` to configure:

- `ApiBaseUrl`: The base URL of the CHAP2API (default: http://localhost:5000)
- `SearchDelayMs`: Delay before triggering search (default: 300ms)
- `MinSearchLength`: Minimum characters before searching (default: 2)

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
API is accessible. Starting interactive search...

Type to search choruses. Search triggers after each keystroke with delay.
Press Enter to select, Escape to clear, Ctrl+C to exit.

Search: h
Search: h (Type at least 2 characters to search)
Search: he
Search: he (2 results)
  1. Hy is Heer G
  2. Heer is my Herder
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

## Prerequisites

1. Make sure the CHAP2API is running on the configured URL
2. The API must have the search endpoint available (`/api/choruses/search`)

## Performance

- **Debounced searches**: Reduces API calls with configurable delay
- **Cancellation support**: Previous searches are cancelled when new input arrives
- **Minimum search length**: Prevents unnecessary API calls for short terms
- **Efficient display**: Shows relevant information without overwhelming output

## Architecture Benefits

- **Separation of Concerns**: Console logic separated from HTTP communication
- **Dependency Injection**: Services properly configured and testable
- **Shared Services**: Business logic reused across applications
- **Error Handling**: Centralized error handling in service layer
- **Maintainability**: Easy to modify business logic in one place
- **Configuration-driven**: Environment-specific settings
- **Modular Design**: Easy to add new features
- **Memory Cache Layer**: Search results are cached for 10 minutes in a common-layer service, reducing API load and improving performance
- **Observer Pattern UI**: The UI uses an observer pattern to keep the search prompt at the top and results below, updating only the results area for a modern, responsive experience 