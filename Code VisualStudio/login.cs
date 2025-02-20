using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace ProjectPBO
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }


        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private async void buttonLogin_Click(object sender, EventArgs e)
        {
            // Ambil data dari TextBox
            string username = textUsername.Text.Trim();
            string password = textPassword.Text.Trim();

            // Validasi input
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Username dan password tidak boleh kosong!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Membuat data JSON untuk dikirim ke API
            var loginData = new
            {
                username = username,
                password = password
            };

            string json = JsonConvert.SerializeObject(loginData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // URL API Flask
                    string url = "https://q6p5dmj2-5000.asse.devtunnels.ms/login";

                    // Kirim data ke API dengan metode POST
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic result = JsonConvert.DeserializeObject(responseBody);

                        // Ambil username dari response API
                        string loggedInUser = result.user.username;

                        // Tampilkan pesan sukses
                        MessageBox.Show("Login berhasil! Selamat datang " + result.user.name, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Buka Dashboard dengan username yang sedang login
                        Dashboard dashboardForm = new Dashboard(loggedInUser);
                        dashboardForm.Show();


                        // Tutup form Login
                        this.Hide();
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        MessageBox.Show("Username atau password salah!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Gagal: {response.StatusCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkSignUp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Register regisForm = new Register();
            regisForm.Show();
            this.Hide();
        }
    }

}
