using System;
using System.Windows;
using System.Net;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using IWshRuntimeLibrary;
using System.Timers;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CAInstaller2._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // 
    // The idea of this installer is to allow us to get the latest version of cubey and install that effortlessly
    // The way we do that is we just get a txt from online which has a link to the latest zip in it, which is hosted on my upload server, and download the link inside.
    // This allows me to update the txt to point it to another download location when updating the game, so i wont have to touch the installer.

    public partial class MainWindow : Window
    {
        public string installloc;
        private static System.Timers.Timer aTimer;
        public int countdown;

        public MainWindow()
        {
            InitializeComponent();
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // gets appdata for later

            installloc = appdata + @"\cubeyrewritten\"; // sets the install location
            SetTimer(); // starts the countdown...
            countdown = 6; // for 6 seconds
            aainfo.Text = "Installing to " + installloc + " in " + countdown + " seconds"; // sets the text first time
        }


        private void SetTimer()
        {
            // Create a timer with a 1 second interval.
            aTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true; 
            aTimer.Enabled = true;
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            aTimer.Enabled = false; // disable the timer
            CommonOpenFileDialog dialog = new CommonOpenFileDialog(); // open a file dialog
            dialog.InitialDirectory = installloc;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                installloc = dialog.FileName; // when they respond ok we set the install location to where they want to install
            }
            SetTimer(); // reset the timer
            countdown = 6;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            countdown -= 1;
            this.Dispatcher.Invoke(() =>
            {
                aainfo.Text = "Installing to " + installloc + " in " + countdown + " seconds"; // set the text
            });
            if(countdown == 0){ // when the timer is done
                aTimer.Enabled = false; // disable timer
                
                this.Dispatcher.Invoke(() =>
                {
                    ChangeButton.IsEnabled = false; // disablethe change button
                    aainfo.Text = "Downloading the latest version..."; // update the text
                    change.Text = ""; // remove the "click here" text
                });
                install(); // install time
            }
        }

        private void install()
        {
            string temp = System.IO.Path.GetTempPath(); // get temp

            WebClient webClient3 = new WebClient();
            webClient3.DownloadFile(new Uri("https://upload.hubza.co.uk/i/ca-icon.ico"), temp + @"ca.ico"); // download the ico for the shortcut

            WebClient webClient = new WebClient();
            System.IO.File.Delete(temp + @"ca-latest.txt");
            webClient.DownloadFile(new Uri("https://upload.hubza.co.uk/i/ca-latest.txt"), temp + @"ca-latest.txt"); // get the latest version, this is usually just a link to another download

            WebClient webClient2 = new WebClient();
            webClient2.DownloadFileCompleted += new AsyncCompletedEventHandler(CompletedCA);
            webClient2.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
            string[] downloc = System.IO.File.ReadAllLines(temp + @"ca-latest.txt"); // get the link
            webClient2.DownloadFileAsync(new Uri(downloc[0]), temp + @"ca.zip"); // download latest version
            //MessageBox.Show(downloc[0]);
           
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                progrss.Value = e.ProgressPercentage; // update progress bar
                aainfo.Text = "Downloading the latest version: " + e.ProgressPercentage + "%"; // update the text
            });
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            //MessageBox.Show("Download completed!");
        }
        
        private void CompletedCA(object sender, AsyncCompletedEventArgs e)
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // get appdata

            //MessageBox.Show("Download completed!");

            string temp = System.IO.Path.GetTempPath(); // get temp

            if (Directory.Exists(installloc)) // checks if the install location exists, then deletes the cubey files
            {
                System.IO.File.Delete(installloc + @"\UnityPlayer.dll");
                System.IO.File.Delete(installloc + @"\Cubey's Adventures.exe");
                System.IO.File.Delete(installloc + @"\UnityCrashHandler64.exe");
                System.IO.File.Delete(installloc + @"\ca.ico");
                Directory.Delete(installloc + @"\MonoBleedingEdge", true);
                Directory.Delete(installloc + @"\Cubey's Adventures_Data", true);
            }
            else
            {
                Directory.CreateDirectory(installloc); // create the install directory
            }
            this.Dispatcher.Invoke(() => // set the text to inform the user that we're now unzipping
            {
                aainfo.Text = "Unzipping to " + installloc + "...";
            });
            ZipFile.ExtractToDirectory(temp + @"ca.zip", installloc); // extract it to the install location
            System.IO.File.Move(temp + @"ca.ico", appdata + @"\cubeyrewritten\ca.ico"); // move the ico

            System.IO.File.Delete(appdata + @"\Microsoft\Windows\Start Menu\Programs\Cubey's Adventures.lnk");
            CreateShortcut("Cubey's Adventures", appdata + @"\Microsoft\Windows\Start Menu\Programs", appdata + @"\cubeyrewritten\Cubey's Adventures.exe"); // create a shortcut

            this.Dispatcher.Invoke(() =>
            {
                aainfo.Text = "Launching game..."; // inform user we're starting the game
            });

            Process.Start(installloc + @"\Cubey's Adventures.exe"); // start game

            System.IO.File.Delete(temp + @"ca.zip"); // then remove zip

            System.Environment.Exit(1); // exit
        }

        public void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
        {
            string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
                
            shortcut.Description = "Cubey's Adventures";   // The description of the shortcut
            shortcut.IconLocation = installloc + @"\ca.ico";           // The icon of the shortcut
            shortcut.TargetPath = targetFileLocation;                 // The path of the file that will launch when the shortcut is run
            shortcut.Save();                                    // Save the shortcut
        }
    }
}