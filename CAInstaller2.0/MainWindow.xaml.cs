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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
using Microsoft.Win32;

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
        private static Timer aTimer;
        public int countdown;
        private Border myBorder1;
        public bool enoughspace = true;

        public MainWindow()
        {
            InitializeComponent();
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // gets appdata for later

            // commented for registry: installloc = appdata + @"\cubeyrewritten\"; // sets the install location
            // installloc = (string)Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Cubey's Adventures", "install", appdata + @"\cubeyrewritten\");

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Team Cubey\\Cubey's Adventures"))
            {
                if (key != null)
                {
                    Object o = key.GetValue("location");
                    if (o != null)
                    {
                        installloc = (o as String);
                    }
                    else
                    {
                        installloc = appdata + @"\cubeyrewritten\";
                    }
                }
                else
                {
                    installloc = appdata + @"\cubeyrewritten\";
                }
            }

            DriveInfo Drive = new DriveInfo(installloc.Substring(0, 1));
            
            if (Drive.AvailableFreeSpace > 600000000)
            {
                SetTimer(); // starts the countdown...
                countdown = 6; // for 6 seconds
                if(System.IO.File.Exists(installloc + @"\Cubey's Adventures.exe")){
                    aainfo.Text = "Updating/Reinstalling your Cubey's Adventures install\nin " + countdown + " seconds";
                }else
                {
                    aainfo.Text = "Installing to " + installloc + "\nin " + countdown + " seconds"; // sets the text first time
                }
            }
            else
            {
                try
                {
                    aTimer.Enabled = false;
                }
                catch
                {
                    
                }
                aainfo.Text = "There is not enough space on the selected drive (" + Drive.AvailableFreeSpace / 1000000 + "mb/600mb), please clear some space or select another drive";
                enoughspace = false;
            }
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
                DriveInfo Drive = new DriveInfo(dialog.FileName.Substring(0,1));
                if (Drive.AvailableFreeSpace > 600000000)
                {
                    installloc = dialog.FileName; // when they respond ok we set the install location to where they want to install
                    SetTimer(); // reset the timer
                    countdown = 6;
                }
                else
                {
                    aTimer.Enabled = false;
                    aainfo.Text = "There is not enough space on the selected drive (" + Drive.AvailableFreeSpace / 1000000 + "mb/600mb), please clear some space or select another drive";
                    enoughspace = false;
                }
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            countdown -= 1;
            this.Dispatcher.Invoke(() =>
            {
                if (System.IO.File.Exists(installloc + @"\Cubey's Adventures.exe"))
                {
                    aainfo.Text = "Updating/Reinstalling your Cubey's Adventures install\nin " + countdown + " seconds";
                }
                else
                {
                    aainfo.Text = "Installing to " + installloc + "\nin " + countdown + " seconds"; // sets the text first time
                }
            });



            if(countdown == 0){ // when the timer is done
                aTimer.Enabled = false; // disable timer
                
                this.Dispatcher.Invoke(() =>
                {
                    ChangeButton.IsEnabled = false; // disablethe change button
                    aainfo.Text = "Downloading the latest version..."; // update the text
                    change.Foreground = new SolidColorBrush(Color.FromArgb(170, 116, 116, 116));
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

                int maxProgressbarValue = 100;
                var taskbarInstance = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
                taskbarInstance.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.Normal);

                
                taskbarInstance.SetProgressValue(e.ProgressPercentage, maxProgressbarValue);
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
                if (!Directory.Exists(installloc + @"\MonoBleedingEdge"))
                {
                    // wipe the IL2CPP version
                    System.IO.File.Delete(installloc + @"\UnityPlayer.dll");
                    System.IO.File.Delete(installloc + @"\GameAssembly.dll");
                    System.IO.File.Delete(installloc + @"\baselib.dll");
                    System.IO.File.Delete(installloc + @"\Cubey's Adventures.exe");
                    System.IO.File.Delete(installloc + @"\UnityCrashHandler64.exe");
                    System.IO.File.Delete(installloc + @"\ca.ico");
                    Directory.Delete(installloc + @"\Cubey's Adventures_Data", true);
                }
                else {
                    // wipe the MONO version with il2cpp just in case lol
                    System.IO.File.Delete(installloc + @"\UnityPlayer.dll");
                    System.IO.File.Delete(installloc + @"\GameAssembly.dll");
                    System.IO.File.Delete(installloc + @"\baselib.dll");
                    System.IO.File.Delete(installloc + @"\Cubey's Adventures.exe");
                    System.IO.File.Delete(installloc + @"\UnityCrashHandler64.exe");
                    System.IO.File.Delete(installloc + @"\ca.ico");
                    Directory.Delete(installloc + @"\MonoBleedingEdge", true);
                    Directory.Delete(installloc + @"\Cubey's Adventures_Data", true);
                }
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
            if(System.IO.File.Exists(installloc + @"\Cubey's Adventures.ico"))
            {
                System.IO.File.Delete(installloc + @"\Cubey's Adventures.ico");
            }
            System.IO.File.Move(temp + @"ca.ico", installloc + @"\Cubey's Adventures.ico"); // move the ico

            System.IO.File.Delete(appdata + @"\Microsoft\Windows\Start Menu\Programs\Cubey's Adventures.lnk");
            CreateShortcut("Cubey's Adventures", appdata + @"\Microsoft\Windows\Start Menu\Programs", installloc + @"\Cubey's Adventures.exe"); // create a shortcut

            this.Dispatcher.Invoke(() =>
            {
                aainfo.Text = "Launching game..."; // inform user we're starting the game
            });

            Process.Start(installloc + @"\Cubey's Adventures.exe"); // start game

            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\Team Cubey\\Cubey's Adventures");
            key.SetValue("location", installloc);
            key.Close();

            System.IO.File.Delete(temp + @"ca.zip"); // then remove zip

            string src = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string dest = installloc + "install.exe";
            if (!System.IO.File.Exists(dest))
            {
                System.IO.File.Copy(src, dest);
            }

            System.Environment.Exit(1); // exit
        }

        public void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
        {
            string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
                
            shortcut.Description = "Cubey's Adventures (Private Beta)";   // The description of the shortcut
            shortcut.IconLocation = installloc + @"\Cubey's Adventures.ico";           // The icon of the shortcut
            shortcut.TargetPath = targetFileLocation;                 // The path of the file that will launch when the shortcut is run
            shortcut.Save();                                    // Save the shortcut
        }

        private void drag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}