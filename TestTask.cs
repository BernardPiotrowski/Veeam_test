using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

//using System.Threading;

//using System.Threading;
using System.Threading.Tasks;
using System.Timers;
//using static System.Net.WebRequestMethods;

namespace Veeam_test
{
    public class TestTask
    {
        static void Main(string[] args)
        {

            //Folder paths, synchronization interval and log file path should be provided using the command line arguments;
            Console.WriteLine("Number of args " + args.Length);
            Console.WriteLine("Source folder path " + args[0]);
            Console.WriteLine("Replica folder path " + args[1]);
            Console.WriteLine("Synchronization interval " + args[2]);
            Console.WriteLine("Log folder path " + args[3]);

            Console.WriteLine("To stop this program - please press Ctrl-c");

            string sourceFolder = args[0];
            string replicaFolder = args[1];
            string interval = args[2];
            int inter = int.Parse(interval);
            string logFile = args[3];

            if (!File.Exists(logFile))
            {
                File.WriteAllText(logFile, "");
            }

            StreamWriter sw = File.AppendText(logFile);


            while (true)
            {
                Thread.Sleep(inter);

                SynchronizeDirectories(sourceFolder, replicaFolder, sw);
            }


            //sw.Close();

            //Console.WriteLine("Press enter to exit");
            //Console.ReadLine();
        }


        public static void SynchronizeDirectories(string sourceDir, string destinationDir, StreamWriter s)
        {
            // 1. List of folders in source and destination
            DirectoryInfo sourceDirectory = new DirectoryInfo(sourceDir);
            DirectoryInfo destinationDirectory = new DirectoryInfo(destinationDir);

            // 2. if destination folder does not exist - create it
            if (!destinationDirectory.Exists)
            {
                try
                {
                    Directory.CreateDirectory(destinationDir);
                    ProvideInfo(s, "Cr", destinationDirectory.FullName);
                }
                catch(Exception ex) 
                {
                    Console.WriteLine(ex.Message); ;
                }
            }

            // 3. Remove files 
            RemoveFiles(sourceDirectory, destinationDirectory,s);

            // 4. Remove subfolders
            RemoveFolders( sourceDir, destinationDir, s);

            // 3. Synchronize files (copying)
            SynchronizeFiles(sourceDirectory, destinationDirectory, s);

            // 4. Synchronize folders (creating, copying)
            foreach (DirectoryInfo subDir in sourceDirectory.GetDirectories())
            {
                string destinationSubDir = Path.Combine(destinationDir, subDir.Name);
                SynchronizeDirectories(subDir.FullName, destinationSubDir, s);
            }
        }


        private static void SynchronizeFiles(DirectoryInfo sourceDir, DirectoryInfo destinationDir, StreamWriter sw)
        {
            foreach (FileInfo file in sourceDir.GetFiles())
            {
                try
                {
                    string destinationFilePath = Path.Combine(destinationDir.FullName, file.Name);

                    // 3.1. Check if file exist in destination
                    if (!File.Exists(destinationFilePath))
                    {
                        // 3.2. if file not exists - copy it
                        file.CopyTo(destinationFilePath);
                        ProvideInfo(sw, "Cop", destinationFilePath);
                    }
                    else
                    {
                        // 3.3. Compare time and size
                        FileInfo destinationFile = new FileInfo(destinationFilePath);
                        if (file.LastWriteTimeUtc > destinationFile.LastWriteTimeUtc || file.Length != destinationFile.Length)
                        {
                            file.CopyTo(destinationFilePath, true); //true - override file
                            ProvideInfo(sw, "Cha", destinationFilePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }

        public static void RemoveFolders(string sourceDir, string destinationDir, StreamWriter s) 
        {
            // 1. List of folders in source and destination 
            DirectoryInfo sourceDirectory = new DirectoryInfo(sourceDir);
            DirectoryInfo destinationDirectory = new DirectoryInfo(destinationDir);

            // 2. Remove folder from destination if not exist in source
            foreach (DirectoryInfo subDir in destinationDirectory.GetDirectories())
            {
                try
                {
                    string destinationSubDir = Path.Combine(destinationDirectory.FullName, subDir.Name);
                    string sourceSubDir = Path.Combine(sourceDir, subDir.Name);
                    DirectoryInfo sourceSubFolder = new DirectoryInfo(sourceSubDir);
                    if (!sourceSubFolder.Exists)
                    {
                        Directory.Delete(destinationSubDir, true);
                        ProvideInfo(s, "DelFol", destinationSubDir);
                    }
                }
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static void RemoveFiles(DirectoryInfo sourceDir, DirectoryInfo destinationDir, StreamWriter sw)
        {
            foreach (FileInfo file in destinationDir.GetFiles())
            {
                try
                { 
                    string sourceFilePath = Path.Combine(sourceDir.FullName, file.Name);
                    string destinationFilePath = Path.Combine(destinationDir.FullName, file.Name);
                    //3.1.Check if file exist in source
                    if (!File.Exists(sourceFilePath))
                    {
                        // 3.2. if file not exists in source and exists in replica - delete it it
                        file.Delete();
                        ProvideInfo(sw, "DelFil", destinationFilePath);
                    }                
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public static void ProvideInfo(StreamWriter s, string type, string nameOfFile)
        //File creation/copying/removal operations should be logged to a file and to the console output;
        {
            string s1 = "";
            switch (type)
            {
                case "Cop":
                    s1 = "File copied";
                    break;
                case "Cha":
                    s1 = "File changed";
                    break;
                case "Cr":
                    s1 = "Folder created";
                    break;
                case "DelFil":
                    s1 = "File deleted";
                    break;
                case "DelFol":
                    s1 = "Folder deleted";
                    break;

                default:
                    break;
            }
            DateTime date = DateTime.Now;

            Console.WriteLine($"{date} {s1} " + nameOfFile); // 
            s.WriteLine($"{date} {s1} " + nameOfFile);
        }

    }
    
}
