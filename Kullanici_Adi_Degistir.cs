using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;

namespace betochat
{
    public partial class Kullanici_Adi_Degistir : Form
    {
        private string connectionString = "";

        public Kullanici_Adi_Degistir()
        {
            InitializeComponent();
        }

        private string ComputeHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private void label8_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = bunifuMaterialTextbox1.Text;
            string oldPassword = ComputeHash(bunifuMaterialTextbox2.Text);
            string newUsername = bunifuMaterialTextbox3.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newUsername))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.");
                return;
            }

            string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username AND password = @password";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@username", username);
                    checkCommand.Parameters.AddWithValue("@password", oldPassword);

                    try
                    {
                        connection.Open();
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (count > 0)
                        {
                            string updateQuery = "UPDATE users SET username = @newUsername WHERE username = @oldUsername";

                            using (MySqlCommand updateCommand = new MySqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@newUsername", newUsername);
                                updateCommand.Parameters.AddWithValue("@oldUsername", username);

                                updateCommand.ExecuteNonQuery();
                                MessageBox.Show("Kullanıcı adı başarıyla güncellendi!");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Kullanıcı adı veya şifre uyuşmuyor!");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Giris_Ekrani girisForm = new Giris_Ekrani();
            this.Hide();
            girisForm.Show();
        }


        private void label9_Click(object sender, EventArgs e)
        {

        }
    }
}
