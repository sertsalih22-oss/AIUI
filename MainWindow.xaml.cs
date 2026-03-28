using AIUI_0._1.Data;
using AIUI_0._1.Models;
using Microsoft.Web.WebView2.Core; // WebView2 çekirdek ayarları için eklendi
using System;
using System.Collections.ObjectModel;
using System.IO; // Klasör yolları için eklendi
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.Collections.Generic;

namespace AIUI_0._1
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Chat> Chats { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();

            // 1. Veritabanı bağlantımızı açıyoruz
            using (var db = new AppDbContext())
            {
                // 2. SİHİRLİ KOMUT: "Eğer AiChats.db dosyası ve tabloları yoksa, şu an sıfırdan oluştur."
                db.Database.EnsureCreated();

                // 3. Veritabanındaki tüm sohbetleri çekiyoruz (Şu an içi boş gelecek)
                var savedChats = db.Chats.OrderByDescending(c => c.AddedDate).ToList();

                // 4. Çektiğimiz gerçek verileri arayüze (ObservableCollection) yüklüyoruz
                Chats = new ObservableCollection<Chat>(savedChats);
            }

            // Listeyi arayüze bağlıyoruz
            ChatList.ItemsSource = Chats;
        }

        // ================= YENİ EKLENEN KISIM =================
        private async void InitializeWebView()
        {
            try
            {
                // 1. Çerezlerin ve oturumun kaydedileceği klasör yolunu belirliyoruz.
                // Windows'taki "AppData/Local/AIUI_0._1_Data" klasörünü kullanacağız.
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string userDataFolder = Path.Combine(appDataPath, "AIUI_0._1_Data");

                // 2. WebView2 ortamını bu klasörle oluşturuyoruz.
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                // 3. XAML'daki 'ChatBrowser' isimli tarayıcımıza bu ayarları yüklüyoruz.
                await ChatBrowser.EnsureCoreWebView2Async(env);

                // 4. Uygulama ilk açıldığında doğrudan Gemini ana sayfasını yükleyelim ki
                // kullanıcı ilk girişini (Login) yapabilsin.
                ChatBrowser.Source = new Uri("https://gemini.google.com");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tarayıcı başlatılırken hata: {ex.Message}");
            }
        }
        // ======================================================

        // Listeden seçim yapıldığında çalışan kodumuz (Bu kısım aynı kalıyor)
        private void ChatList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatList.SelectedItem is Chat selectedChat)
            {
                try
                {
                    // Eğer tarayıcı hazırsa (CoreWebView2 null değilse) adrese git
                    if (ChatBrowser != null && ChatBrowser.CoreWebView2 != null)
                    {
                        ChatBrowser.Source = new Uri(selectedChat.Url);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"URL yüklenirken hata oluştu: {ex.Message}");
                }
            }
        }

        private async void SyncChats_Click(object sender, RoutedEventArgs e)
        {
            // 1. Tarayıcının hazır ve Gemini sayfasında olduğundan emin olalım
            if (ChatBrowser.CoreWebView2 == null)
            {
                MessageBox.Show("Tarayıcı henüz yüklenmedi.");
                return;
            }

            try
            {
                // 2. JavaScript Kodumuz: Sayfadaki sol menüyü tarayıp linkleri ve başlıkları toplayacak
                // Not: Gemini'nin web yapısı zamanla değişebilir, bu kod o anki 'a' (link) etiketlerini hedefler.
                string jsCode = @"
            (() => {
                let chatList = [];
                // Gemini'de sohbet linkleri genellikle '/app/' ile başlar
                let links = document.querySelectorAll('a[href^=""/app/""]');
                
                links.forEach(link => {
                    let title = link.textContent.trim();
                    let url = link.href;
                    
                    // Eğer başlık boş değilse ve listemizde yoksa ekle
                    if(title && title.length > 0) {
                        chatList.push({ Title: title, Url: url, Category: 'İçe Aktarılanlar' });
                    }
                });
                
                // C#'a göndermek için JSON formatına çeviriyoruz
                return JSON.stringify(chatList);
            })();
        ";

                // 3. JavaScript'i tarayıcıda çalıştır ve sonucu al
                string jsonResult = await ChatBrowser.CoreWebView2.ExecuteScriptAsync(jsCode);

                // ExecuteScriptAsync sonucu çift tırnaklı (stringified string) döner, bunu temizlememiz lazım
                if (jsonResult != "null" && jsonResult != "\"[]\"")
                {
                    // Çift tırnakları ve kaçış karakterlerini temizliyoruz
                    string cleanJson = JsonSerializer.Deserialize<string>(jsonResult);

                    // JSON metnini bizim C# Chat nesneleri listesine dönüştürüyoruz
                    var importedChats = JsonSerializer.Deserialize<List<Chat>>(cleanJson);

                    int eklenecekSayi = 0;

                    // 4. Gelen listeyi veritabanımıza kaydediyoruz
                    using (var db = new AppDbContext())
                    {
                        foreach (var chat in importedChats)
                        {
                            // Aynı URL'den veritabanında var mı diye kontrol et (Çift kaydı önlemek için)
                            bool exists = db.Chats.Any(c => c.Url == chat.Url);

                            if (!exists)
                            {
                                chat.AddedDate = DateTime.Now;
                                db.Chats.Add(chat);
                                eklenecekSayi++;
                            }
                        }

                        // Değişiklikleri kaydet
                        db.SaveChanges();
                    }

                    // 5. Arayüzü güncelle
                    if (eklenecekSayi > 0)
                    {
                        MessageBox.Show($"{eklenecekSayi} adet eski sohbet başarıyla çekildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        RefreshChatList(); // Arayüzdeki listeyi yenileyen metodumuz
                    }
                    else
                    {
                        MessageBox.Show("Yeni sohbet bulunamadı veya hepsi zaten ekli.", "Bilgi");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Senkronizasyon sırasında hata: {ex.Message}");
            }
        }

        // Listeyi veritabanından tekrar çekip arayüzü güncelleyen küçük bir yardımcı metot
        private void RefreshChatList()
        {
            using (var db = new AppDbContext())
            {
                Chats.Clear();
                var currentChats = db.Chats.OrderByDescending(c => c.AddedDate).ToList();
                foreach (var chat in currentChats)
                {
                    Chats.Add(chat);
                }
            }
        }

        private void NewChat_Click(object sender, RoutedEventArgs e)
        {
            string currentUrl = "";
            // Yeni ekleme penceresini oluşturuyoruz
            if (ChatBrowser != null && ChatBrowser.Source != null)
            {
                currentUrl = ChatBrowser.Source.ToString();
            }
            AddChatWindow addWindow = new AddChatWindow(currentUrl);

            // Pencereyi "Dialog" olarak açıyoruz (Yani bu pencere kapanmadan arkaya tıklanamaz)
            addWindow.Owner = this;
            bool? result = addWindow.ShowDialog();

            // Eğer pencere "Kaydet" butonuna basılıp başarıyla kapandıysa (DialogResult = true olduysa)
            if (result == true)
            {
                // Ana ekrandaki listemizi veritabanından tekrar çekip güncelliyoruz
                using (var db = new AppDbContext())
                {
                    Chats.Clear(); // Eski listeyi temizle
                    var currentChats = db.Chats.OrderByDescending(c => c.AddedDate).ToList();
                    foreach (var chat in currentChats)
                    {
                        Chats.Add(chat); // Arayüzü güncelle
                    }
                }
            }
        }

        private void CategoryFilter_Click(object sender, RoutedEventArgs e)
        {
            // 1. Hangi butona tıklandığını yakalıyoruz
            if (sender is Button clickedButton)
            {
                // Butonun üzerindeki yazıyı (Content) alıyoruz (Örn: "C# Projeleri")
                string selectedCategory = clickedButton.Content.ToString();

                // 2. Veritabanına bağlanıp filtreleme yapıyoruz
                using (var db = new AppDbContext())
                {
                    Chats.Clear(); // Ekrandaki mevcut listeyi temizle

                    List<Chat> filteredChats;

                    // Eğer "Tüm Sohbetler" seçildiyse hepsini getir
                    if (selectedCategory == "Tüm Sohbetler")
                    {
                        filteredChats = db.Chats.ToList();
                    }
                    // Değilse, veritabanına sadece o kategoriye ait olanları getirmesini söyle
                    else
                    {
                        // LINQ Gücü: SQL'deki "WHERE Category = 'seçilen_kategori'" sorgusunu otomatik oluşturur
                        filteredChats = db.Chats.Where(c => c.Category == selectedCategory).ToList();
                    }

                    // 3. Veritabanından gelen filtrelenmiş sonuçları arayüze (ObservableCollection) ekle
                    foreach (var chat in filteredChats)
                    {
                        Chats.Add(chat);
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ChatBrowser.Source = new Uri("https://gemini.google.com/app");
        }
    }

}