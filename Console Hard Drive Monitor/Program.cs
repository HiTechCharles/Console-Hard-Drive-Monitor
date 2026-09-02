using System;
using System.Diagnostics;       // PerformanceCounter
using System.Globalization;     // CultureInfo
using System.Management;        // ManagementObjectSearcher
using System.IO;                // DriveInfo
using System.Text;              // StringBuilder
using System.Threading;         // Thread.Sleep
using System.Linq;
using System.Speech.Synthesis;  // SpeechSynthesizer

namespace Console_Hard_Drive_Monitor
{
    class Program
    {
        // Constants for better maintainability
        private const int COUNTER_WARMUP_DELAY_MS = 500;
        private const double BYTES_TO_KB = 1024.0;
        private const double BYTES_TO_MB = BYTES_TO_KB * 1024.0;
        private const double BYTES_TO_GB = BYTES_TO_MB * 1024.0;
        private const double BYTES_TO_TB = BYTES_TO_GB * 1024.0;
        private const double MB_TO_GB = 1024.0;
        private const int Speech_Rate = 3; // Speech rate for the synthesizer
        private const int Speech_Volume = 100; // Volume for the synthesizer (0-100)    
        private static SpeechSynthesizer HDDTalk = new SpeechSynthesizer();
        private static bool SpeechEnabled = true; // Flag to enable or disable speech output
        private static string AppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CHDM");
        private static string OptionsFile = Path.Combine(AppDirectory, "options.txt");

        static void Main()
        {
            Console.Title = "Talking Hard Drive Stats";
            Console.ForegroundColor = ConsoleColor.White;

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            InitializeSpeech();
            LoadOptions(); // Load speech option from file or prompt user if not found56
            DisplaySystemInformation();
            SpeakAndDisplay();

            DisplayDriveInformation(allDrives);

            SpeakAndDisplay("\nPress any key to exit.");
            Console.ReadKey();
        }

        static void InitializeSpeech()
        {
            HDDTalk.Rate = Speech_Rate;
            HDDTalk.Volume = Speech_Volume;
        }       

        static void SpeakAndDisplay(string message = "")
        {
            Console.WriteLine(message);
            if (SpeechEnabled)
            {
                message = SuffixSearch(message);
                HDDTalk.SpeakAsync(message);
            }
        }

        static void DisplaySystemInformation()
        {
            SpeakAndDisplay("\n\nCurrent system and hard disk information:");
            SpeakAndDisplay();

            try
            {
                using PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                using PerformanceCounter freeRamCounter = new PerformanceCounter("Memory", "Available MBytes");
                using PerformanceCounter uptimeCounter = new PerformanceCounter("System", "System Up Time");
                // First NextValue() often returns 0; allow counter time to populate
                cpuCounter.NextValue();
                freeRamCounter.NextValue();
                uptimeCounter.NextValue();
                Thread.Sleep(COUNTER_WARMUP_DELAY_MS);

                DisplayCpuUsage(cpuCounter);
                DisplayMemoryInfo(freeRamCounter);
                DisplayUptime(uptimeCounter);
                DisplayComputerInfo();
            }
            catch (UnauthorizedAccessException ex)
            {
                SpeakAndDisplay("Warning: Insufficient permissions to read performance counters: " + ex.Message);
                SpeakAndDisplay();
            }
            catch (Exception ex)
            {
                SpeakAndDisplay("Warning: Failed to read some performance counters: " + ex.Message);
                SpeakAndDisplay();
            }
        }

        static void DisplayCpuUsage(PerformanceCounter cpuCounter)
        {
            int cpuPercent = (int)cpuCounter.NextValue();
            string cpuUsageMessage = $"       CPU Utilization:  {cpuPercent}%";  
            SpeakAndDisplay(cpuUsageMessage);
        }

        static void DisplayMemoryInfo(PerformanceCounter freeRamCounter)
        {
            double ramGbAvailable = freeRamCounter.NextValue() / MB_TO_GB;
            double totalMemoryGb = GetTotalPhysicalMemoryGB();
            string memoryInfoMessage = $" Free and Total Memory:  {ramGbAvailable:N2} GB of {totalMemoryGb:N2} GB";

            SpeakAndDisplay(memoryInfoMessage);
        }

        static double GetTotalPhysicalMemoryGB()
        {
            try
            {
                // Query WMI for total physical memory on Windows systems
                if (OperatingSystem.IsWindows())
                {
                    var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        if (mo["TotalPhysicalMemory"] != null &&
                            long.TryParse(mo["TotalPhysicalMemory"].ToString(), out long totalBytes))
                        {
                            return totalBytes / BYTES_TO_GB;
                        }
                    }
                }
            }
            catch
            {
                // ignore and fallback
            }

            return 0.0;
        }

        static void DisplayUptime(PerformanceCounter uptimeCounter)
        {
            TimeSpan upTimeSpan = TimeSpan.FromSeconds(uptimeCounter.NextValue());
            string uptimeMessage = $"      System UpTime is:  {((int)upTimeSpan.TotalDays)} Days, {upTimeSpan.Hours} Hours, {upTimeSpan.Minutes} Minutes, {upTimeSpan.Seconds} Seconds";
            SpeakAndDisplay(uptimeMessage);
        }

        static void DisplayComputerInfo()
        {
            string computerInfoMessage = $"         Computer Name:  {Environment.MachineName}";
            SpeakAndDisplay(computerInfoMessage);
            string userInfoMessage = $"   Logged in User Name:  {Environment.UserName}";
            SpeakAndDisplay(userInfoMessage);
        }

        static void DisplayDriveInformation(DriveInfo[] allDrives)
        {
            var availableDrivesBuilder = new StringBuilder("Available Drives are: ");
            bool hasAvailableDrives = false;

            foreach (var drive in allDrives)
            {
                if (TryDisplayDriveInfo(drive))
                {
                    if (hasAvailableDrives)
                        availableDrivesBuilder.Append(", ");

                    availableDrivesBuilder.Append(drive.Name.Substring(0, 1));
                    hasAvailableDrives = true;
                }
            }

            SpeakAndDisplay();
            if (hasAvailableDrives)
            {
                SpeakAndDisplay(availableDrivesBuilder.ToString());
            }
            else
            {
                SpeakAndDisplay("No available drives found.");
            }
        }

        static bool TryDisplayDriveInfo(DriveInfo drive)
        {
            try
            {
                if (!drive.IsReady)
                    return false;

                long totalBytes = drive.TotalSize;
                long freeBytes = drive.AvailableFreeSpace;

                double percentFull = totalBytes > 0
                    ? 100.0 * (totalBytes - freeBytes) / totalBytes
                    : 0.0;

                string freeStr = FormatBytes(freeBytes) + " Free, ";
                string totalStr = FormatBytes(totalBytes) + " Total, ";
                string volumeLabel = GetVolumeLabel(drive);
                string PercentFullStr = percentFull.ToString("N2", CultureInfo.CurrentCulture) + "% Full";
                string driveLetter = drive.Name.Substring(0, 1);

                SpeakAndDisplay($"Drive {driveLetter}: {volumeLabel}, {freeStr}{totalStr}{PercentFullStr}");
                return true;
            }
            catch (IOException)
            {
                // Skip drives that throw IO exceptions (e.g., media not present)
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                // Skip inaccessible drives
                return false;
            }
        }

        static void SaveOptions()
        {
            try
            {
                if (!Directory.Exists(AppDirectory))
                {
                    Directory.CreateDirectory(AppDirectory);
                }
                using (StreamWriter writer = new StreamWriter(OptionsFile))
                {
                    writer.WriteLine(SpeechEnabled ? "SpeechEnabled=true" : "SpeechEnabled=false");
                }
            }
            catch (Exception ex)
            {
                SpeakAndDisplay("Warning: Failed to save options: " + ex.Message);
            }
        }

        static void AskforSpeech()

        {
            string ChangeSetting = "\n\nTo be reprompted, remove the AppDirectory folder CHDM.";
            
            SpeakAndDisplay("\nDo you want to enable speech output? (Y/N): ");
            string input = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (input == "Y")
            {
                SpeechEnabled = true;
                SpeakAndDisplay("Speech output enabled.");
            }
            else if (input == "N")
            {
                SpeechEnabled = false;
                SpeakAndDisplay("Speech output disabled.");
            }
            else
            {
                SpeakAndDisplay("Invalid input. Please enter 'Y' or 'N'.");
                AskforSpeech(); // Recursively ask again for valid input
            }
            SpeakAndDisplay(ChangeSetting);
            SaveOptions();
        }

        static void LoadOptions()
        {
            try
            {
                if (File.Exists(OptionsFile))
                {
                    using (StreamReader reader = new StreamReader(OptionsFile))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("SpeechEnabled=", StringComparison.OrdinalIgnoreCase))
                            {
                                SpeechEnabled = line.Substring("SpeechEnabled=".Length).Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                            }
                        }
                    }
                }
                else
                {
                    AskforSpeech(); // Prompt for speech option if no options file exists
                }
            }
            catch (Exception ex)
            {
                SpeakAndDisplay("Warning: Failed to load options: " + ex.Message);
            }
        }

        static string GetVolumeLabel(DriveInfo drive)
        {
            try
            {
                return drive.VolumeLabel ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        static string FormatBytes(long bytes)
        {
            var culture = CultureInfo.CurrentCulture;

            if (bytes >= BYTES_TO_TB)
                return (bytes / BYTES_TO_TB).ToString("N2", culture) + " TB";
            if (bytes >= BYTES_TO_GB)
                return (bytes / BYTES_TO_GB).ToString("N2", culture) + " GB";
            if (bytes >= BYTES_TO_MB)
                return (bytes / BYTES_TO_MB).ToString("N2", culture) + " MB";
            if (bytes >= BYTES_TO_KB)
                return (bytes / BYTES_TO_KB).ToString("N2", culture) + " KB";

            return bytes + " B";
        }

        static string SuffixSearch(string message)
        {
            string[] suffixes = { "MB", "GB", "TB" };
            string[] replacements = { "Megabytes", "Gigabytes", "Terabytes" };

            // Search the message string and replace the suffixes with the full words
            foreach (var pair in suffixes.Zip(replacements, (s, r) => new { S = s, R = r }))
            {
                if (message.Contains(pair.S))
                {
                    message = message.Replace(pair.S, pair.R);
                }
            }
            return message;
        }
    }
}