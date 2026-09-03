using System;
using System.Diagnostics;       // PerformanceCounter
using System.Globalization;     // CultureInfo
using System.Management;        // ManagementObjectSearcher
using System.IO;                // DriveInfo
using System.Text;              // StringBuilder
using System.Threading;         // Thread.Sleep
using System.Linq;
using ShadowFlame;              // TTS  

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

        // Single shared TTS instance (disposable)
        private static readonly TTS tts = new TTS();

        static void Main()
        {
            Console.Title = "Talking Hard Drive Stats";
            Console.ForegroundColor = ConsoleColor.White;


            DriveInfo[] allDrives = DriveInfo.GetDrives();
            DisplaySystemInformation();
            tts.SpeakAndDisplay();

            DisplayDriveInformation(allDrives);

            tts.SpeakAndDisplay("\nPress any key to exit.");
            Console.ReadKey();
        }



        static void DisplaySystemInformation()
        {
            tts.SpeakAndDisplay("\n\nCurrent system and hard disk information Generated on" + Dates.Long());
            tts.SpeakAndDisplay();

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
                tts.SpeakAndDisplay("Warning: Insufficient permissions to read performance counters: " + ex.Message);
                tts.SpeakAndDisplay();
            }
            catch (Exception ex)
            {
                tts.SpeakAndDisplay("Warning: Failed to read some performance counters: " + ex.Message);
                tts.SpeakAndDisplay();
            }
        }

        static void DisplayCpuUsage(PerformanceCounter cpuCounter)
        {
            int cpuPercent = (int)cpuCounter.NextValue();
            string cpuUsageMessage = $"       CPU Utilization:  {cpuPercent}%";  
            tts.SpeakAndDisplay(cpuUsageMessage);
        }

        static void DisplayMemoryInfo(PerformanceCounter freeRamCounter)
        {
            double ramGbAvailable = freeRamCounter.NextValue() / MB_TO_GB;
            double totalMemoryGb = GetTotalPhysicalMemoryGB();
            string memoryInfoMessage = $" Free and Total Memory:  {ramGbAvailable:N2} GB of {totalMemoryGb:N2} GB";

            tts.SpeakAndDisplay(memoryInfoMessage);
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
            tts.SpeakAndDisplay(uptimeMessage);
        }

        static void DisplayComputerInfo()
        {
            string computerInfoMessage = $"         Computer Name:  {Environment.MachineName}";
            tts.SpeakAndDisplay(computerInfoMessage);
            string userInfoMessage = $"   Logged in User Name:  {Environment.UserName}";
            tts.SpeakAndDisplay(userInfoMessage);
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

            tts.SpeakAndDisplay();
            if (hasAvailableDrives)
            {
                tts.SpeakAndDisplay(availableDrivesBuilder.ToString());
            }
            else
            {
                tts.SpeakAndDisplay("No available drives found.");
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

                tts.SpeakAndDisplay($"Drive {driveLetter}: {volumeLabel}, {freeStr}{totalStr}{PercentFullStr}");
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