using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;

namespace betochat
{
    public partial class Giris_Ekrani : Form
    {
        private string connectionString = "";

        public Giris_Ekrani()
        {
            InitializeComponent();
        }

        private void label8_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = bunifuMaterialTextbox1.Text;
            string password = bunifuMaterialTextbox2.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Lütfen kullanıcı adı ve şifre giriniz.");
                return;
            }

            string query = "SELECT password FROM users WHERE username = @username";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    try
                    {
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string hashedPasswordFromDB = reader["password"].ToString();
                                if (VerifyHashedPassword(password, hashedPasswordFromDB))
                                {
                                    MessageBox.Show("Giriş başarılı!");
                                    Sohbet_Ekrani sohbetEkrani = new Sohbet_Ekrani(username);
                                    sohbetEkrani.Show();
                                    this.Hide();
                                }
                                else
                                {
                                    MessageBox.Show("Kullanıcı adı veya şifre uyuşmuyor!");
                                }
                            }
                            else
                            {
                                MessageBox.Show("Kullanıcı bulunamadı!");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private bool VerifyHashedPassword(string inputPassword, string hashedPassword)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inputPassword));
                string hashedInputPassword = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

                return hashedInputPassword.Equals(hashedPassword);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // Kodunuzun devamı...
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Kayit_Ekrani kayitEkrani = new Kayit_Ekrani();
            kayitEkrani.Show();
            this.Hide();
        }

        private void label9_Click(object sender, EventArgs e)
        {
            // Kodunuzun devamı...
        }
    }
}
