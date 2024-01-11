using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Agreement.JPake;
using System.Net;
using System.Text.RegularExpressions;



namespace betochat
{
    public partial class Sohbet_Ekrani : Form
    {
        public string currentUser;
        private DateTime lastMessageTimestamp = DateTime.MinValue;
        private System.Threading.Timer refreshTimer;
        private List<string> receivedMessages = new List<string>();
        private bool userLoggedOut = false;
        // Son gönderilen mesaj
        private string lastSentMessage = "";
        // Gönderilen mesajların zaman damgası ve içeriğini takip etmek için liste
        private List<Tuple<DateTime, string>> sentMessagesList = new List<Tuple<DateTime, string>>();


        public Sohbet_Ekrani(string username)
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // Kullanıcı adını tanımla
            currentUser = username;
            label1.Text = currentUser;

            // Durum ComboBox'ını doldur
            comboBox1.Items.Add("Online");
            comboBox1.Items.Add("Offline");

            // Kullanıcı oturumu kapatılmamışsa
            if (!userLoggedOut)
            {
                // Mesajları güncellemek için bir zamanlayıcı oluştur
                refreshTimer = new System.Threading.Timer(RefreshMessages, null, 0, 3000);

                // Kullanıcı adlarını güncelle
                UpdateUsernamesListBox();

                // Arkadaşlık isteklerini kontrol et
                CheckFriendRequests();

                // Profil resmini yükle
                LoadProfileImage();

                // Kullanıcının durumunu al ve belirtilen durumu ayarla
                string userStatus = GetUserStatus();
                if (userStatus == "Online" || userStatus == "Offline")
                {
                    comboBox1.SelectedItem = userStatus;
                }
            }
        }
        private void SendMessageButton_Click(object sender, EventArgs e)
        {
            // Mesaj metnini al
            string messageText = MessageTextBox.Text;

            // Aynı mesajın daha önce gönderilip gönderilmediğini kontrol et
            if (IsDuplicateMessage(messageText))
            {
                MessageBox.Show("Bu mesajı zaten gönderdiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Eğer bir alıcı seçilmişse
            if (usernamesListBox.SelectedIndex != -1)
            {
                // Alıcı kullanıcı adını al
                string receiverUsername = usernamesListBox.SelectedItem.ToString();

                // Gönderilen mesajın zaman damgası ve içeriğini listeye ekle
                DateTime messageTimestamp = DateTime.Now;
                sentMessagesList.Add(new Tuple<DateTime, string>(messageTimestamp, messageText));

                // Mesajı kaydet ve göster
                SaveMessage(currentUser, receiverUsername, messageText);

                // Mesajı göster, giden mesaj olduğunu belirt
                ShowMessage(currentUser, messageText, messageTimestamp, false);

                // Gönderilen mesajı temizle
                MessageTextBox.Text = "";
            }
            else
            {
                // Alıcı seçilmediği durumda uyarı ver
                MessageBox.Show("Lütfen bir alıcı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private bool IsDuplicateMessage(string messageText)
        {
            // Gönderilen mesajların içeriğini kontrol et
            foreach (var sentMessage in sentMessagesList)
            {
                if (sentMessage.Item2 == messageText)
                {
                    // Aynı içeriğe sahip bir mesaj daha önce gönderilmiş
                    return true;
                }
            }

            // Aynı içeriğe sahip mesaj bulunamadı
            return false;
        }


        private void SaveMessage(string senderUsername, string receiverUsername, string messageText)
        {
            // Veritabanı bağlantı bilgileri
            string connectionString = "";
            // Veritabanına eklenecek mesajı eklemek için sorgu
            string query = "INSERT INTO messages (sender_id, receiver_id, message_text, sender_username, receiver_username) " +
                            "SELECT u1.id AS sender_id, u2.id AS receiver_id, @messageText AS message_text, @senderUsername, @receiverUsername " +
                            "FROM users u1 " +
                            "JOIN users u2 ON u2.username = @receiverUsername " +
                            "WHERE u1.username = @senderUsername";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        connection.Open();
                        // Mesajı şifrele
                        string encryptedMessage = EncryptMessage(messageText);

                        // Parametreleri ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@messageText", encryptedMessage);
                        command.Parameters.AddWithValue("@senderUsername", senderUsername);
                        command.Parameters.AddWithValue("@receiverUsername", receiverUsername);

                        command.ExecuteNonQuery();
                    }
                    catch (MySqlException ex)
                    {
                        // Hata durumunda kullanıcıya bilgi ver
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private void ShowMessage(string senderUsername, string decryptedMessage, DateTime messageTimestamp, bool isIncoming)
        {
            if (!decryptedMessage.StartsWith("HATA:"))
            {
                string formattedMessage = $"{messageTimestamp.ToString("yyyy-MM-dd HH:mm:ss")} ";

                ChatDisplayTextBox.Invoke(new Action(() =>
                {
                    // Ana iş parçacığına Invoke ile çağrı yap
                    ChatDisplayTextBox.SelectionStart = ChatDisplayTextBox.TextLength;
                    ChatDisplayTextBox.SelectionLength = 0;

                    if (isIncoming)
                    {
                        formattedMessage += $"{senderUsername}: {decryptedMessage}";

                        // Eğer alınan mesajın kullanıcı tarafından okunup okunmadığını kontrol et
                        if (senderUsername != currentUser)
                        {
                            // Gelen mesaj bildirimi
                            if (!IsFormFocused()) // Eğer form odaklı değilse
                            {
                                ShowMessageBox($"Yeni Mesaj - {senderUsername}", decryptedMessage);
                            }
                        }
                    }
                    else
                    {
                        formattedMessage += $"{currentUser}: {decryptedMessage}";
                    }

                    ChatDisplayTextBox.AppendText(formattedMessage + Environment.NewLine);
                }));
            }
            else
            {
                MessageBox.Show(decryptedMessage);
            }
        }

        private bool IsFormFocused()
        {
            // Formun odaklı olup olmadığını kontrol et
            return this.Focused || this.ActiveControl == ChatDisplayTextBox;
        }

        private void ShowMessageBox(string title, string message)
        {
            // MessageBox üzerinden bildirim gösterme işlemleri
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string EncryptMessage(string message)
        {
            // Aes algoritması kullanılarak mesajı şifrele
            using (Aes aesAlg = Aes.Create())
            {
                // Rastgele anahtar ve IV oluştur
                aesAlg.GenerateKey();
                aesAlg.GenerateIV();
                // Mesajı byte dizisine çevir
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(message);
                // Şifreleme işlemcisini oluştur
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                // Şifreleme işlemi uygula
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);
                // Şifrelenmiş mesajı Base64 formatına çevir
                string encryptedMessage = Convert.ToBase64String(encryptedBytes);
                // Anahtar ve IV'yi Base64 formatına çevir
                string aesKeyBase64 = Convert.ToBase64String(aesAlg.Key);
                string aesIVBase64 = Convert.ToBase64String(aesAlg.IV);
                // Tüm bilgileri birleştirerek döndür
                string fullEncryptedMessage = encryptedMessage + "|" + aesKeyBase64 + "|" + aesIVBase64;
                return fullEncryptedMessage;
            }
        }

        private string DecryptMessage(string encryptedMessage)
        {
            try
            {
                // Şifreli mesajı parçalara ayır
                string[] parts = encryptedMessage.Split('|');
                // Parça sayısını kontrol et
                if (parts.Length != 3)
                {
                    MessageBox.Show("Geçersiz şifreli mesaj formatı. Parça sayısı: " + parts.Length);
                    return "HATA: Mesaj çözülemedi.";
                }
                // Parçalardan gerekli bilgileri al
                string encryptedText = parts[0];
                string aesKeyBase64 = parts[1];
                string aesIVBase64 = parts[2];
                // Base64 formatındaki bilgileri byte dizilerine çevir
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] aesKey = Convert.FromBase64String(aesKeyBase64);
                byte[] aesIV = Convert.FromBase64String(aesIVBase64);
                // Aes algoritması kullanarak mesajı çöz
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = aesKey;
                    aesAlg.IV = aesIV;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                    // Byte dizisini stringe çevirerek döndür
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda kullanıcıya bilgi ver
                MessageBox.Show("Mesaj çözülürken bir hata oluştu: " + ex.Message);
                return "HATA: Mesaj çözülemedi.";
            }
        }

        public void UpdateUsernamesListBox()
        {
            // Veritabanı bağlantı bilgileri
            string connectionString = "";

            // Kullanıcının arkadaş listesini almak için sorgu
            string query = "SELECT DISTINCT friend_username " +
                            "FROM user_friend_list " +
                            "WHERE username = @currentUser";

            // MySQL bağlantısını oluştur
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // MySQL komutunu oluştur
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Bağlantıyı aç
                        connection.Open();
                        // Parametreyi ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@currentUser", currentUser);
                        // Okuyucuyu oluştur
                        MySqlDataReader reader = command.ExecuteReader();
                        // ListBox'ı temizle
                        usernamesListBox.Items.Clear();
                        // Okuma işlemi
                        while (reader.Read())
                        {
                            // Arkadaşın kullanıcı adını al
                            string friendUsername = reader.GetString("friend_username");
                            // ListBox'a ekle
                            usernamesListBox.Items.Add(friendUsername);
                        }

                        // Okuyucuyu kapat
                        reader.Close();
                    }
                    catch (MySqlException ex)
                    {
                        // Hata durumunda kullanıcıya bilgi ver
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        // Mesaj gönderme durumu kontrolü
        private bool isSendingMessage = false;
        private void RefreshMessages(object state)
        {
            // Veritabanı bağlantı bilgileri
            string connectionString = "";
            // Mesajları güncellemek için sorgu
            string query = "SELECT sender_username, receiver_username, message_text, message_timestamp FROM messages " +
                            "WHERE ((sender_username = @currentUser AND receiver_username = @selectedUser) " +
                            "OR (sender_username = @selectedUser AND receiver_username = @currentUser)) " +
                            "AND message_timestamp > @lastTimestamp";
            // Seçilen kullanıcıyı al
            string selectedUser = null;
            // Eğer bu işlem bir kontrol üzerinde çalışıyorsa Invoke kullan
            if (usernamesListBox.InvokeRequired)
            {
                usernamesListBox.Invoke(new Action(() => selectedUser = usernamesListBox.SelectedItem?.ToString()));
            }
            else
            {
                selectedUser = usernamesListBox.SelectedItem?.ToString();
            }
            // Seçilen bir kullanıcı varsa
            if (selectedUser != null)
            {
                // MySQL bağlantısını oluştur
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    // MySQL komutunu oluştur
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        try
                        {
                            // Bağlantıyı aç
                            connection.Open();
                            // Parametreleri ekleyerek sorguyu çalıştır
                            command.Parameters.AddWithValue("@currentUser", currentUser);
                            command.Parameters.AddWithValue("@selectedUser", selectedUser);
                            command.Parameters.AddWithValue("@lastTimestamp", lastMessageTimestamp);
                            // Okuyucuyu oluştur
                            MySqlDataReader reader = command.ExecuteReader();
                            // Okuma işlemi
                            while (reader.Read())
                            {
                                // Gelen verileri al
                                string senderUsername = reader.GetString("sender_username");
                                string receiverUsername = reader.GetString("receiver_username");
                                string encryptedMessage = reader.GetString("message_text");
                                DateTime messageTimestamp = reader.GetDateTime("message_timestamp");
                                // Mesajı çöz
                                string decryptedMessage = DecryptMessage(encryptedMessage);
                                // Mesajı göster
                                ShowMessage(senderUsername, decryptedMessage, messageTimestamp, senderUsername != currentUser);
                                // Son alınan mesajın zaman damgasını güncelle
                                if (messageTimestamp > lastMessageTimestamp)
                                {
                                    lastMessageTimestamp = messageTimestamp;
                                }
                            }
                            // Okuyucuyu kapat
                            reader.Close();
                        }
                        catch (MySqlException ex)
                        {
                            // Hata durumunda kullanıcıya bilgi ver
                            MessageBox.Show("Bir hata oluştu: " + ex.Message);
                        }
                    }
                }
            }
        }
        private void panelLeft_Paint(object sender, PaintEventArgs e)
        {

        }
        private void IconButton1_Click(object sender, EventArgs e)
        {
            // Kullanıcıdan aranacak kullanıcı adını al
            string searchKeyword = Microsoft.VisualBasic.Interaction.InputBox("Aranacak kullanıcı adını girin:", "Kullanıcı Arama", "");
            // Eğer kullanıcı tarafından bir değer girilmediyse uyarı ver ve işlemi iptal et
            if (string.IsNullOrWhiteSpace(searchKeyword))
            {
                MessageBox.Show("Arama iptal edildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Veritabanı bağlantı bilgileri
            string connectionString = "";
            // Kullanıcı adına göre arama sorgusu
            string query = "SELECT username FROM users WHERE username LIKE @searchUsername";
            // MySQL bağlantısını oluştur
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // MySQL komutunu oluştur
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Bağlantıyı aç
                        connection.Open();
                        // Parametreyi ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@searchUsername", "%" + searchKeyword + "%");
                        // Okuyucuyu oluştur
                        MySqlDataReader reader = command.ExecuteReader();
                        // ListBox'ı temizle
                        usernamesListBox.Items.Clear();
                        bool found = false;
                        // Kullanıcı adlarını oku
                        while (reader.Read())
                        {
                            found = true;
                            string username = reader.GetString("username");
                            usernamesListBox.Items.Add(username);
                        }
                        // Okuyucuyu kapat
                        reader.Close();
                        // Kullanıcı bulunamadıysa uyarı ver
                        if (!found)
                        {
                            MessageBox.Show("Kullanıcı bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            // Kullanıcı adları listesinde en az bir öğe varsa ilk öğeyi seç ve arkadaşlık isteği gönder
                            if (usernamesListBox.Items.Count > 0)
                            {
                                usernamesListBox.SelectedIndex = 0;
                                string selectedUsername = usernamesListBox.SelectedItem.ToString();
                                SendFriendRequest(selectedUsername);
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        // Hata durumunda kullanıcıya bilgi ver
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // ListBox'ı temizle
            usernamesListBox.Items.Clear();
            // Bilgi mesajı göster
            MessageBox.Show("Arama iptal edildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SendFriendRequest(string selectedUsername)
        {
            // Gönderen ve alıcı kullanıcı adlarını belirle
            string senderUsername = currentUser;
            string receiverUsername = selectedUsername;
            // Veritabanı bağlantı bilgileri
            string connectionString = "";
            // Arkadaşlık isteği eklemek için sorgu
            string query = "INSERT INTO friend_requests (sender_username, receiver_username) VALUES (@senderUsername, @receiverUsername)";
            // MySQL bağlantısını oluştur
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // MySQL komutunu oluştur
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Bağlantıyı aç
                        connection.Open();
                        // Parametreleri ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@senderUsername", senderUsername);
                        command.Parameters.AddWithValue("@receiverUsername", receiverUsername);
                        // Komutu çalıştır
                        command.ExecuteNonQuery();
                        // Başarılı mesajı göster
                        MessageBox.Show($"Arkadaşlık isteği '{receiverUsername}' kullanıcısına gönderildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (MySqlException ex)
                    {
                        // Hata durumunda kullanıcıya bilgi ver
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private void ShowUserFriends()
        {
            // Veritabanı bağlantı bilgileri
            string connectionString = "";
            // Arkadaş listesini almak için sorgu
            string query = "SELECT receiver_username FROM friend_requests WHERE sender_username = @currentUser";
            // MySQL bağlantısını oluştur
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // MySQL komutunu oluştur
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Bağlantıyı aç
                        connection.Open();
                        // Parametreyi ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@currentUser", currentUser);
                        // Okuyucuyu oluştur
                        MySqlDataReader reader = command.ExecuteReader();
                        // Arkadaş listesi için bir liste oluştur
                        List<string> friendList = new List<string>();
                        // Kullanıcıların arkadaşlarını oku
                        while (reader.Read())
                        {
                            string friendUsername = reader.GetString("receiver_username");
                            friendList.Add(friendUsername);
                        }
                        // Okuyucuyu kapat
                        reader.Close();
                        // Arkadaş listesini göster
                        MessageBox.Show($"Eklediğiniz Arkadaşlar:\n{string.Join("\n", friendList)}", "Arkadaşlar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (MySqlException ex)
                    {
                        // Hata durumunda kullanıcıya bilgi ver
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private void AcceptFriendRequest(string requesterUsername)
        {
            // Veritabanı bağlantı bilgileri
            string connectionString = "";
            // Arkadaşlık isteğini kabul etmek için sorgu
            string acceptQuery = "DELETE FROM friend_requests WHERE sender_username = @requesterUsername AND receiver_username = @currentUser;" +
                                 "INSERT INTO user_friend_list (username, friend_username) VALUES (@currentUser, @requesterUsername);" +
                                 "INSERT INTO user_friend_list (username, friend_username) VALUES (@requesterUsername, @currentUser)";

            // MySQL bağlantısını oluştur
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // MySQL komutunu oluştur
                using (MySqlCommand command = new MySqlCommand(acceptQuery, connection))
                {
                    try
                    {
                        // Bağlantıyı aç
                        connection.Open();
                        // Parametreleri ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@requesterUsername", requesterUsername);
                        command.Parameters.AddWithValue("@currentUser", currentUser);
                        // Komutu çalıştır ve etkilenen satır sayısını al
                        int rowsAffected = command.ExecuteNonQuery();
                        // Eğer işlem başarılıysa
                        if (rowsAffected > 0)
                        {
                            // Başarılı mesajı göster
                            MessageBox.Show($"Arkadaşlık isteği kabul edildi: {requesterUsername}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Kullanıcı adlarını güncelle
                            UpdateUsernamesListBox();
                        }
                        else
                        {
                            // Hata durumunda kullanıcıya bilgi ver
                            MessageBox.Show($"Arkadaşlık isteği kabul edilirken bir hata oluştu: {requesterUsername}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (MySqlException ex)
                    {
                        // Hata durumunda kullanıcıya bilgi ver
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private void CheckFriendRequests()
        {
            // Veritabanı bağlantı bilgileri
            string connectionString = "";
            // Arkadaşlık isteklerini kontrol etmek için sorgu
            string query = "SELECT sender_username FROM friend_requests WHERE receiver_username = @currentUser";
            // MySQL bağlantısını oluştur
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // MySQL komutunu oluştur
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Bağlantıyı aç
                        connection.Open();
                        // Parametreyi ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@currentUser", currentUser);
                        // Okuyucuyu oluştur
                        MySqlDataReader reader = command.ExecuteReader();
                        // Bekleyen arkadaşlık istekleri için bir liste oluştur
                        List<string> pendingRequests = new List<string>();
                        // Okuma işlemi
                        while (reader.Read())
                        {
                            string senderUsername = reader.GetString("sender_username");
                            pendingRequests.Add(senderUsername);
                        }
                        // Okuyucuyu kapat
                        reader.Close();
                        // Eğer bekleyen istek varsa
                        if (pendingRequests.Count > 0)
                        {
                            // İstek yapan kullanıcıları birleştir ve kullanıcıdan onay iste
                            string requesters = string.Join("\n", pendingRequests);
                            DialogResult result = MessageBox.Show($"Aşağıdaki kullanıcılardan arkadaşlık isteği var:\n{requesters}\n\nKabul etmek istiyor musunuz?", "Arkadaşlık İsteği", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            // Kullanıcı onay verirse
                            if (result == DialogResult.Yes)
                            {
                                // Her bir isteği kabul et
                                foreach (var requester in pendingRequests)
                                {
                                    AcceptFriendRequest(requester);
                                }
                                // Arkadaş listesini göster
                                ShowUserFriends();
                            }
                        }
                        else
                        {
                            // Bekleyen arkadaşlık isteği yoksa bilgi mesajı ver
                            MessageBox.Show("Bekleyen arkadaşlık isteği yok.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (MySqlException ex)
                    {
                        // Hata durumunda kullanıcıya bilgi ver
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private void MessageTextBox_TextChanged_1(object sender, EventArgs e)
        {

        }
        private void LoadProfileImage()
        {
            try
            {
                // Veritabanı bağlantı bilgileri
                string connectionString = "";
                // Kullanıcının profil resmini almak için sorgu
                string query = "SELECT ProfileImage FROM users WHERE username = @username";
                // MySQL bağlantısını oluştur
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    // MySQL komutunu oluştur
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Parametreyi ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@username", currentUser);
                        // Bağlantıyı aç
                        connection.Open();
                        // Sorgudan sonuç al
                        object result = command.ExecuteScalar();
                        // Sonuç boş değilse ve DBNull.Value değilse
                        if (result != null && result != DBNull.Value)
                        {
                            // Byte dizisini al
                            byte[] imageBytes = (byte[])result;

                            // Byte dizisi uzunluğu 0'dan büyükse
                            if (imageBytes.Length > 0)
                            {
                                // Byte dizisini kullanarak MemoryStream oluştur ve PictureBox'a resmi yükle
                                using (MemoryStream ms = new MemoryStream(imageBytes))
                                {
                                    pictureBox1.Image = new Bitmap(ms);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda kullanıcıya bilgi ver
                MessageBox.Show("Profil resmi yüklenirken bir hata oluştu: " + ex.Message);
            }
        }


        private void ıconButton2_Click(object sender, EventArgs e)
        {
            // Resim dosyası seçim penceresi oluştur
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            // Sadece belirtilen uzantılardaki dosyaları göster
            openFileDialog1.Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
            // Kullanıcı dosya seçtiyse
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Veritabanı bağlantı bilgileri
                    string connectionString = "";
                    // MySQL bağlantısını oluştur
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        // MySQL komutunu oluştur
                        using (MySqlCommand command = new MySqlCommand())
                        {
                            // Komutun bağlantısını belirle
                            command.Connection = connection;
                            // Bağlantıyı aç
                            connection.Open();
                            // Seçilen dosyanın yolunu al
                            string filePath = openFileDialog1.FileName;
                            // Bitmap olarak seçilen resmi yükle
                            Bitmap selectedImage = new Bitmap(filePath);
                            // Bitmap'i MemoryStream'e çevir
                            using (MemoryStream ms = new MemoryStream())
                            {
                                selectedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                byte[] imageBytes = ms.ToArray();
                                // Kullanıcıya ait profil resmini güncellemek için sorgu
                                string updateQuery = "UPDATE users SET ProfileImage = @profileImage WHERE username = @username";
                                // Komutun sorgusunu belirle
                                command.CommandText = updateQuery;
                                // Parametreleri ekleyerek sorguyu çalıştır
                                command.Parameters.AddWithValue("@username", currentUser);
                                // Eğer resim varsa ve boyutu 0'dan büyükse
                                if (imageBytes != null && imageBytes.Length > 0)
                                {
                                    // Parametre olarak resim byte dizisini ekle
                                    command.Parameters.Add("@profileImage", MySqlDbType.Blob).Value = imageBytes;
                                }
                                else
                                {
                                    // Resim yoksa veya boyutu 0 ise parametreyi boş değer olarak ekle
                                    command.Parameters.Add("@profileImage", MySqlDbType.Blob).Value = DBNull.Value;
                                }
                                // Sorguyu çalıştır ve etkilenen satır sayısını al
                                int rowsAffected = command.ExecuteNonQuery();
                                // Eğer işlem başarılıysa
                                if (rowsAffected > 0)
                                {
                                    // Kullanıcıya bilgi mesajı göster
                                    MessageBox.Show("Profil resmi başarıyla güncellendi.");
                                    // PictureBox'a yeni resmi yükle
                                    pictureBox1.Image = selectedImage;
                                    // Resmi PictureBox'a uygun şekilde ölçekle
                                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                                }
                                else
                                {
                                    // Hata durumunda kullanıcıya bilgi ver
                                    MessageBox.Show("Profil resmi güncellenirken bir hata oluştu.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Hata durumunda kullanıcıya bilgi ver
                    MessageBox.Show("Bir hata oluştu: " + ex.Message);
                }
            }
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            ShowEmojiSelector();
        }

        private void ShowEmojiSelector()
        {
            EmojiSelectorForm emojiSelector = new EmojiSelectorForm();
            emojiSelector.EmojiSelected += EmojiSelector_EmojiSelected;
            emojiSelector.ShowDialog();
        }

        private void EmojiSelector_EmojiSelected(object sender, string selectedEmoji)
        {
            MessageTextBox.Text += selectedEmoji;
        }

        private void ıconButton3_Click(object sender, EventArgs e)
        {
            Ayarlar_Form settingsForm = new Ayarlar_Form();
            settingsForm.ShowDialog();
        }

        private void panelRight_Paint(object sender, PaintEventArgs e)
        {

        }

        private string previousStatus = "";
        internal string senderUsername;

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ComboBox'tan seçilen durumu al
            string selectedStatus = comboBox1.SelectedItem.ToString();
            // Seçilen durum "Online" veya "Offline" ise
            if (selectedStatus == "Online" || selectedStatus == "Offline")
            {
                // Seçilen durum önceki durumdan farklı ise
                if (selectedStatus != previousStatus)
                {
                    // Kullanıcı durumunu güncelle
                    UpdateUserStatus(selectedStatus);
                    // Önceki durumu güncelle
                    previousStatus = selectedStatus;
                }
            }
            else
            {
                // Geçersiz durum seçimi durumunda kullanıcıya uyarı mesajı göster
                MessageBox.Show("Geçersiz durum seçimi.");
            }
        }
        private void UpdateUserStatus(string status)
        {
            // Veritabanı bağlantı bilgileri
            string connectionString = "" +
                "";
            // Kullanıcının durumunu güncellemek için sorgu
            string query = "UPDATE users SET online_status = @status WHERE username = @currentUser";

            // MySQL bağlantısını oluştur
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // MySQL komutunu oluştur
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Bağlantıyı aç
                        connection.Open();
                        // Parametreleri ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@currentUser", currentUser);
                        // Sorguyu çalıştır ve etkilenen satır sayısını al
                        int rowsAffected = command.ExecuteNonQuery();
                        // Eğer işlem başarılıysa
                        if (rowsAffected > 0)
                        {
                            // Kullanıcıya bilgi mesajı göster
                            MessageBox.Show("Durum başarıyla güncellendi.");

                            // Online kullanıcıları kontrol et ve bildirim göster
                            CheckOnlineUsers();
                        }
                        else
                        {
                            // Durum güncellenirken bir hata oluştu, satır etkilenmedi
                            MessageBox.Show("Durum güncellenirken bir hata oluştu. Satır etkilenmedi.");
                        }
                    }
                    catch (MySqlException ex)
                    {
                        // Hata durumunda kullanıcıya bilgi ver
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private void CheckOnlineUsers()
        {
            // Veritabanı bağlantı bilgileri
            string connectionString = "";

            // Online olan arkadaşları kontrol etmek için sorgu
            string query = "SELECT uf.friend_username, u.online_status " +
                           "FROM user_friend_list uf " +
                           "JOIN users u ON uf.friend_username = u.username " +
                           "WHERE uf.username = @currentUser AND u.online_status = 'Online'";

            // MySQL bağlantısını oluştur
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // MySQL komutunu oluştur
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        connection.Open();

                        // Parametreyi ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@currentUser", currentUser);

                        // Kullanıcıları kontrol et
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                // Online olan arkadaşlar varsa
                                StringBuilder messageBuilder = new StringBuilder("Online olan arkadaşlarınız: ");

                                while (reader.Read())
                                {
                                    string friendUsername = reader["friend_username"].ToString();
                                    messageBuilder.Append(friendUsername).Append(", ");
                                }

                                // Son virgülü ve boşluğu kaldır
                                string message = messageBuilder.ToString().TrimEnd(',', ' ');

                                // Bildirim göster
                                MessageBox.Show(message + ". Sohbet etmek ister misiniz?");
                            }
                            else
                            {
                                // Online arkadaş bulunamadı
                                MessageBox.Show("Online arkadaşınız bulunmamaktadır.");
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private string GetUserStatus()
        {
            // Kullanıcının durumunu tutacak değişken
            string userStatus = "";
            // Veritabanı bağlantı bilgileri
            string connectionString = "";
            // Kullanıcının durumunu sorgulamak için sorgu
            string query = "SELECT online_status FROM users WHERE username = @currentUser";
            // MySQL bağlantısını oluştur
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // MySQL komutunu oluştur
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Bağlantıyı aç
                        connection.Open();
                        // Parametreleri ekleyerek sorguyu çalıştır
                        command.Parameters.AddWithValue("@currentUser", currentUser);
                        // Sorgudan sonuç al
                        var result = command.ExecuteScalar();
                        // Eğer sonuç boş değilse
                        if (result != null)
                        {
                            // Sonucu stringe çevir ve userStatus'a ata
                            userStatus = result.ToString();

                            // ComboBox'ta kullanıcının durumunu seçili olarak göster
                            comboBox1.SelectedItem = userStatus;
                        }
                        else
                        {
                            // Durum bulunamadı veya null durumunda kullanıcıya uyarı mesajı göster
                            MessageBox.Show("Durum bulunamadı veya null.");
                        }
                    }
                    catch (MySqlException ex)
                    {
                        // Hata durumunda kullanıcıya bilgi ver
                        MessageBox.Show("Bir hata oluştu: " + ex.Message);
                    }
                }
            }
            // Kullanıcının durumunu döndür
            return userStatus;
        }

        private void usernamesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            try
            {
                

                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Title = "Resim Seç",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    DefaultExt = "jpg",
                    Filter = "Resim dosyaları (*.jpg; *.jpeg; *.png; *.gif)|*.jpg; *.jpeg; *.png; *.gif",
                    FilterIndex = 1,
                    RestoreDirectory = true,
                    ReadOnlyChecked = true,
                    ShowReadOnly = true
                };
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string resimYolu = openFileDialog1.FileName;

                    using (WebClient client = new WebClient())
                    {
                       
                        string dosyaAdi = Path.GetFileName(resimYolu);
                     
                        

                        string resimURL = ;
                        string receiverUsername = usernamesListBox.SelectedItem?.ToString();
                        string connectionString = "";
                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = ;
                            MySqlCommand command = new MySqlCommand(query, connection);
                            command.Parameters.AddWithValue("@senderUsername", currentUser);
                            command.Parameters.AddWithValue("@imageURL", resimURL);
                            command.Parameters.AddWithValue("@messageTimestamp", DateTime.Now);
                            command.ExecuteNonQuery();
                            // Güncellendi: Kullanıcıya tıklanabilir bir bağlantıyı açma seçeneği sunma
                            MessageTextBox.Text = $"Resmi Görmek İçin URL: {resimURL}";
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Resim gönderme sırasında bir hata oluştu: " + ex.Message);
            }
        }

        private void Sohbet_Ekrani_Load(object sender, EventArgs e)
        {

        }

    }
}