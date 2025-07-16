# CHAP2 Web Portal

A beautiful, modern web portal for the CHAP2 musical chorus management system.

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- CHAP2.Chorus.Api running on `http://localhost:5050`

### Launch in Cursor IDE

#### Method 1: Using Debug Configuration
1. Open the project in Cursor IDE
2. Press `F5` or go to **Run and Debug** (Ctrl+Shift+D)
3. Select **"Launch CHAP2.Web"** from the dropdown
4. Click the green play button or press `F5`

#### Method 2: Using Terminal
```bash
# Navigate to the project directory
cd CHAP2.UI/CHAP2.WebPortal/CHAP2.Web

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

#### Method 3: Using Watch Mode (Hot Reload)
```bash
dotnet watch run
```

### Debugging Features

#### Breakpoints
- Set breakpoints in any `.cs` file
- Set breakpoints in Razor views (`.cshtml`)
- JavaScript debugging in browser DevTools

#### Debug Configurations
- **Launch CHAP2.Web**: Standard launch with auto-browser opening
- **Launch CHAP2.Web (HTTPS)**: Launch with HTTPS only
- **Attach to CHAP2.Web**: Attach to running process

#### Hot Reload
- Changes to `.cs` files trigger automatic rebuild
- Changes to `.cshtml` files trigger page refresh
- Changes to CSS/JS files trigger browser refresh

## 🎨 Features

### Search Interface
- **Real-time search** with debounced input
- **Multiple search modes**: title, text, musical key
- **Smart sorting**: exact key matches first, then alphabetical
- **Search highlighting** in results
- **Export functionality** to CSV

### Detail Views
- **Modal popup** for quick viewing
- **New window** for full-screen detail view
- **Beautiful animations** and transitions
- **Print functionality**
- **Copy lyrics** to clipboard

### Visual Design
- **Frosted glass effects** with backdrop blur
- **Animated backgrounds** with floating musical notes
- **Responsive design** for all devices
- **Modern typography** using Inter font
- **Smooth animations** and hover effects

## 🔧 Configuration

### API Connection
The web portal connects to the CHAP2.Chorus.Api. Update the API URL in `appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5050"
}
```

### Development Settings
- **HTTP**: `http://localhost:5001`
- **HTTPS**: `https://localhost:7000`
- **Environment**: Development

## 🛠️ Development

### Project Structure
```
CHAP2.Web/
├── Controllers/          # MVC Controllers
├── Interfaces/           # Service interfaces
├── Services/            # Service implementations
├── Views/               # Razor views
├── wwwroot/            # Static files
│   ├── css/           # Stylesheets
│   ├── js/            # JavaScript files
│   └── images/        # Images
└── .vscode/           # Cursor IDE configuration
```

### Key Files
- `Program.cs` - Application startup and configuration
- `Controllers/HomeController.cs` - Main controller
- `Services/ChorusApiService.cs` - API communication
- `Views/Home/Index.cshtml` - Main search page
- `Views/Home/Detail.cshtml` - Detail view page
- `wwwroot/css/site.css` - Base styles
- `wwwroot/css/search.css` - Search page styles
- `wwwroot/css/detail.css` - Detail page styles

### Debugging Tips

#### C# Debugging
- Set breakpoints in controllers and services
- Use `Console.WriteLine()` or `_logger.LogInformation()`
- Check the Debug Console for output

#### JavaScript Debugging
- Open browser DevTools (F12)
- Set breakpoints in JavaScript files
- Use `console.log()` for debugging

#### CSS Debugging
- Use browser DevTools Elements panel
- Inspect CSS variables and computed styles
- Test responsive design with device simulation

### Common Issues

#### API Connection Issues
1. Ensure CHAP2.Chorus.Api is running on port 5050
2. Check `appsettings.json` for correct API URL
3. Verify CORS settings in the API

#### Build Errors
1. Run `dotnet restore` to restore packages
2. Run `dotnet clean` then `dotnet build`
3. Check for missing project references

#### Runtime Errors
1. Check the Debug Console for error messages
2. Verify API connectivity in browser DevTools
3. Check browser console for JavaScript errors

## 🎯 Keyboard Shortcuts

### Development
- `F5` - Start debugging
- `Ctrl+F5` - Start without debugging
- `Ctrl+Shift+F5` - Restart debugging
- `Shift+F5` - Stop debugging

### Application
- `Ctrl+K` - Focus search input
- `Enter` - Trigger search
- `Escape` - Close modals
- `Ctrl+P` - Print (in detail view)
- `Ctrl+C` - Copy lyrics (in detail view)

## 🔍 Troubleshooting

### Port Conflicts
If ports 5001 or 7000 are in use:
1. Update `Properties/launchSettings.json`
2. Update `appsettings.json` API URL
3. Restart the application

### SSL Certificate Issues
For HTTPS development:
```bash
dotnet dev-certs https --trust
```

### Performance Issues
- Use `dotnet watch run` for faster development
- Check browser DevTools Performance tab
- Monitor API response times

## 📱 Browser Support
- Chrome/Edge (recommended)
- Firefox
- Safari
- Mobile browsers

## 🎨 Customization

### Styling
- Modify CSS variables in `wwwroot/css/site.css`
- Update color scheme in `:root` section
- Add custom animations in CSS files

### Functionality
- Extend `IChorusApiService` for new API calls
- Add new controllers for additional features
- Modify JavaScript for custom interactions

## 📄 License
This project is part of the CHAP2 system. 