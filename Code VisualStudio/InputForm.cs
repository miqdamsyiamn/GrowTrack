using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json;


namespace ProjectPBO
{
    public partial class InputForm : Form
    {
        private string currentUser; // Menyimpan username pengguna yang sedang login
        private bool isNewEntry = true; // Default: bisa tambah data
        public InputForm(string username)
        {
            InitializeComponent();
            currentUser = username; // Simpan username pengguna yang login
            LoadData();
        }

        private void ResetForm()
        {
            textNamePlant.Clear();
            textPlantHeight.Clear();
            textPlantCondition.Clear();
            textWaterDemand.Clear();
            dateTimePicker.Value = DateTime.Now;
            comboBoxStatus.SelectedIndex = -1;
            isNewEntry = true; // 🔹 Pastikan kembali ke mode Tambah Data
        }


        private async void buttonSave_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"[DEBUG] isNewEntry: {isNewEntry}");
            // 🔹 Jika dalam mode tambah data, pastikan tidak memilih baris yang sudah ada
            if (isNewEntry == true)
            {
                if (dataGridView.SelectedRows.Count > 0 && dataGridView.SelectedRows[0].Index < dataGridView.Rows.Count - 1)
                {
                    MessageBox.Show("Silakan pilih baris kosong untuk menambah data baru!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Ambil data dari input form
            string plantName = textNamePlant.Text.Trim();
            string plantHeight = textPlantHeight.Text.Trim();
            string leafCondition = textPlantCondition.Text.Trim();
            string waterDemand = textWaterDemand.Text.Trim();
            string date = dateTimePicker.Value.ToString("yyyy-MM-dd");
            string status = comboBoxStatus.SelectedItem?.ToString() ?? "Belum Panen";

            // 🔹 Pastikan semua field diisi
            if (string.IsNullOrEmpty(plantName) || string.IsNullOrEmpty(plantHeight) ||
                string.IsNullOrEmpty(leafCondition) || string.IsNullOrEmpty(waterDemand))
            {
                MessageBox.Show("Semua field harus diisi!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 🔹 Validasi tinggi tanaman harus berupa angka
            if (!float.TryParse(plantHeight, out _))
            {
                MessageBox.Show("Tinggi tanaman harus berupa angka tanpa karakter tambahan!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Buat JSON data untuk dikirim ke API
            var inputData = new
            {
                username = currentUser,
                name = plantName,
                plant_height = plantHeight,
                leaf_condition = leafCondition,
                water_demand = waterDemand,
                date = date,
                status = status
            };

            string json = JsonConvert.SerializeObject(inputData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url;
                    HttpResponseMessage response;

                    if (isNewEntry) // Mode Tambah Data
                    {
                        url = "https://q6p5dmj2-5000.asse.devtunnels.ms/input";
                        response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                    }
                    else // Mode Edit Data
                    {
                        url = "https://q6p5dmj2-5000.asse.devtunnels.ms/edit";
                        response = await client.PutAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show(isNewEntry ? "Data berhasil ditambahkan!" : "Data berhasil diperbarui!",
                                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await Task.Delay(300); //
                        LoadData(); // 
                        ResetForm(); // 

                        await NotifyDashboard();
                    }
                    else
                    {
                        MessageBox.Show($"Gagal menyimpan data: {response.StatusCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            // Kosongkan semua input form
            textNamePlant.Clear();
            textPlantHeight.Clear();
            textPlantCondition.Clear();
            textWaterDemand.Clear();
            dateTimePicker.Value = DateTime.Now;
            comboBoxStatus.SelectedIndex = 0;
        }

        private async void LoadData()
        {
            try
            {
                await Task.Delay(300); // Tambahkan delay untuk memastikan data sudah tersedia di backend

                int retryCount = 3; //  Coba ulang maksimal 3 kali jika data belum diperbarui
                while (retryCount > 0)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string url = $"https://q6p5dmj2-5000.asse.devtunnels.ms/input?username={currentUser}";
                        HttpResponseMessage response = await client.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            var data = JsonConvert.DeserializeObject<List<dynamic>>(responseBody);

                            if (data != null && data.Count > 0) // 🔹 Pastikan data yang diterima tidak kosong
                            {
                                await Task.Delay(100); // delay tambahan untuk stabilitas

                                DataTable dt = new DataTable();
                                dt.Columns.Add("name");
                                dt.Columns.Add("plant_height");
                                dt.Columns.Add("leaf_condition");
                                dt.Columns.Add("water_demand");
                                dt.Columns.Add("date");
                                dt.Columns.Add("status"); // Tambahkan kolom status ke DataGridView

                                foreach (var item in data)
                                {
                                    DataRow row = dt.NewRow();
                                    row["name"] = item.name;
                                    row["plant_height"] = item.plant_height;
                                    row["leaf_condition"] = item.leaf_condition;
                                    row["water_demand"] = item.water_demand;
                                    row["date"] = item.date;
                                    row["status"] = item.status ?? "Belum Panen"; // Jika status null, default "Belum Panen"
                                    dt.Rows.Add(row);
                                }

                                dataGridView.DataSource = dt;
                                break; // 🔹 Berhenti jika data berhasil dimuat
                            }
                        }

                        retryCount--;
                        if (retryCount > 0)
                        {
                            await Task.Delay(300); // 🔹 Tambahkan delay sebelum mencoba lagi
                        }
                    }
                }

                if (retryCount == 0)
                {
                    MessageBox.Show("Gagal memuat data terbaru, silakan coba lagi.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // 🔹 Pastikan mode kembali ke tambah data
                isNewEntry = true;
                buttonSave.Click -= buttonSave_Click;
                buttonSave.Click += buttonSave_Click;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task NotifyDashboard()
        {
            // Cek apakah instance Dashboard sudah ada
            Dashboard dashboardForm = Application.OpenForms["Dashboard"] as Dashboard;
            if (dashboardForm != null)
            {
                await dashboardForm.CheckFirstInput(); // Perbarui data Dashboard
                dashboardForm.LoadPlantTypes(); // Perbarui ComboBox jika tanaman baru ditambahkan
            }
        }
        private void buttonDashboard_Click(object sender, EventArgs e)
        {
            // Kembali ke Dashboard
            Dashboard dashboardForm = Application.OpenForms["Dashboard"] as Dashboard;
            if (dashboardForm != null)
            {
                dashboardForm.Show();
            }
            else
            {
                Dashboard newDashboard = new Dashboard(currentUser);
                newDashboard.Show();
            }

            this.Hide();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private async void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 1)
            {
                DataGridViewRow row = dataGridView.SelectedRows[0];

                if (row.Index < dataGridView.Rows.Count - 1) // Pastikan bukan baris terakhir (kosong)
                {
                    textNamePlant.Text = row.Cells["name"].Value?.ToString() ?? "";
                    textPlantHeight.Text = row.Cells["plant_height"].Value?.ToString() ?? "0";
                    textPlantCondition.Text = row.Cells["leaf_condition"].Value?.ToString() ?? "";
                    textWaterDemand.Text = row.Cells["water_demand"].Value?.ToString() ?? "";

                    if (DateTime.TryParse(row.Cells["date"].Value?.ToString(), out DateTime parsedDate))
                    {
                        dateTimePicker.Value = parsedDate;
                    }
                    else
                    {
                        dateTimePicker.Value = DateTime.Now;
                    }

                    comboBoxStatus.SelectedItem = row.Cells["status"].Value?.ToString() ?? "Belum Panen";

                    isNewEntry = false; // ✅ Mode Edit Aktif
                }
            }
        }

        private async void buttonEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 0 || dataGridView.SelectedRows[0].Index >= dataGridView.Rows.Count - 1)
            {
                MessageBox.Show("Pilih satu baris untuk diedit!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataGridViewRow row = dataGridView.SelectedRows[0];

            string originalName = row.Cells["name"].Value?.ToString() ?? "";
            string originalDate = row.Cells["date"].Value?.ToString() ?? "";
            string originalPlantHeight = row.Cells["plant_height"].Value?.ToString() ?? "0";

            textNamePlant.Text = originalName;
            textPlantHeight.Text = originalPlantHeight;
            textPlantCondition.Text = row.Cells["leaf_condition"].Value?.ToString() ?? "";
            textWaterDemand.Text = row.Cells["water_demand"].Value?.ToString() ?? "";

            if (DateTime.TryParse(originalDate, out DateTime parsedDate))
            {
                dateTimePicker.Value = parsedDate;
            }
            else
            {
                dateTimePicker.Value = DateTime.Now;
            }

            comboBoxStatus.SelectedItem = row.Cells["status"].Value?.ToString() ?? "Belum Panen";
            isNewEntry = false;

            MessageBox.Show("Silakan edit data di form, lalu tekan tombol 'Save' untuk menyimpan perubahan.", "Edit Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);

            buttonSave.Click -= buttonSave_Click;
            buttonSave.Click += async (s, ev) => await SaveEdit(originalName, originalDate, originalPlantHeight);
        }


        private async Task SaveEdit(string originalName, string originalDate, string originalPlantHeight)
        {
            string plantName = textNamePlant.Text.Trim();
            string plantHeightText = textPlantHeight.Text.Trim();
            string leafCondition = textPlantCondition.Text.Trim();
            string waterDemand = textWaterDemand.Text.Trim();
            string date = dateTimePicker.Value.ToString("yyyy-MM-dd");
            string status = comboBoxStatus.SelectedItem?.ToString() ?? "Belum Panen";

            if (string.IsNullOrEmpty(plantName) || string.IsNullOrEmpty(plantHeightText) ||
                string.IsNullOrEmpty(leafCondition) || string.IsNullOrEmpty(waterDemand))
            {
                MessageBox.Show("Semua field harus diisi!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!float.TryParse(plantHeightText, out float plantHeight))
            {
                MessageBox.Show("Tinggi tanaman harus berupa angka tanpa karakter tambahan!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var updatedData = new
            {
                username = currentUser,
                original_name = originalName,
                original_date = originalDate,
                original_plant_height = originalPlantHeight,
                name = plantName,
                plant_height = plantHeight,
                leaf_condition = leafCondition,
                water_demand = waterDemand,
                date = date,
                status = status
            };

            string json = JsonConvert.SerializeObject(updatedData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = "https://q6p5dmj2-5000.asse.devtunnels.ms/edit";
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PutAsync(url, content);

                    string responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Data berhasil diperbarui!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        await Task.Delay(500); // Delay sebelum memuat ulang data

                        LoadData();
                        ResetForm();
                    }
                    else
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            MessageBox.Show("Gagal memperbarui data: Data tidak ditemukan di server.\nPastikan data yang diedit masih tersedia.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show($"Gagal memperbarui data: {response.StatusCode}\n{responseText}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Kembalikan event handler tombol Save ke mode tambah data
            isNewEntry = true;
            buttonSave.Click -= buttonSave_Click;
            buttonSave.Click += buttonSave_Click;
        }






        private async void buttonDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 0 || dataGridView.SelectedRows[0].Index >= dataGridView.Rows.Count - 1)
            {
                MessageBox.Show("Pilih satu baris untuk dihapus!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataGridViewRow row = dataGridView.SelectedRows[0];
            string plantName = row.Cells["name"].Value.ToString();
            string plantDate = row.Cells["date"].Value.ToString();

            DialogResult dialogResult = MessageBox.Show($"Apakah Anda yakin ingin menghapus data tanaman '{plantName}' pada tanggal {plantDate}?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.No)
            {
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://q6p5dmj2-5000.asse.devtunnels.ms/delete?username={currentUser}&date={plantDate}"; // 🔄 URL yang diperbarui
                    HttpResponseMessage response = await client.DeleteAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Data berhasil dihapus!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show($"Gagal menghapus data: {response.StatusCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonReport_Click(object sender, EventArgs e)
        {
            ReportForm reportForm = new ReportForm(currentUser);
            reportForm.Show(); // Menampilkan form Report
            this.Hide();
        }
    }
}
