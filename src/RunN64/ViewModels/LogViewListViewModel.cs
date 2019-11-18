using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using NLog;
using Avalonia.Media;

namespace RunN64.ViewModels {
    public class LogViewListViewModel : ViewModelBase {
        private readonly SourceList<LogEventInfo> m_LogSource = new SourceList<LogEventInfo>();
        private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Colors.Red);
        private static readonly SolidColorBrush WarningBrush = new SolidColorBrush(Colors.Yellow);
        private static readonly SolidColorBrush NormalBush = new SolidColorBrush(Colors.Green);
        private static readonly SolidColorBrush DebugBrush = new SolidColorBrush(Colors.Magenta);

        private readonly ReadOnlyObservableCollection<LogViewItemViewModel> _items;
        public ReadOnlyObservableCollection<LogViewItemViewModel> Items => _items;

        public LogViewListViewModel() {
            m_LogSource
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform((e) => new LogViewItemViewModel(e.FormattedMessage, GetLogColorBrush(e)))
            .Bind(out _items)
            .Subscribe();
        }

        public ISourceList<LogEventInfo> LogSource => m_LogSource;

        private static Brush GetLogColorBrush(LogEventInfo info) {
            if (info.Level == LogLevel.Error) {
                return ErrorBrush;
            }
            else if (info.Level == LogLevel.Warn) {
                return WarningBrush;
            }
            else if (info.Level == LogLevel.Debug) {
                return DebugBrush;
            }
            else {
                return NormalBush;
            }
        }
    }
}