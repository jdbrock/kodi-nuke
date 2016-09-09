using KodiSharp;
using MahApps.Metro.Controls;
using SonarrSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;
using PropertyChanged;

namespace KodiNuke
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ImplementPropertyChanged]
    public partial class MainWindow : MetroWindow
    {
        public ObservableCollection<Series> Series { get; }
        public Series SelectedSeries { get; set; }

        public ICommand DeleteCommand { get; }

        private bool _refreshed;

        private SonarrClient _sClient;
        private KodiClient _kClient;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _sClient = new SonarrClient("http://192.168.1.50:8989/api/", "9f7f7589ac1942cea5ec5cecdc431945");
            _kClient = new KodiClient("192.168.1.75", userName: "kodi");

            DeleteCommand = new DelegateCommand(async _ => await DeleteAsync());

            Series = new ObservableCollection<Series>();
        }

        protected override void OnActivated(EventArgs e)
        {
            if (!_refreshed)
            {
                Refresh();
                _refreshed = true;
            }
        }

        private async Task Refresh()
        {
            var progress = await this.ShowProgressAsync("Kodi Nuke", "", false);

            try
            {
                progress.SetMessage("Refreshing Sonarr");
                var sSeries = await _sClient.Series.GetAllAsync();

                progress.SetMessage("Refreshing Kodi");
                var kSeries = await _kClient.TV.GetShows();

                var sSeriesLookup = sSeries
                    .Where(x => x.TvdbId != null)
                    .ToDictionary(x => x.TvdbId.Value);

                progress.SetMessage("Matching series");

                // NB: It says IMDB number here, but it's really the ID for TVDB.
                var kSeriesLookup = kSeries
                    .Where(x => !String.IsNullOrWhiteSpace(x.ImdbNumber))
                    .ToDictionary(x => Int32.Parse(x.ImdbNumber));

                var matches = sSeriesLookup.Keys.Intersect(kSeriesLookup.Keys)
                    .Select(key => new Series(kSeriesLookup[key], sSeriesLookup[key]))
                    .OrderBy(x => x.Sonarr.Title);

                var sNonMatches = sSeriesLookup.Keys.Except(kSeriesLookup.Keys)
                    .Select(key => sSeriesLookup[key].Title);

                var kNonMatches = kSeriesLookup.Keys.Except(sSeriesLookup.Keys)
                    .Select(key => kSeriesLookup[key].Title);

                foreach (var sNoImdb in sSeries.Where(x => String.IsNullOrWhiteSpace(x.ImdbId)))
                    Console.WriteLine($"Failed to match series '{sNoImdb.Title}' (no IMDB ID) (Sonarr).");

                foreach (var kNoImdb in kSeries.Where(x => String.IsNullOrWhiteSpace(x.ImdbNumber)))
                    Console.WriteLine($"Failed to match series '{kNoImdb.Title}' (no IMDB ID) (Kodi).");

                foreach (var sNonMatch in sNonMatches)
                    Console.WriteLine($"Failed to match series '{sNonMatch}' (only in Sonarr).");

                foreach (var kNonMatch in kNonMatches)
                    Console.WriteLine($"Failed to match series '{kNonMatch}' (only in Kodi).");

                Series.Clear();

                foreach (var series in matches)
                    Series.Add(series);
            }
            finally
            {
                await progress.CloseAsync();
            }
        }

        private async Task DeleteAsync()
        {
            if (SelectedSeries == null)
                return;

            var dialog = await this.ShowMessageAsync(SelectedSeries.Sonarr.Title, $"Are you absolutely sure you want to delete {SelectedSeries.Sonarr.Title}? This cannot be undone.", MessageDialogStyle.AffirmativeAndNegative);

            var progress = await this.ShowProgressAsync("Kodi Nuke", "", false);

            progress.SetMessage("Removing from Kodi...");
            await _kClient.TV.RemoveShow(SelectedSeries.Kodi.TvShowId);

            progress.SetMessage("Removing from Sonarr and deleting permanently...");
            await _sClient.Series.DeleteAsync(SelectedSeries.Sonarr.Id);

            await progress.CloseAsync();

            Series.Remove(SelectedSeries);
            SelectedSeries = null;
        }
    }
}
