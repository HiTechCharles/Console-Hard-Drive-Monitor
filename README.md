# Console Hard Drive Monitor

A Windows console application that displays real-time system and hard drive information, including CPU usage, memory statistics, system uptime, and detailed drive storage information.

## Features

- **CPU Monitoring**: Real-time processor utilization percentage
- **Memory Information**: Available and total physical memory in GB
- **System Uptime**: Displays how long the system has been running
- **Drive Statistics**: Comprehensive information for all available drives including:
  - Drive letter and volume label
  - Free space available
  - Total capacity
  - Percentage used
  - Support for multiple storage units (B, KB, MB, GB, TB)

## Requirements

- **Operating System**: Windows
- **.NET Framework**: 4.8.1 or higher
- **Permissions**: Administrator privileges recommended for full performance counter access

## Installation

### Option 1: Build from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/HiTechCharles/Console-Hard-Drive-Monitor.git
   cd Console-Hard-Drive-Monitor
   ```

2. Open the solution in Visual Studio 2017 or later

3. Build the solution:
   - Press `Ctrl+Shift+B` or
   - Go to `Build > Build Solution`

4. Run the executable from:
   ```
   bin\Debug\Talking hard drive monitor.exe
   ```
   or
   ```
   bin\Release\Talking hard drive monitor.exe
   ```

### Option 2: Download Release

Download the latest compiled release from the [Releases](https://github.com/HiTechCharles/Console-Hard-Drive-Monitor/releases) page.

## Usage

Simply run the executable:

```bash
"Talking hard drive monitor.exe"
```

The application will display:
1. Current CPU utilization
2. Available and total memory
3. System uptime
4. Computer name and logged-in user
5. Detailed information for all available drives

Press any key to exit the application.

## Sample Output

```
Current system and hard disk information:

 Processor Utilization: 15%
 Free and Total Memory: 8.45 GB of 16.00 GB
	  System UpTime is: 2 Days, 5 Hours, 23 Minutes, 45 Seconds
		 Computer Name: DESKTOP-PC
   Logged in User Name: User

	 DRIVE LETTER         FREE SPACE     TOTAL SPACE     % FULL
	 ────────────         ──────────     ───────────     ──────
C Windows                   125.43 GB       465.76 GB       73.1%
D Data                      850.20 GB     1,863.01 GB       54.4%

Available Drives are: C, D

Press any key to exit.
```

## Technical Details

### Performance Counters Used

- **Processor**: `% Processor Time` (_Total)
- **Memory**: `Available MBytes`
- **System**: `System Up Time`

### Error Handling

The application includes robust error handling for:
- Unauthorized access to performance counters
- Unavailable or unready drives
- Missing drive information
- IO exceptions for removable media

### Code Highlights

- Culture-aware number formatting
- Automatic unit conversion (bytes to KB/MB/GB/TB)
- Safe handling of inaccessible drives
- Performance counter warmup to ensure accurate readings

## Development

### Project Structure

```
Console-Hard-Drive-Monitor/
├── Program.cs              # Main application logic
├── App.config              # Application configuration
├── Properties/
│   └── AssemblyInfo.cs    # Assembly metadata
└── Talking hard drive Stats.csproj  # Project file
```

### Building

Open the solution in Visual Studio and build for your target platform:
- **Debug**: Includes debug symbols for development
- **Release**: Optimized build for production use

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is open source. Please check the repository for license details.

## Troubleshooting

### "Insufficient permissions to read performance counters"

Run the application as Administrator to access all performance counters.

### Drive not showing up

- Ensure the drive is properly connected and recognized by Windows
- Check if the drive is ready and mounted
- Verify you have read permissions for the drive

### Inaccurate CPU or memory readings

The application waits 500ms after initializing performance counters to ensure accurate readings. If readings seem off, the system may be under heavy load during counter initialization.

## Author

**HiTechCharles**

## Acknowledgments

- Built with .NET Framework 4.8.1
- Uses Windows Performance Counters API
- Utilizes System.IO.DriveInfo for drive information
