# CHAP2.Console.Bulk

A console application for bulk conversion of PowerPoint files to choruses, using CQRS and shared services from CHAP2.Console.Common.

---

## 🏗️ How This Fits Into the Solution

CHAP2.Console.Bulk is part of the CHAP2 Musical Chorus Management System, which follows iDesign Clean Architecture principles. It provides batch PowerPoint-to-chorus conversion, communicating with the API and reusing shared services from `CHAP2.Console.Common`.

See the [root README](../../../README.md) for full architecture and solution context.

---

## Features

- Recursively scans directories for .ppsx and .pptx files
- Sends binary data to the slide conversion endpoint of CHAP2.Chorus.Api
- Processes multiple files in batch operations
- Real-time progress tracking during conversion
- Configurable API base URL
- Command-line argument support
- Comprehensive error handling and reporting
- Uses shared API client and display services from CHAP2.Console.Common

---

## CQRS & Architecture
- All API communication uses the shared `ApiClientService`.
- Follows repository and service naming conventions (`AddAsync`, `GetByNameAsync`, etc.).
- Clean separation of concerns for maintainability and testability.

---

## Configuration

Edit `appsettings.json` to configure:
- `ApiBaseUrl`: The base URL of the CHAP2.Chorus.Api (default: http://localhost:5050)
- `DefaultPpsxFilePath`: Default file path if no argument is provided

---

## Usage

### Method 1: Command-line argument
```bash
dotnet run path/to/your/file.ppsx
```

### Method 2: Configured default file
```bash
dotnet run
```
(Will use the file specified in `DefaultPpsxFilePath` in appsettings.json)

### Method 3: Absolute path
```bash
dotnet run /absolute/path/to/file.ppsx
```

---

## Prerequisites
1. Make sure the CHAP2.Chorus.Api is running on the configured URL
2. Ensure your .ppsx file exists and is accessible
3. The file must have a .ppsx or .pptx extension

---

## Example Output

```
Reading file: /path/to/sample.ppsx
File size: 12345 bytes
Filename: sample.ppsx
Calling API...
Response Status: 201
Success! Response:
{
  "message": "Successfully converted PowerPoint file to chorus: sample",
  "chorus": { ... },
  "originalFilename": "sample.ppsx"
}
```

---

## Error Handling & Troubleshooting
- File not found or invalid extension
- API connection issues (check `ApiBaseUrl` and that the API is running)
- Server errors (check API logs for details)
- File too large (see API's max file size setting)

---

## 🧩 Architecture Benefits
- **CQRS**: All write operations use command endpoints.
- **Shared Services**: Uses `ApiClientService` and `ConsoleDisplayService` from CHAP2.Console.Common.
- **Maintainability**: Easy to extend for new file types or endpoints.

---

## 🚀 Extending
- Add new file types or endpoints by extending the bulk upload service or API client.
- Reuse or extend shared services in `CHAP2.Console.Common`.
- See the [root README](../../README.md) for architectural guidance.

---

## 🤝 Contributing
- Follow iDesign/Clean Architecture and CQRS principles
- Add/modify tests for new features
- Update documentation as needed
- Open an issue or pull request for discussion 