using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;

namespace betochat
{
    public partial class Sifremi_Degistir : Form
    {
        private string connectionString = "";

        public Sifremi_Degistir()
        {
            InitializeComponent();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
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

        private void button1_Click(object sender, EventArgs e)
        {
            string username = bunifuMaterialTextbox1.Text;
            string oldPassword = bunifuMaterialTextbox2.Text;
            string newPassword = bunifuMaterialTextbox3.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.");
                return;
            }

            string checkQuery = "SELECT password FROM users WHERE username = @username";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@username", username);

                    try
                    {
                        connection.Open();
                        object result = checkCommand.ExecuteScalar();

                        if (result != null)
                        {
                            string hashedPassword = result.ToString();

                            if (VerifyHashedPassword(oldPassword, hashedPassword))
                            {
                                string updateQuery = "UPDATE users SET password = @newPassword WHERE username = @username";

                                using (MySqlCommand updateCommand = new MySqlCommand(updateQuery, connection))
                                {
                                    updateCommand.Parameters.AddWithValue("@newPassword", HashPassword(newPassword));
                                    updateCommand.Parameters.AddWithValue("@username", username);

                                    updateCommand.ExecuteNonQuery();
                                    MessageBox.Show("Şifre başarıyla güncellendi!");
                                }
                            }
                            else
                            {
                                MessageBox.Show("Eski şifre yanlış!");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Kullanıcı bulunamadı!");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
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

    }
}
