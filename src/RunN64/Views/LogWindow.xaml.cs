using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using cor64;
using NLog;
using RunN64.ViewModels;
using DynamicData;

namespace RunN64.Views
{
    public class LogWindow : Window
    {
        private readonly LogViewListViewModel m_LogViewModel = new LogViewListViewModel();

        public LogWindow()
        {
            InitializeComponent();

            //var logList = (ListBox)this.FindControl<ListBox>("logView");

            foreach (NLogViewer logger in LogManager.Configuration.AllTargets.Where(t => t is NLogViewer).Cast<NLogViewer>())
            {
                var missed = logger.CheckMissedLogs();

                m_LogViewModel.LogSource.AddRange(missed.Select(x => x.LogEvent));

                logger.LogReceived += (msg) =>
                {
                    m_LogViewModel.LogSource.Add(msg.LogEvent);
                };
            }

            DataContext = m_LogViewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}