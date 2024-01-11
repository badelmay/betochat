using System;
using System.Net;
using System.Net.Mail;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using System.Data.SqlClient;
using Microsoft.VisualBasic;
using System.Security.Cryptography;

namespace chat
{
    public partial class Kayit_Ekrani : Form
    {
        private MySqlConnection connection;
        private MySqlCommand command;
        private string connectionString = "";

        public Kayit_Ekrani()
        {
            InitializeComponent();
            connection = new MySqlConnection(connectionString);
        }

        private void label8_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void bunifuMaterialTextbox1_OnValueChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = bunifuMaterialTextbox1.Text;
            string password = bunifuMaterialTextbox2.Text;
            string confirmPassword = bunifuMaterialTextbox3.Text;
            string email = bunifuMaterialTextbox4.Text;

            if (password != confirmPassword)
            {
                MessageBox.Show("Şifreler uyuşmuyor!");
                return;
            }

            // Kontrol ekleniyor: Aynı kullanıcı adı veya e-posta var mı?
            if (IsUsernameOrEmailAlreadyExists(username, email))
            {
                MessageBox.Show("Bu kullanıcı adı veya e-posta zaten kullanımda!");
                return;
            }

            int verificationCode = GenerateVerificationCode();
            SendVerificationCode(email, verificationCode.ToString());

            string code = Microsoft.VisualBasic.Interaction.InputBox("Doğrulama kodunu girin:", "Doğrulama Kodu", "");

            if (code != verificationCode.ToString())
            {
                MessageBox.Show("Geçersiz doğrulama kodu!");
                return;
            }

            SaveToDatabase(username, password, email);
            MessageBox.Show("Üyelik başarıyla oluşturuldu!");
        }

        private bool IsUsernameOrEmailAlreadyExists(string username, string email)
        {
            string query = "SELECT COUNT(*) FROM users WHERE username = @username OR email = @email";
            command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);

            try
            {
                connection.Open();
                int count = Convert.ToInt32(command.ExecuteScalar());

                // Eğer count 0'dan büyükse, kullanıcı adı veya e-posta zaten var demektir.
                return count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return true; // Hata durumunda var sayalım.
            }
            finally
            {
                connection.Close();
            }
        }


        private int GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(1000, 9999);
        }

        private void SendVerificationCode(string email, string code)
        {
            try
            {
                SmtpClient smtpClient = new SmtpClient("");
                smtpClient.Port = 587;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential("");
                smtpClient.EnableSsl = true;

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("");
                mailMessage.To.Add(email);

                mailMessage.Subject = "Onay Kodu";
                mailMessage.Body = $"Onay kodunuz: {code}";

                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"E-posta gönderilirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void SaveToDatabase(string username, string password, string email)
        {
            string hashedPassword = HashPassword(password);

            string query = "INSERT INTO users (username, password, email) VALUES (@username, @password, @email)";
            command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", hashedPassword);
            command.Parameters.AddWithValue("@email", email);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Giris_Ekrani kayitEkrani = new Giris_Ekrani();
            kayitEkrani.Show();
            this.Hide();
        }
    }
}
