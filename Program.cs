using System;
using System.Diagnostics;       // PerformanceCounter
using System.Globalization;
using System.IO;                // DriveInfo
using System.Text;              // StringBuilder
using System.Threading;         // Thread.Sleep

namespace Talking_Hard_Drive_Stats
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

        static void Main()
        {
            Console.Title = "Talking Hard Drive Stats";
            Console.ForegroundColor = ConsoleColor.White;

            DriveInfo[] allDrives = DriveInfo.GetDrives();

            DisplaySystemInformation();
            Console.WriteLine();

            DisplayDriveInformation(allDrives);

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        private static void DisplaySystemInformation()
        {
            Console.WriteLine("Current system and hard disk information:");
            Console.WriteLine();

            try
            {
                using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                using (var freeRamCounter = new PerformanceCounter("Memory", "Available MBytes"))
                using (var uptimeCounter = new PerformanceCounter("System", "System Up Time"))
                {
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
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Warning: Insufficient permissions to read performance counters: " + ex.Message);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: Failed to read some performance counters: " + ex.Message);
                Console.WriteLine();
            }
        }

        private static void DisplayCpuUsage(PerformanceCounter cpuCounter)
        {
            int cpuPercent = (int)cpuCounter.NextValue();
            Console.WriteLine(" Processor Utilization: {0}%", cpuPercent);
        }

        private static void DisplayMemoryInfo(PerformanceCounter freeRamCounter)
        {
            double ramGbAvailable = freeRamCounter.NextValue() / MB_TO_GB;
            double totalMemoryGb = GetTotalPhysicalMemoryGB();

            Console.WriteLine(" Free and Total Memory: {0} GB of {1} GB",
                ramGbAvailable.ToString("N2", CultureInfo.CurrentCulture),
                totalMemoryGb.ToString("N2", CultureInfo.CurrentCulture));
        }

        private static double GetTotalPhysicalMemoryGB()
        {
            try
            {
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                return computerInfo.TotalPhysicalMemory / BYTES_TO_GB;
            }
            catch
            {
                return 0.0;
            }
        }

        private static void DisplayUptime(PerformanceCounter uptimeCounter)
        {
            TimeSpan upTimeSpan = TimeSpan.FromSeconds(uptimeCounter.NextValue());
            Console.WriteLine("      System UpTime is: {0} Days, {1} Hours, {2} Minutes, {3} Seconds",
                (int)upTimeSpan.TotalDays, upTimeSpan.Hours, upTimeSpan.Minutes, upTimeSpan.Seconds);
        }

        private static void DisplayComputerInfo()
        {
            Console.WriteLine("         Computer Name: {0}", Environment.MachineName);
            Console.WriteLine("   Logged in User Name: {0}", Environment.UserName);
        }

        private static void DisplayDriveInformation(DriveInfo[] allDrives)
        {
            Console.WriteLine("     DRIVE LETTER         FREE SPACE     TOTAL SPACE     % FULL");
            Console.WriteLine("     ────────────         ──────────     ───────────     ──────");

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

            Console.WriteLine();
            if (hasAvailableDrives)
            {
                Console.WriteLine(availableDrivesBuilder.ToString());
            }
            else
            {
                Console.WriteLine("No available drives found.");
            }
        }

        private static bool TryDisplayDriveInfo(DriveInfo drive)
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

                string freeStr = FormatBytes(freeBytes);
                string totalStr = FormatBytes(totalBytes);
                string volumeLabel = GetVolumeLabel(drive);
                string driveLetter = drive.Name.Substring(0, 1);

                Console.WriteLine("{0,-20} {1,15} {2,15} {3,10}",
                    driveLetter + " " + volumeLabel.PadRight(18),
                    freeStr,
                    totalStr,
                    percentFull.ToString("N1", CultureInfo.CurrentCulture) + "%");

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

        private static string GetVolumeLabel(DriveInfo drive)
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

        private static string FormatBytes(long bytes)
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
    }
}