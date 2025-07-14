# CHAP2Console

A console debugging application for testing the CHAP2API slide conversion endpoint.

## Features

- Reads .ppsx files from the file system
- Sends binary data to the slide conversion endpoint
- Searches for choruses containing "heer" after upload
- Displays all matching choruses with detailed information
- Configurable API base URL
- Command-line argument support
- JSON response formatting

## Configuration

Edit `appsettings.json` to configure:

- `ApiBaseUrl`: The base URL of the CHAP2API (default: http://localhost:5000)
- `DefaultPpsxFilePath`: Default file path if no argument is provided

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

## Prerequisites

1. Make sure the CHAP2API is running on the configured URL
2. Ensure your .ppsx file exists and is accessible
3. The file must have a .ppsx extension

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
  "chorus": {
    "id": "12345678-1234-1234-1234-123456789abc",
    "name": "sample",
    "key": 0,
    "timeSignature": 0,
    "chorusText": "Chorus text extracted from PowerPoint file: sample",
    "type": 0
  },
  "originalFilename": "sample.ppsx"
}

Searching for choruses containing 'heer'...
Found 2 choruses containing 'heer':

--- Chorus 1 ---
  Id: 12345678-1234-1234-1234-123456789abc
  Name: Hy is Heer G
  Key: NotSet
  TimeSignature: NotSet
  Type: NotSet
  ChorusText:
  [Extracted text from PowerPoint slides]

--- Chorus 2 ---
  Id: 87654321-4321-4321-4321-cba987654321
  Name: Another Heer Chorus
  Key: NotSet
  TimeSignature: NotSet
  Type: NotSet
  ChorusText:
  [Extracted text from PowerPoint slides]
```

## Error Handling

The application will display helpful error messages for:
- File not found
- Invalid file extension (non-.ppsx files)
- API connection issues
- Server errors 