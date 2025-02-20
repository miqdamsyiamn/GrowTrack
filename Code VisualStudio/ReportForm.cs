using System;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.Layout;
using iText.Layout.Element;
using MongoDB.Driver;


namespace ProjectPBO
{
    public partial class ReportForm : Form
    {
        private string currentUser;
        public ReportForm(string username)
        {
            InitializeComponent();
            this.currentUser = username;
            _ = LoadPlantSummary(); // ✅ Gunakan async dengan '_ ='
        }


        // Fungsi untuk mengambil daftar tanaman unik dan rentang tanggalnya
        private async Task LoadPlantSummary()
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
                        var plantNames = JsonConvert.DeserializeObject<List<string>>(responseBody);

                        DataTable dt = new DataTable();
                        dt.Columns.Add("Nama");
                        dt.Columns.Add("Tanggal");
                        dt.Columns.Add("Status");

                        foreach (var plant in plantNames)
                        {
                            string historyUrl = $"https://q6p5dmj2-5000.asse.devtunnels.ms/plant_history?username={currentUser}&name={plant}";
                            HttpResponseMessage historyResponse = await client.GetAsync(historyUrl);

                            if (historyResponse.IsSuccessStatusCode)
                            {
                                var historyData = JsonConvert.DeserializeObject<List<dynamic>>(await historyResponse.Content.ReadAsStringAsync());

                                if (historyData.Count > 0)
                                {
                                    string startDate = historyData[0]["date"];
                                    string endDate = historyData[^1]["date"]; // ✅ Gunakan indeks terakhir dengan `^1`
                                    string status = historyData[^1]["status"];

                                    dt.Rows.Add(plant, $"{startDate} - {endDate}", status);
                                }
                            }
                        }

                        if (dt.Rows.Count > 0)
                        {
                            dataGridViewReport.DataSource = dt;
                        }
                        else
                        {
                            MessageBox.Show("Tidak ada data tanaman yang tersedia.", "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Gagal memuat data tanaman: {response.StatusCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Fungsi untuk mengambil detail riwayat pertumbuhan tanaman
        private async Task<DataTable> GetPlantHistory(string plantName)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://q6p5dmj2-5000.asse.devtunnels.ms/plant_history?username={currentUser}&name={plantName}";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<List<dynamic>>(responseBody);

                        DataTable dt = new DataTable();
                        dt.Columns.Add("Nama", typeof(string));
                        dt.Columns.Add("Tinggi Tanaman", typeof(string));
                        dt.Columns.Add("Kondisi Daun", typeof(string));
                        dt.Columns.Add("Kebutuhan Air", typeof(string));
                        dt.Columns.Add("Tanggal", typeof(string));
                        dt.Columns.Add("Status", typeof(string));

                        foreach (var item in data)
                        {
                            DataRow row = dt.NewRow();
                            row["Nama"] = item.name;
                            row["Tinggi Tanaman"] = item.plant_height.ToString();
                            row["Kondisi Daun"] = item.leaf_condition;
                            row["Kebutuhan Air"] = item.water_demand;
                            row["Tanggal"] = item.date;
                            row["Status"] = item.status ?? "Belum Panen";
                            dt.Rows.Add(row);
                        }

                        return dt;
                    }
                    else
                    {
                        MessageBox.Show($"Gagal memuat data tanaman: {response.StatusCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }



        private void dataGridViewReport_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private async void buttonExport_Click(object sender, EventArgs e)
        {
            if (dataGridViewReport.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih satu tanaman untuk diekspor ke PDF!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedPlant = dataGridViewReport.SelectedRows[0].Cells["Nama"].Value.ToString();
            DataTable plantHistory = await GetPlantHistory(selectedPlant);

            if (plantHistory == null || plantHistory.Rows.Count == 0)
            {
                MessageBox.Show("Tidak ada data riwayat tanaman yang bisa diekspor!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf",
                    Title = "Save Report as PDF"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    using (PdfWriter writer = new PdfWriter(filePath))
                    using (PdfDocument pdf = new PdfDocument(writer))
                    using (Document document = new Document(pdf))
                    {
                        PdfFont font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

                        document.Add(new Paragraph($"Laporan Riwayat Tanaman: {selectedPlant}")
                            .SetFont(font)
                            .SetFontSize(14));

                        Table table = new Table(6);
                        table.AddHeaderCell("Nama");
                        table.AddHeaderCell("Tinggi Tanaman");
                        table.AddHeaderCell("Kondisi Daun");
                        table.AddHeaderCell("Kebutuhan Air");
                        table.AddHeaderCell("Tanggal");
                        table.AddHeaderCell("Status");

                        foreach (DataRow row in plantHistory.Rows)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(row["Nama"].ToString())));
                            table.AddCell(new Cell().Add(new Paragraph(row["Tinggi Tanaman"].ToString())));
                            table.AddCell(new Cell().Add(new Paragraph(row["Kondisi Daun"].ToString())));
                            table.AddCell(new Cell().Add(new Paragraph(row["Kebutuhan Air"].ToString())));
                            table.AddCell(new Cell().Add(new Paragraph(row["Tanggal"].ToString())));
                            table.AddCell(new Cell().Add(new Paragraph(row["Status"].ToString())));
                        }

                        document.Add(table);
                        MessageBox.Show("Laporan berhasil diekspor ke PDF!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal mengekspor PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonDashboard_Click(object sender, EventArgs e)
        {
            Dashboard dashboardForm = new Dashboard(currentUser);
            dashboardForm.Show(); // Menampilkan form Report
            this.Hide();
        }

        private void buttonInput_Click(object sender, EventArgs e)
        {
            InputForm inputForm = new InputForm(currentUser);
            inputForm.Show(); // Menampilkan form Report
            this.Hide();
        }
    }
}
