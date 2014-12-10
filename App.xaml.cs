﻿#region

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using LeagueSharp.Loader.Class;
using LeagueSharp.Loader.Data;
using Microsoft.Win32;
using System.IO;

#endregion

namespace LeagueSharp.Loader
{

    public partial class App
    {
        private Mutex _mutex;

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        protected override void OnStartup(StartupEventArgs e)
        {
            if(File.Exists(Updater.SetupFile))
            {
                Thread.Sleep(1000);
            }

            bool createdNew;
            _mutex = new Mutex(true, @"LeagueSharp.Loader.Mutex", out createdNew);
            if (!createdNew)
            {
                if (e.Args.Length > 0)
                {
                    var wnd = Injection.FindWindow(IntPtr.Zero, "LeagueSharp");
                    if (wnd != IntPtr.Zero)
                    {
                        Clipboard.SetText(e.Args[0]);
                        ShowWindow(wnd, 5);
                        SetForegroundWindow(wnd);
                    }
                }

                _mutex = null;
                Environment.Exit(0);
            }

            Utility.CreateFileFromResource(Directories.ConfigFilePath, "LeagueSharp.Loader.Resources.config.xml");

            try
            {
                Config.Instance = ((Config)Utility.MapXmlFileToClass(typeof(Config), Directories.ConfigFilePath));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Couldn't load config.xml.");
                File.Delete(Directories.ConfigFilePath);
                Environment.Exit(0);
            }

            //Load the language resources.
            var dict = new ResourceDictionary();
            
            if (Config.Instance.SelectedLanguage != null)
            {
                dict.Source = new Uri("..\\Resources\\Language\\" + Config.Instance.SelectedLanguage + ".xaml", UriKind.Relative);
            }
            else
            {
                var lid = Thread.CurrentThread.CurrentCulture.ToString().Contains("-") ? Thread.CurrentThread.CurrentCulture.ToString().Split('-')[0].ToUpperInvariant() : Thread.CurrentThread.CurrentCulture.ToString().ToUpperInvariant();
                switch (lid)
                {
                    case "DE":
                        dict.Source = new Uri("..\\Resources\\Language\\German.xaml", UriKind.Relative);
                        break;
                    case "AR":
                        dict.Source = new Uri("..\\Resources\\Language\\Arabic.xaml", UriKind.Relative);
                        break;
                    case "ES":
                        dict.Source = new Uri("..\\Resources\\Language\\Spanish.xaml", UriKind.Relative);
                        break;
                    case "FR":
                        dict.Source = new Uri("..\\Resources\\Language\\French.xaml", UriKind.Relative);
                        break;
                    case "IT":
                        dict.Source = new Uri("..\\Resources\\Language\\Italian.xaml", UriKind.Relative);
                        break;
                    case "KO":
                        dict.Source = new Uri("..\\Resources\\Language\\Korean.xaml", UriKind.Relative);
                        break;
                    case "NL":
                        dict.Source = new Uri("..\\Resources\\Language\\Dutch.xaml", UriKind.Relative);
                        break;
                    case "PL":
                        dict.Source = new Uri("..\\Resources\\Language\\Polish.xaml", UriKind.Relative);
                        break;
                    case "PT":
                        dict.Source = new Uri("..\\Resources\\Language\\Portuguese.xaml", UriKind.Relative);
                        break;
                    case "RO":
                        dict.Source = new Uri("..\\Resources\\Language\\Romanian.xaml", UriKind.Relative);
                        break;
                    case "RU":
                        dict.Source = new Uri("..\\Resources\\Language\\Russian.xaml", UriKind.Relative);
                        break;
                    case "SE":
                        dict.Source = new Uri("..\\Resources\\Language\\Swedish.xaml", UriKind.Relative);
                        break;
                    case "TR":
                        dict.Source = new Uri("..\\Resources\\Language\\Turkish.xaml", UriKind.Relative);
                        break;
                    case "VI":
                        dict.Source = new Uri("..\\Resources\\Language\\Vietnamese.xaml", UriKind.Relative);
                        break;
                    case "ZH":
                        dict.Source = new Uri("..\\Resources\\Language\\Chinese.xaml", UriKind.Relative);
                        break;
                    default:
                        dict.Source = new Uri("..\\Resources\\Language\\English.xaml", UriKind.Relative);
                        break;
                }
            }
            
            Resources.MergedDictionaries.Add(dict);
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mutex != null)
                _mutex.ReleaseMutex();
            base.OnExit(e);
        }
    }
}