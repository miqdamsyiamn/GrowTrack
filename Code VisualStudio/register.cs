using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net.Http;

namespace ProjectPBO
{
    public partial class Register : Form
    {
        public Register()
        {
            InitializeComponent();
        }

        private async void buttonRegister_Click(object sender, EventArgs e)
        {
            // Ambil data dari TextBox
            string name = textName.Text.Trim();
            string username = textUsername.Text.Trim();
            string password = textPassword.Text.Trim();

            // Validasi input
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Semua field harus diisi!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Membuat data JSON
            var userData = new
            {
                name = name,
                username = username,
                password = password
            };

            string json = JsonConvert.SerializeObject(userData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // url api
                    string url = "https://q6p5dmj2-5000.asse.devtunnels.ms/users";

                    // method post ke api
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Registrasi berhasil!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        textName.Clear();
                        textUsername.Clear();
                        textPassword.Clear();
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

        private void linkSignIn_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Login loginForm = new Login();
            loginForm.Show();
            this.Hide();
        }
    }
}
