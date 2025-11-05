using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace StudentRegistry
{
    public partial class MainWindow : Window
    {
        private const string DataFile = "tanulok.json";
        private List<Tanulo> tanulok;

        public MainWindow()
        {
            InitializeComponent();
            tanulok = BetoltAdatok();
            DpBeiratkozas.SelectedDate = DateTime.Now;
        }

        private void ChkKollegista_Checked(object sender, RoutedEventArgs e)
        {
            CmbKollegium.IsEnabled = true;
        }

        private void ChkKollegista_Unchecked(object sender, RoutedEventArgs e)
        {
            CmbKollegium.IsEnabled = false;
            CmbKollegium.SelectedIndex = -1;
        }

        private void BtnMentes_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidalAdatok())
                return;

            var ujTanulo = new Tanulo
            {
                Nev = TxtNev.Text.Trim(),
                SzuletesiHely = TxtSzulHely.Text.Trim(),
                SzuletesiIdo = DpSzulIdo.SelectedDate!.Value,
                AnyjaNeve = TxtAnyja.Text.Trim(),
                Lakcim = TxtLakcim.Text.Trim(),
                BeiratkozasIdopontja = DpBeiratkozas.SelectedDate!.Value,
                Szak = (CmbSzak.SelectedItem as ComboBoxItem)?.Content.ToString(),
                Osztaly = (CmbOsztaly.SelectedItem as ComboBoxItem)?.Content.ToString(),
                Kollegista = ChkKollegista.IsChecked == true,
                Kollegium = ChkKollegista.IsChecked == true ?
                    (CmbKollegium.SelectedItem as ComboBoxItem)?.Content.ToString() : null,
                Debreceni = ChkDebreceni.IsChecked == true
            };

            tanulok.Add(ujTanulo);
            GeneralSorszamokat();
            MentAdatok();

            TxtStatus.Text = $"Sikeres mentés! Napló sorszám: {ujTanulo.NaploSorszam}, Törzslap szám: {ujTanulo.TorzslapSzam}";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Green;

            UresitMezok();
        }

        private void BtnUjKezdese_Click(object sender, RoutedEventArgs e)
        {
            UresitMezok();
            TxtStatus.Text = "";
        }

        private void BtnFrissites_Click(object sender, RoutedEventArgs e)
        {
            tanulok = BetoltAdatok();
            DgTanulok.ItemsSource = null;
            DgTanulok.ItemsSource = tanulok;
            FrissitOsszesito();
        }

        private void BtnTorles_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tanulo = button?.Tag as Tanulo;

            if (tanulo == null)
                return;

            var result = MessageBox.Show(
                $"Biztosan törölni szeretnéd a következő tanulót?\n\n" +
                $"Név: {tanulo.Nev}\n" +
                $"Osztály: {tanulo.Osztaly}\n" +
                $"Napló sorszám: {tanulo.NaploSorszam}",
                "Törlés megerősítése",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                tanulok.Remove(tanulo);
                GeneralSorszamokat();
                MentAdatok();
                DgTanulok.ItemsSource = null;
                DgTanulok.ItemsSource = tanulok;
                FrissitOsszesito();
                MessageBox.Show("A tanuló sikeresen törölve!", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnStatisztika_Click(object sender, RoutedEventArgs e)
        {
            var statisztika = new System.Text.StringBuilder();
            statisztika.AppendLine("=== STATISZTIKA ===\n");

            var kollegistakSzama = tanulok.Count(t => t.Kollegista);
            var debreceniSzama = tanulok.Count(t => t.Debreceni);
            var bejarosSzama = tanulok.Count(t => !t.Kollegista && !t.Debreceni);

            statisztika.AppendLine($"Kollégisták száma: {kollegistakSzama}");
            statisztika.AppendLine($"Debreceniek száma: {debreceniSzama}");
            statisztika.AppendLine($"Bejárósok száma: {bejarosSzama}");
            statisztika.AppendLine($"Összes tanuló: {tanulok.Count}\n");

            statisztika.AppendLine("=== ÉVENKÉNTI FELVÉTEL SZAKONKÉNT ===\n");

            var evenkent = tanulok
                .GroupBy(t => t.BeiratkozasIdopontja.Year)
                .OrderBy(g => g.Key);

            foreach (var ev in evenkent)
            {
                statisztika.AppendLine($"{ev.Key}:");
                var szakonkent = ev.GroupBy(t => t.Szak).OrderBy(g => g.Key);
                foreach (var szak in szakonkent)
                {
                    statisztika.AppendLine($"  {szak.Key}: {szak.Count()} fő");
                }
                statisztika.AppendLine($"  Összesen: {ev.Count()} fő\n");
            }

            MessageBox.Show(statisztika.ToString(), "Statisztika", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool ValidalAdatok()
        {
            if (string.IsNullOrWhiteSpace(TxtNev.Text))
            {
                MessageBox.Show("A név megadása kötelező!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtSzulHely.Text))
            {
                MessageBox.Show("A születési hely megadása kötelező!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!DpSzulIdo.SelectedDate.HasValue)
            {
                MessageBox.Show("A születési idő megadása kötelező!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtAnyja.Text))
            {
                MessageBox.Show("Az anyja nevének megadása kötelező!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtLakcim.Text))
            {
                MessageBox.Show("A lakcím megadása kötelező!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!DpBeiratkozas.SelectedDate.HasValue)
            {
                MessageBox.Show("A beiratkozás időpontjának megadása kötelező!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (CmbSzak.SelectedIndex < 0)
            {
                MessageBox.Show("A szak kiválasztása kötelező!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (CmbOsztaly.SelectedIndex < 0)
            {
                MessageBox.Show("Az osztály kiválasztása kötelező!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (ChkKollegista.IsChecked == true && CmbKollegium.SelectedIndex < 0)
            {
                MessageBox.Show("Kollégista esetén a kollégium kiválasztása kötelező!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void UresitMezok()
        {
            TxtNev.Clear();
            TxtSzulHely.Clear();
            DpSzulIdo.SelectedDate = null;
            TxtAnyja.Clear();
            TxtLakcim.Clear();
            DpBeiratkozas.SelectedDate = DateTime.Now;
            CmbSzak.SelectedIndex = -1;
            CmbOsztaly.SelectedIndex = -1;
            ChkKollegista.IsChecked = false;
            CmbKollegium.SelectedIndex = -1;
            ChkDebreceni.IsChecked = false;
        }

        private void GeneralSorszamokat()
        {
            var osztalyok = tanulok.GroupBy(t => t.Osztaly);

            foreach (var osztaly in osztalyok)
            {
                var szeptemberElott = osztaly
                    .Where(t => t.BeiratkozasIdopontja.Month < 9 ||
                               (t.BeiratkozasIdopontja.Month == 9 && t.BeiratkozasIdopontja.Day == 1))
                    .OrderBy(t => t.Nev)
                    .ToList();

                var szeptemberUtan = osztaly
                    .Where(t => t.BeiratkozasIdopontja.Month > 9 ||
                               (t.BeiratkozasIdopontja.Month == 9 && t.BeiratkozasIdopontja.Day > 1))
                    .OrderBy(t => t.BeiratkozasIdopontja)
                    .ToList();

                int sorszam = 1;
                foreach (var tanulo in szeptemberElott)
                {
                    tanulo.NaploSorszam = sorszam;
                    tanulo.TorzslapSzam = $"{sorszam}/{tanulo.BeiratkozasIdopontja.Year}";
                    sorszam++;
                }

                foreach (var tanulo in szeptemberUtan)
                {
                    tanulo.NaploSorszam = sorszam;
                    tanulo.TorzslapSzam = $"{sorszam}/{tanulo.BeiratkozasIdopontja.Year}";
                    sorszam++;
                }
            }
        }

        private void MentAdatok()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(tanulok, options);
                File.WriteAllText(DataFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt a mentés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<Tanulo> BetoltAdatok()
        {
            try
            {
                if (File.Exists(DataFile))
                {
                    var json = File.ReadAllText(DataFile);
                    return JsonSerializer.Deserialize<List<Tanulo>>(json) ?? new List<Tanulo>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt a betöltés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return new List<Tanulo>();
        }

        private void FrissitOsszesito()
        {
            if (tanulok.Count == 0)
            {
                TxtOsszesito.Text = "Nincs még tanuló a rendszerben.";
                return;
            }

            TxtOsszesito.Text = $"Összes tanuló: {tanulok.Count} fő | " +
                               $"Kollégisták: {tanulok.Count(t => t.Kollegista)} fő | " +
                               $"Debreceniek: {tanulok.Count(t => t.Debreceni)} fő | " +
                               $"Bejárósok: {tanulok.Count(t => !t.Kollegista && !t.Debreceni)} fő";
        }
    }

    public class Tanulo
    {
        public int NaploSorszam { get; set; }
        public string TorzslapSzam { get; set; } = "";
        public string Nev { get; set; } = "";
        public string SzuletesiHely { get; set; } = "";
        public DateTime SzuletesiIdo { get; set; }
        public string AnyjaNeve { get; set; } = "";
        public string Lakcim { get; set; } = "";
        public DateTime BeiratkozasIdopontja { get; set; }
        public string? Szak { get; set; }
        public string? Osztaly { get; set; }
        public bool Kollegista { get; set; }
        public string? Kollegium { get; set; }
        public bool Debreceni { get; set; }
    }
}