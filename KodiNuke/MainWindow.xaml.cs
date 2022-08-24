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
using KodiSharp.MySQL;
using KodiNuke.Models;
using System.Windows.Threading;
using KodiNuke.Utility;
using KodiNuke.Config;
using System.IO;

namespace KodiNuke
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ImplementPropertyChanged]
    public partial class MainWindow : MetroWindow
    {
        public ObservableCollection<Series> FilteredSeries { get; set; }
        public ObservableCollection<Series> Series { get; }
        public Series SelectedSeries { get; set; }

        public ObservableCollection<Movie> Movies { get; }
        public Movie SelectedMovie { get; set; }

        public ICommand TvSortByNameCommand { get; set; }
        public ICommand TvSortBySizeCommand { get; set; }
        public ICommand TvDeleteCommand { get; }

        public IList<ButtonViewModel> Buttons { get; private set; }

        public string DiskSpaceReport { get; set; }

        public Configuration Config { get; set; }

        private System.Linq.Expressions.Expression<Func<IQueryable<Series>, IQueryable<Series>>> _tvSort;

        private bool _refreshed;

        private SonarrClient _sClient;

#if !DISABLE_KODI
        private KodiClient _kClient;
        private KodiDatabase _kDatabase;
#endif

        private IDictionary<string, ButtonViewModel> _buttonsByPath;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Config = new Configuration();
            Config.TvPaths = new[]
            {
                new TvPath(@"U:\", @"U:\TV\",         "smb://192.168.1.41/archive/TV/",         "smb://batu/archiveTV/"),
                new TvPath(@"V:\", @"V:\TV (Clean)\", "smb://192.168.1.42/archive/TV (Clean)/", "smb://fathom/archive/TV (Clean)/"),
                new TvPath(@"W:\", @"W:\TV\",         "smb://192.168.1.43/archive/TV/",         "smb://kalopsia/archive/TV/"),
                new TvPath(@"P:\", @"P:\TV\",         "smb://192.168.1.51/Purgatory/TV/",       "smb://portland/Purgatory/TV/"),
            };

            _sClient = new SonarrClient("http://192.168.1.150:8989/api/", "9f7f7589ac1942cea5ec5cecdc431945");

#if !DISABLE_KODI
            _kClient = new KodiClient("192.168.1.180", userName: "xbmc", password: "xbmc");
            _kDatabase = new KodiDatabase("server=192.168.1.58;user=xbmc;database=MyVideos107;port=3306;password=xbmc;SslMode=none");
#endif

            _tvSort = x => x.OrderBy(y => y.Sonarr.Title);

            TvSortByNameCommand = new DelegateCommand(TvSortByName);
            TvSortBySizeCommand = new DelegateCommand(TvSortBySize);
            TvDeleteCommand = new DelegateCommand(async _ => await DeleteAsync());

            Series = new ObservableCollection<Series>();
            FilteredSeries = new ObservableCollection<Series>();

            Movies = new ObservableCollection<Movie>();

            StartMonitoringDiskSpace();
        }

        private async Task StartMonitoringDiskSpace()
        {
            while (true)
            {
                var paths = Config.TvPaths;

                if (_buttonsByPath == null)
                {
                    _buttonsByPath = paths
                        .ToDictionary(x => x.RootPath, x => new ButtonViewModel("", new DelegateCommand(_ => MoveSelectedSeriesTo(x))));

                    Buttons = _buttonsByPath.Values
                        .OrderBy(x => x.Text)
                        .ToList();
                }

                var sb = new StringBuilder("Disk Space:    ");

                foreach (var path in paths)
                {
                    double gb = 0;

                    try
                    {
                        var bytes = await Task.Run(
                            () => DiskSpaceUtility.GetFreeDiskSpaceBytes(path.RootPath)
                        );

                        gb = Math.Round(bytes / (1024d * 1024d * 1024d), 2);

                        sb.Append($"{path.RootPath}: {gb}GB    ");
                    }
                    catch { }

                    _buttonsByPath[path.RootPath].Text = $"Move to {path.RootPath} ({gb}GB free)";
                }

                DiskSpaceReport = sb.ToString();

                await Task.Delay(5000);
            }
        }

        private async void MoveSelectedSeriesTo(TvPath configTargetPath)
        {
            var progress = await this.ShowProgressAsync("Kodi Nuke", "", false);


            try
            {
                var selectedSeries = SelectedSeries;

                if (string.IsNullOrWhiteSpace(selectedSeries?.Sonarr?.Path))
                {
                    MessageBox.Show($"Path not defined.");
                    return;
                }

                var localSourcePath = selectedSeries.Sonarr.Path;
                if (localSourcePath.StartsWith(configTargetPath.RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Series is already in {configTargetPath.RootPath}");
                    return;
                }

                var configSourcePath = Config.TvPaths
                    .FirstOrDefault(x => localSourcePath.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));

                if (configSourcePath == null)
                {
                    MessageBox.Show($"Couldn't find source path for {selectedSeries.Sonarr.Title}");
                    return;
                }

                var localTargetPath = RegexUtility.CaseInsensitiveReplaceAtStart(
                    selectedSeries.Sonarr.Path,
                    configSourcePath.Path,
                    configTargetPath.Path
                );

                if (!Directory.Exists(localSourcePath))
                {
                    MessageBox.Show($"Couldn't find path locally: {localSourcePath}");
                    return;
                }

                progress.SetMessage($"Moving {localSourcePath} to {localTargetPath}...");

                // Move files.
                await Task.Factory.StartNew(() => Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(localSourcePath, localTargetPath,
                    Microsoft.VisualBasic.FileIO.UIOption.AllDialogs,
                    Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException
                ));

                // First path is preferred.
                var targetNetworkPath = configTargetPath.NetworkPaths.First();

                progress.SetMessage("Updating Kodi database...");

#if !DISABLE_KODI
                using var transaction = _kDatabase.BeginTransaction();
                int rowsChanged = 0;

                foreach (var sourceNetworkPath in configSourcePath.NetworkPaths)
                {
                    var findPath = RegexUtility.CaseInsensitiveReplaceAtStart(
                            localSourcePath,
                            configSourcePath.Path,
                            sourceNetworkPath)
                        .Replace("\\", "/");

                    var replacePath = RegexUtility.CaseInsensitiveReplaceAtStart(
                            localTargetPath,
                            configTargetPath.Path,
                            targetNetworkPath)
                        .Replace("\\", "/");

                    rowsChanged += _kDatabase.FindRenamePaths(transaction, findPath, replacePath);
                }
#endif

                var series = await _sClient.Series.UpdateAsync(selectedSeries.Sonarr.Id, o =>
                {
                    dynamic d = (dynamic)o;
                    d.path = localTargetPath;
                });

#if !DISABLE_KODI
                transaction.Commit();
                transaction?.Dispose();
#endif

                progress.SetMessage("Updating Sonarr series...");

                SelectedSeries.Sonarr = series;

            }
            catch (OperationCanceledException)
            {
                if (progress != null && progress.IsOpen)
                    await progress?.CloseAsync();
            }
            finally
            {
                if (progress != null && progress.IsOpen)
                    await progress?.CloseAsync();
            }
        }

        private void TvSortBySize(object obj)
        {
            _tvSort = x => x.OrderByDescending(y => y.Sonarr.SizeOnDisk);
            UpdateFilteredSeries();
        }

        private void TvSortByName(object obj)
        {
            _tvSort = x => x.OrderBy(y => y.Sonarr.Title);
            UpdateFilteredSeries();
        }

        private void UpdateFilteredSeries()
        {
            var sort = _tvSort.Compile();
            var sorted = sort(Series.AsQueryable());

            var prevSelection = SelectedSeries;

            FilteredSeries.Clear();

            foreach (var item in sorted)
                FilteredSeries.Add(item);

            SelectedSeries = prevSelection;
        }

        protected override async void OnActivated(EventArgs e)
        {
            if (!_refreshed)
            {
                await Refresh();
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

#if !DISABLE_KODI
                var kSeries = await _kClient.TV.GetShows();
                //var kMovies = await _kClient.Movies.GetMovies();
#endif

                var sSeriesLookup = sSeries
                    .Where(x => x.TvdbId != null)
                    .ToDictionary(x => x.TvdbId.Value);

                progress.SetMessage("Matching series");

#if !DISABLE_KODI
                // NB: It says IMDB number here, but it's really the ID for TVDB.
                var kSeriesLookup = new Dictionary<int, KodiTvShow>();

                //var kSeriesLookup = kSeries
                //    .Where(x => !String.IsNullOrWhiteSpace(x.ImdbNumber))
                //    .ToDictionary(x => Int32.Parse(x.ImdbNumber));

                // Replace above with set/add as we can sometimes have multiple series
                // with the same TVDB ID.
                foreach (var series in kSeries)
                {
                    if (string.IsNullOrWhiteSpace(series.ImdbNumber))
                        continue;

                    if (!int.TryParse(series.ImdbNumber, out var imdbNumber))
                        continue;

                    kSeriesLookup[imdbNumber] = series;
                }

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
#else
                var matches = sSeriesLookup
                    .Select(x => new Series(x.Value))
                    .OrderBy(x => x.Sonarr.Title);
#endif

                Series.Clear();

                //foreach (var series in matches.Where(x => x.Sonarr.Path.StartsWith("U:", StringComparison.OrdinalIgnoreCase)))
                foreach (var series in matches)
                    Series.Add(series);

                UpdateFilteredSeries();

                //Movies.Clear();

                //foreach (var movie in kMovies)
                //    Movies.Add(new Movie
                //    {
                //        Kodi = movie
                //    });
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.ToString());
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

#if !DISABLE_KODI
            progress.SetMessage("Removing from Kodi...");
            await _kClient.TV.RemoveShow(SelectedSeries.Kodi.TvShowId);
#endif

            progress.SetMessage("Removing from Sonarr and deleting permanently...");
            await _sClient.Series.DeleteAsync(SelectedSeries.Sonarr.Id);

            await progress.CloseAsync();

            Series.Remove(SelectedSeries);
            FilteredSeries.Remove(SelectedSeries);

            SelectedSeries = null;
        }
    }
}
