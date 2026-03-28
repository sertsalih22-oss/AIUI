using System;
using System.Windows;
using AIUI_0._1.Data;
using AIUI_0._1.Models;

namespace AIUI_0._1
{
    public partial class AddChatWindow : Window
    {
        public AddChatWindow(string incomingUrl = "")
        {   
            InitializeComponent();
            if (!string.IsNullOrEmpty(incomingUrl))
            {
                // XAML tarafındaki URL gireceğin TextBox'ın adının 'txtUrl' olduğunu varsayıyorum. 
                // Senin projendeki ismi neyse ('UrlTextBox' vb.) burayı ona göre değiştir.
                txtUrl.Text = incomingUrl;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kullanıcının girdiği verileri alıyoruz
            string title = txtTitle.Text;
            string url = txtUrl.Text;
            string category = cmbCategory.Text;

            // 2. Boş alan kontrolü yapıyoruz
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Lütfen başlık ve URL alanlarını doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Yeni bir Chat nesnesi oluşturuyoruz
            var newChat = new Chat
            {
                Title = title,
                Url = url,
                Category = category,
                AddedDate = DateTime.Now
            };

            // 4. Veritabanına (SQLite) bağlanıp kaydediyoruz
            try
            {
                using (var db = new AppDbContext())
                {
                    db.Chats.Add(newChat); // Nesneyi tabloya ekle
                    db.SaveChanges();      // Değişiklikleri kalıcı olarak dosyaya yaz
                }

                // İşlem başarılıysa pencereyi kapatıyoruz
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydetme sırasında bir hata oluştu: {ex.Message}");
            }
        }
    }
}