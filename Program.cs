using System;
using System.Threading;         //used for thread.sleep
using System.Diagnostics;       //performance counter usage
using System.IO;                //get hard drive statistics

namespace Talking_Hard_Drive_Stats
{
    class Program
    {
        static void Main()
        {
            #region Variable Declaration
            //strings to hold cpu, ram, and uptime messages 
            //that get displayed and spoken
            //*flag holds free and total space suffix (GB/TB), available drive letters
            string CPUMessage, RamAvailMessage, UptimeMessage;
            String FreeSpaceSuffix, TotalSpaceSuffix, AvailableDrives = "Available Drives are:  ";
            //store info for all drives
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            //holds ram, free and total space, and percent full
            double FreeSpaceConverted, TotalSpaceConverted, RAMConverted, PercentFull;
            #endregion

            #region Performance counter Initialization
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");   //gets cpu usage %
            PerformanceCounter FreeRamCounter = new PerformanceCounter("Memory", "Available MBytes");
            PerformanceCounter UptimeCounter = new PerformanceCounter("System",
                "System Up Time");  //system uptime
            
            cpuCounter.NextValue();  //first performance counters values will be 0
            FreeRamCounter.NextValue();
            UptimeCounter.NextValue();
            //wait 0.5 seconds to give the computer time to get usable values
            Thread.Sleep(500);
            #endregion

            #region display computer info
            //print and speak that the program has started
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "Talking Hard Drive Stats";  //change window title

            Console.WriteLine("Current system and hard disk information:");
            
            //store cpu percentage as integer,  string is used to display and speak same info
            CPUMessage = " CPU Utilization:  " + (int)cpuCounter.NextValue() + "%";

            RAMConverted = FreeRamCounter.NextValue() / 1024;  //get mb ram free, convert to GB
            RamAvailMessage = "Available Memory:  " + RAMConverted.ToString("n1");   //tostring formats output to 1 decimal place


            //get the uptime in hours, minutes, etc instead of just seconds
            TimeSpan UpTimeSpan = TimeSpan.FromSeconds(UptimeCounter.NextValue());
            UptimeMessage = string.Format("System UpTime is:  {0} Days, {1} Hours, {2} Minutes, {3} Seconds",
                (int)UpTimeSpan.TotalDays, UpTimeSpan.Hours, UpTimeSpan.Minutes, UpTimeSpan.Seconds);

            Console.WriteLine("");  //blank line
            Console.WriteLine(CPUMessage);  //display cpu and ram data
            Console.WriteLine(RamAvailMessage + " GB");  //for display, use GB, for speech, use gigabyte
            Console.WriteLine(UptimeMessage);  //purnt uptime id D,H,M,S format
            Console.WriteLine("   Computer Name:  {0}", Environment.MachineName);
            Console.WriteLine("    Current User:  {0}", Environment.UserName);
            Console.WriteLine();
            #endregion

            #region hard drive stats
            Console.WriteLine("     DRIVE LETTER         FREE SPACE     TOTAL SPACE     % FULL");
            foreach (DriveInfo d in allDrives)   //for each available drive
            {
                if (d.IsReady == true)   //if drive is available 
                {
                    AvailableDrives += d.Name.Substring(0, 1) + ", ";
                    PercentFull = 100 - (d.AvailableFreeSpace / (float)d.TotalSize) * 100;  //% full
                    FreeSpaceConverted = d.TotalFreeSpace / (1024.0 * 1024 * 1024); //convert free space from bytes to gb
                    FreeSpaceSuffix = "GB";  //set suffix for free disk space display
                    if (FreeSpaceConverted >= 1024)  //if > 1024 gb, convert to tb
                    {
                        FreeSpaceSuffix = "TB";  //change free space suffix 
                        FreeSpaceConverted /= 1024;  //calculate free space as terabytes
                    }

                    TotalSpaceConverted = d.TotalSize / (1024.0 * 1024 * 1024);  //gets total size and makes it in gb
                    TotalSpaceSuffix = "GB";
                    if (TotalSpaceConverted >= 1024)
                    {
                        TotalSpaceSuffix = "TB";  //change free space suffix 
                        TotalSpaceConverted /= 1024;
                    }
                    //write drive letter, volume label, free space, total space, % full
                    //----|----|----|----|----|----|----|----|----|----|----|----|----|
                    Console.WriteLine("{0,-20} {1,15} {2,15} {3,10}",
                        d.Name.Substring(0, 1) + " " + d.VolumeLabel.PadRight(18),
                        FreeSpaceConverted.ToString("n2") + " " + FreeSpaceSuffix,
                        TotalSpaceConverted.ToString("n2") + " " + TotalSpaceSuffix,
                        PercentFull.ToString("n1") + "%");
                }
            }
            #endregion
            Console.WriteLine("\nPress a Key to exit.");
            Console.ReadKey();
        }
    }
}