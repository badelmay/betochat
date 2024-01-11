using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using MySql.Data.MySqlClient;



namespace betochat
{
    public partial class Ayarlar_Form : Form
    {
        public Ayarlar_Form()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.Manual;
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            int formWidth = this.Width;
            int formHeight = this.Height;
            int x = screenWidth - formWidth;
            int y = screenHeight - formHeight;

            this.Location = new System.Drawing.Point(x, y);
        }

        private void Ayarlar_Form_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Add("Karanlık");
            comboBox1.Items.Add("Aydınlık");

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Sohbet_Ekrani sohbetEkraniForm = Application.OpenForms.OfType<Sohbet_Ekrani>().FirstOrDefault();
            if (sohbetEkraniForm != null)
            {
                if (comboBox1.SelectedIndex == 0)
                {
                    // Karanlık seçeneği
                    sohbetEkraniForm.BackColor = System.Drawing.Color.Silver;

                    // ChatDisplayTextBox ve usernamesListBox renkleri
                    sohbetEkraniForm.ChatDisplayTextBox.BackColor = System.Drawing.Color.Silver;
                    sohbetEkraniForm.ChatDisplayTextBox.ForeColor = System.Drawing.Color.White;
                    sohbetEkraniForm.usernamesListBox.BackColor = System.Drawing.Color.Silver;
                    sohbetEkraniForm.usernamesListBox.ForeColor = System.Drawing.Color.White;
                    sohbetEkraniForm.MessageTextBox.BackColor = System.Drawing.Color.Silver;
                    sohbetEkraniForm.MessageTextBox.ForeColor = System.Drawing.Color.White; 
                }
                else if (comboBox1.SelectedIndex == 1)
                {
                    // Aydınlık seçeneği
                    sohbetEkraniForm.BackColor = System.Drawing.Color.FromName("GradientInactiveCaption");

                    // ChatDisplayTextBox ve usernamesListBox renkleri
                    sohbetEkraniForm.ChatDisplayTextBox.BackColor = System.Drawing.Color.FromName("Control"); // veya istediğiniz renk
                    sohbetEkraniForm.ChatDisplayTextBox.ForeColor = System.Drawing.Color.FromName("ControlText"); // veya istediğiniz renk
                    sohbetEkraniForm.usernamesListBox.BackColor = System.Drawing.Color.FromName("Control"); // veya istediğiniz renk
                    sohbetEkraniForm.usernamesListBox.ForeColor = System.Drawing.Color.FromName("ControlText"); // veya istediğiniz renk
                    sohbetEkraniForm.MessageTextBox.BackColor = System.Drawing.Color.FromName("Control"); // veya istediğiniz renk
                    sohbetEkraniForm.MessageTextBox.ForeColor = System.Drawing.Color.FromName("ControlText");
                }
            }
        }



        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedOption = comboBox2.SelectedItem.ToString();

            if (selectedOption == "Sesli")
            {
              
            }
          
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Kullanici_Adi_Degistir KullaniciAdiDegistirForm = new Kullanici_Adi_Degistir();
            KullaniciAdiDegistirForm.Show();

            foreach (Form form in Application.OpenForms)
            {
                if (form is Sohbet_Ekrani)
                {
                    form.Hide();
                    break;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Sifremi_Degistir SifremiDegistir = new Sifremi_Degistir();
            SifremiDegistir.Show();

            foreach (Form form in Application.OpenForms)
            {
                if (form is Sohbet_Ekrani)
                {
                    form.Hide();
                    break;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Kullanıcı adını al (örneğin oturum açmış kullanıcı adı)
            string currentUser; // Bu kısmı kendi senaryonuza göre ayarlayın

            // Sohbet_Ekrani formunu bul ve kullanıcı adını al
            Sohbet_Ekrani sohbetEkraniForm = Application.OpenForms.OfType<Sohbet_Ekrani>().FirstOrDefault();
            if (sohbetEkraniForm != null)
            {
                currentUser = sohbetEkraniForm.currentUser;
            }
            else
            {
                MessageBox.Show("Sohbet Ekranı bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ListBox üzerinde seçili olan arkadaşı al
            string selectedFriend = sohbetEkraniForm.usernamesListBox.SelectedItem as string;

            if (selectedFriend == null)
            {
                MessageBox.Show("Lütfen bir arkadaş seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Arkadaşlıktan çıkma işlemi
            string connectionString = "";
            string deleteFriendQuery = "DELETE FROM user_friend_list WHERE username = @currentUser AND friend_username = @selectedFriend";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand deleteCommand = new MySqlCommand(deleteFriendQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@currentUser", currentUser);
                    deleteCommand.Parameters.AddWithValue("@selectedFriend", selectedFriend);

                    try
                    {
                        connection.Open();
                        int rowsAffected = deleteCommand.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("ARKADAŞLIK İPTAL EDİLDİ", "Başarı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Arkadaş listesini güncelle
                            sohbetEkraniForm.UpdateUsernamesListBox();
                        }
                        else
                        {
                            MessageBox.Show("İPTAL İŞLEMİ GERÇEKLEŞTİRİLEMEDİ", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


        private void button7_Click(object sender, EventArgs e)
        {
            // Kullanıcıya temizleme işlemini onaylat
            DialogResult result = MessageBox.Show("MESAJ GEÇMİŞİNİ TEMİZLEMEK İSTEDİĞİNİZDEN EMİN MİSİNİZ?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Sohbet_Ekrani formunu bul ve kullanıcı adını al
                Sohbet_Ekrani sohbetEkraniForm = Application.OpenForms.OfType<Sohbet_Ekrani>().FirstOrDefault();
                if (sohbetEkraniForm != null)
                {
                    // Eğer Sohbet_Ekrani sınıfında currentUser adında bir özellik varsa,
                    // bu özelliği kullanabilirsiniz.
                    string senderUsername = sohbetEkraniForm.currentUser; // currentUser'yi kullanıcı adını içeren bir özellik olarak değiştirin

                    // Mesaj geçmişini temizleme işlemi
                    string connectionString = "";
                    string deleteMessagesQuery = "DELETE FROM messages WHERE sender_username = @senderUsername OR receiver_username = @receiverUsername";

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        using (MySqlCommand deleteCommand = new MySqlCommand(deleteMessagesQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@senderUsername", senderUsername);
                            deleteCommand.Parameters.AddWithValue("@receiverUsername", senderUsername);

                            try
                            {
                                connection.Open();
                                int rowsAffected = deleteCommand.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("MESAJ GEÇMİŞİ TEMİZLENDİ", "Başarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    MessageBox.Show("TEMİZLEME İŞLEMİ GERÇEKLEŞTİRİLEMEDİ", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Sohbet Ekranı bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }



    }
}
