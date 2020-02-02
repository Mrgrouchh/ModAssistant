using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using ModAssistant.Pages;
using static ModAssistant.Http;

namespace ModAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;
        public static bool ModsOpened = false;
        public static string GameVersion;
        public TaskCompletionSource<bool> VersionLoadStatus = new TaskCompletionSource<bool>();

        public string MainText
        {
            get
            {
                return MainTextBlock.Text;
            }
            set
            {
                Dispatcher.Invoke(new Action(() => { MainWindow.Instance.MainTextBlock.Text = value; }));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            VersionText.Text = App.Version;

            if (Utils.IsVoid())
            {
                Main.Content = Invalid.Instance;
                MainWindow.Instance.ModsButton.IsEnabled = false;
                MainWindow.Instance.OptionsButton.IsEnabled = false;
                MainWindow.Instance.IntroButton.IsEnabled = false;
                MainWindow.Instance.AboutButton.IsEnabled = false;
                MainWindow.Instance.GameVersionsBox.IsEnabled = false;
                return;
            }

            Task.Run(() => LoadVersionsAsync());

            if (!Properties.Settings.Default.Agreed || string.IsNullOrEmpty(Properties.Settings.Default.LastTab))
            {
                Main.Content = Intro.Instance;
            }
            else
            {
                switch (Properties.Settings.Default.LastTab)
                {
                    case "Intro":
                        Main.Content = Intro.Instance;
                        break;
                    case "Mods":
                        Mods.Instance.LoadMods();
                        ModsOpened = true;
                        Main.Content = Mods.Instance;
                        break;
                    case "About":
                        Main.Content = About.Instance;
                        break;
                    case "Options":
                        Main.Content = Options.Instance;
                        break;
                    default:
                        Main.Content = Intro.Instance;
                        break;
                }
            }
        }

        private async void LoadVersionsAsync()
        {
            try
            {
                var resp = await HttpClient.GetAsync(Utils.Constants.BeatModsAPIUrl + "version");
                var body = await resp.Content.ReadAsStringAsync();

                List<string> versions = JsonSerializer.Deserialize<string[]>(body).ToList();
                Dispatcher.Invoke(() =>
                {
                    GameVersion = GetGameVersion(versions);

                    GameVersionsBox.ItemsSource = versions;
                    GameVersionsBox.SelectedValue = GameVersion;

                    if (!string.IsNullOrEmpty(GameVersion) && Properties.Settings.Default.Agreed)
                    {
                        MainWindow.Instance.ModsButton.IsEnabled = true;
                    }
                });

                VersionLoadStatus.SetResult(true);
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() =>
                {
                    GameVersionsBox.IsEnabled = false;
                    MessageBox.Show("Could not load game versions, Mods tab will be unavailable.\n" + e);
                });

                VersionLoadStatus.SetResult(false);
            }
        }

        private string GetGameVersion(List<string> versions)
        {
            string version = Utils.GetVersion();
            if (!string.IsNullOrEmpty(version) && versions.Contains(version))
            {
                return version;
            }

            string versionsString = String.Join(",", versions.ToArray());
            if (Properties.Settings.Default.AllGameVersions != versionsString)
            {
                Properties.Settings.Default.AllGameVersions = versionsString;
                Properties.Settings.Default.Save();
                Utils.ShowMessageBoxAsync("It looks like there's been a game update.\n\nPlease double check that the correct version is selected at the bottom left corner!", "New Game Version Detected!");
                return versions[0];
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.GameVersion) && versions.Contains(Properties.Settings.Default.GameVersion))
                return Properties.Settings.Default.GameVersion;
            return versions[0];
        }

        private void ModsButton_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = Mods.Instance;
            Properties.Settings.Default.LastTab = "Mods";
            Properties.Settings.Default.Save();

            if (!ModsOpened)
            {
                Mods.Instance.LoadMods();
                ModsOpened = true;
                return;
            }

            if (Mods.Instance.PendingChanges)
            {
                Mods.Instance.LoadMods();
                Mods.Instance.PendingChanges = false;
            }
        }

        private void IntroButton_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = Intro.Instance;
            Properties.Settings.Default.LastTab = "Intro";
            Properties.Settings.Default.Save();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = About.Instance;
            Properties.Settings.Default.LastTab = "About";
            Properties.Settings.Default.Save();
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = Options.Instance;
            Properties.Settings.Default.LastTab = "Options";
            Properties.Settings.Default.Save();
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            Mods.Instance.InstallMods();
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            if ((Mods.ModListItem)Mods.Instance.ModsListView.SelectedItem == null)
            {
                MessageBox.Show("No mod selected");
                return;
            }
            Mods.ModListItem mod = ((Mods.ModListItem)Mods.Instance.ModsListView.SelectedItem);
            string infoUrl = mod.ModInfo.link;
            if (string.IsNullOrEmpty(infoUrl))
            {
                MessageBox.Show(mod.ModName + " does not have an info page");
            }
            else
            {
                System.Diagnostics.Process.Start(infoUrl);
            }
        }

        private void GameVersionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string oldGameVersion = GameVersion;

            GameVersion = (sender as ComboBox).SelectedItem.ToString();

            if (string.IsNullOrEmpty(oldGameVersion)) return;

            Properties.Settings.Default.GameVersion = GameVersion;
            Properties.Settings.Default.Save();

            if (ModsOpened)
            {
                Mods.Instance.LoadMods();
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            About.Instance.PatUp.IsOpen = false;
            About.Instance.PatButton.IsEnabled = true;
            About.Instance.HugUp.IsOpen = false;
            About.Instance.HugButton.IsEnabled = true;
        }
    }
}
