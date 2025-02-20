using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ProjectPBO
{
    public partial class Dashboard : Form
    {
        private System.Windows.Forms.Timer timerWatering;
        private System.Windows.Forms.Timer timerFertilization;
        private System.Windows.Forms.Timer timerPesticide;

        //private int wateringCountdown = 14400; // 4 jam dalam detik
        //private int fertilizationCountdown = 28800; // 8 jam dalam detik
        //private int pesticideCountdown = 43200; // 12 jam dalam detik

        private int wateringCountdown = 30; // 4 jam dalam detik
        private int fertilizationCountdown = 40; // 8 jam dalam detik
        private int pesticideCountdown = 50; // 12 jam dalam detik

        private string currentUser;
        private bool hasPlantData = false; // Untuk mengecek apakah user sudah pernah input tanaman
        public Dashboard(string username)
        {
            InitializeComponent();
            currentUser = username; // Simpan username pengguna yang login

            // Inisialisasi Timer
            timerWatering = new System.Windows.Forms.Timer { Interval = 1000 }; // 1 detik
            timerWatering.Tick += TimerWatering_Tick;

            timerFertilization = new System.Windows.Forms.Timer { Interval = 1000 };
            timerFertilization.Tick += TimerFertilization_Tick;

            timerPesticide = new System.Windows.Forms.Timer { Interval = 1000 };
            timerPesticide.Tick += TimerPesticide_Tick;

            // Load daftar tanaman untuk ComboBox
            LoadPlantTypes();

            // Tampilkan semua tanaman saat pertama kali dibuka
            LoadAllPlantData();


            // Cek apakah user sudah input tanaman
            CheckFirstInput();
        }

        public async void LoadPlantTypes()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://q6p5dmj2-5000.asse.devtunnels.ms/plant_names?username={currentUser}";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var plantTypes = JsonConvert.DeserializeObject<List<string>>(responseBody);

                        comboBox.Items.Clear();
                        comboBox.Items.Add("Semua Tanaman");

                        foreach (var plant in plantTypes)
                        {
                            comboBox.Items.Add(plant);
                        }

                        if (comboBox.Items.Count > 0)
                        {
                            comboBox.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat daftar tanaman: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void LoadAllPlantData()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://q6p5dmj2-5000.asse.devtunnels.ms/input?username={currentUser}";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<List<dynamic>>(responseBody);

                        chart.Series.Clear();
                        Dictionary<string, Series> plantSeries = new Dictionary<string, Series>();
                        HashSet<DateTime> allDates = new HashSet<DateTime>();

                        // Kumpulkan semua tanggal unik terlebih dahulu
                        foreach (var item in data)
                        {
                            DateTime date = DateTime.ParseExact((string)item.date, "yyyy-MM-dd", null);
                            allDates.Add(date);
                        }

                        // Urutkan tanggal agar kontinu
                        List<DateTime> sortedDates = allDates.OrderBy(d => d).ToList();

                        foreach (var item in data)
                        {
                            string plantName = item.name;
                            double height = Convert.ToDouble(item.plant_height);
                            DateTime date = DateTime.ParseExact((string)item.date, "yyyy-MM-dd", null);

                            // Jika tanaman belum ada dalam series, buat baru
                            if (!plantSeries.ContainsKey(plantName))
                            {
                                Series newSeries = new Series(plantName)
                                {
                                    ChartType = SeriesChartType.Line,
                                    BorderWidth = 2,
                                    XValueType = ChartValueType.DateTime // Menggunakan format tanggal
                                };
                                chart.Series.Add(newSeries);
                                plantSeries[plantName] = newSeries;
                            }

                            // Tambahkan data pertumbuhan ke grafik
                            plantSeries[plantName].Points.AddXY(date, height);
                        }

                        // Tambahkan semua tanggal ke grafik agar kontinu
                        foreach (var series in plantSeries.Values)
                        {
                            foreach (var date in sortedDates)
                            {
                                if (!series.Points.Any(p => p.XValue == date.ToOADate()))
                                {
                                    series.Points.AddXY(date, double.NaN);
                                }
                            }
                        }

                        // Konfigurasi tampilan sumbu X
                        chart.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
                        chart.ChartAreas[0].AxisX.LabelStyle.Format = "dd/MM/yyyy";
                        chart.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Days;
                        chart.ChartAreas[0].AxisX.Interval = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data grafik: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void LoadPlantData(string plantName)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://q6p5dmj2-5000.asse.devtunnels.ms/plant_growth?username={currentUser}&name={plantName}";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<List<dynamic>>(responseBody);

                        chart.Series.Clear();
                        Series series = new Series(plantName)
                        {
                            ChartType = SeriesChartType.Line,
                            BorderWidth = 2,
                            XValueType = ChartValueType.DateTime
                        };
                        chart.Series.Add(series);

                        HashSet<DateTime> allDates = new HashSet<DateTime>();

                        // Kumpulkan semua tanggal unik terlebih dahulu
                        foreach (var item in data)
                        {
                            DateTime date = DateTime.ParseExact((string)item["date"], "yyyy-MM-dd", null);
                            allDates.Add(date);
                        }

                        // Urutkan tanggal agar kontinu
                        List<DateTime> sortedDates = allDates.OrderBy(d => d).ToList();

                        foreach (var item in data)
                        {
                            double height = Convert.ToDouble(item["plant_height"]);
                            DateTime date = DateTime.ParseExact((string)item["date"], "yyyy-MM-dd", null);
                            series.Points.AddXY(date, height);
                        }

                        // Tambahkan semua tanggal ke grafik agar kontinu
                        foreach (var date in sortedDates)
                        {
                            if (!series.Points.Any(p => p.XValue == date.ToOADate()))
                            {
                                series.Points.AddXY(date, double.NaN);
                            }
                        }

                        // Konfigurasi tampilan sumbu X
                        chart.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
                        chart.ChartAreas[0].AxisX.LabelStyle.Format = "dd/MM/yyyy";
                        chart.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Days;
                        chart.ChartAreas[0].AxisX.Interval = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data grafik: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        public async Task CheckFirstInput()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://q6p5dmj2-5000.asse.devtunnels.ms/check_first_input?username={currentUser}";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic result = JsonConvert.DeserializeObject(responseBody);

                        if (result.status == "exists")
                        {
                            hasPlantData = true; // User sudah pernah input tanaman
                            StartAllTimers();
                        }
                    }
                    else
                    {
                        hasPlantData = false; // Tidak ada data tanaman
                        labelWatering.Text = "Belum ada data";
                        labelFertilization.Text = "Belum ada data";
                        labelPesticide.Text = "Belum ada data";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan saat mengecek data pertama: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonInput_Click(object sender, EventArgs e)
        {
            InputForm inputForm = new InputForm(currentUser);
            inputForm.Show();
            this.Hide();
        }

        public void StartAllTimers()
        {
            if (hasPlantData)
            {
                timerWatering.Start();
                timerFertilization.Start();
                timerPesticide.Start();
            }
        }

        private void TimerWatering_Tick(object sender, EventArgs e)
        {
            if (wateringCountdown > 0)
            {
                wateringCountdown--;
                TimeSpan time = TimeSpan.FromSeconds(wateringCountdown);
                labelWatering.Text = $" {time:hh\\:mm\\:ss}";
            }
            else
            {
                timerWatering.Stop();
                MessageBox.Show("Waktunya menyiram tanaman!", "Pengingat Penyiraman", MessageBoxButtons.OK, MessageBoxIcon.Information);
                wateringCountdown = 14400; // Reset ke 4 jam
                timerWatering.Start();
            }
        }

        private void TimerFertilization_Tick(object sender, EventArgs e)
        {
            if (fertilizationCountdown > 0)
            {
                fertilizationCountdown--;
                TimeSpan time = TimeSpan.FromSeconds(fertilizationCountdown);
                labelFertilization.Text = $" {time:hh\\:mm\\:ss}";
            }
            else
            {
                timerFertilization.Stop();
                MessageBox.Show("Waktunya memberikan pupuk!", "Pengingat Pemupukan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                fertilizationCountdown = 28800; // Reset ke 8 jam
                timerFertilization.Start();
            }
        }

        private void TimerPesticide_Tick(object sender, EventArgs e)
        {
            if (pesticideCountdown > 0)
            {
                pesticideCountdown--;
                TimeSpan time = TimeSpan.FromSeconds(pesticideCountdown);
                labelPesticide.Text = $" {time:hh\\:mm\\:ss}";
            }
            else
            {
                timerPesticide.Stop();
                MessageBox.Show("Waktunya menyemprot pestisida!", "Pengingat Penyemprotan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                pesticideCountdown = 43200; // Reset ke 12 jam
                timerPesticide.Start();
            }
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {
            // Timer hanya mulai jika user sudah memiliki data tanaman
            CheckFirstInput();
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox.SelectedItem != null)
            {
                string selectedPlant = comboBox.SelectedItem.ToString();
                if (selectedPlant == "Semua Tanaman")
                {
                    LoadAllPlantData(); // Tampilkan semua tanaman
                }
                else
                {
                    LoadPlantData(selectedPlant); // Tampilkan pertumbuhan spesifik tanaman
                }
            }
        }

        private void buttonReport_Click(object sender, EventArgs e)
        {
            ReportForm reportForm = new ReportForm(currentUser);
            reportForm.Show(); // Menampilkan form Report
            this.Hide();
        }

        private void buttonLogout_Click(object sender, EventArgs e)
        {

        }

        private void labelPesticide_Click(object sender, EventArgs e)
        {

        }
    }
}
