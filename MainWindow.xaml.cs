using ThreadBase.Data;
using ThreadBase.Models;
using Microsoft.Web.WebView2.Core; // WebView2 çekirdek ayarları için eklendi
using System;
using System.Collections.ObjectModel;
using System.IO; // Klasör yolları için eklendi
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.Collections.Generic;
using ThreadBase.Properties;

namespace ThreadBase
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Chat> Chats { get; set; }
        public ObservableCollection<Category> Categories { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();

            // 1. Veritabanı bağlantımızı açıyoruz
            using (var db = new AppDbContext())
            {
                // 🚨 GELİŞTİRME AŞAMASI ÖNLEMİ:
                // Şemamız (sütunlar ve tablolar) tamamen değiştiği için eski uyumsuz .db dosyasını siliyoruz.
                //db.Database.EnsureDeleted();

                // Yeni 'Category' tablosu ve 'CategoryId' ilişkisiyle veritabanını sıfırdan tertemiz oluşturuyoruz.
                db.Database.EnsureCreated();

                // 2. İLK AÇILIŞTA VARSAYILAN KATEGORİLERİ ENJEKTE ETME
                // Veritabanı sıfırlandığı için sol menü boş kalmasın diye başlangıç kategorilerini ekliyoruz.
                if (!db.Categories.Any())
                {
                    db.Categories.Add(new Category { Name = "Genel" });

                    // Değişiklikleri SQLite veritabanı dosyasına fiziksel olarak kaydet
                    db.SaveChanges();
                }

                // 3. VERİLERİ ARAYÜZE (UI) BAĞLAMA
                // Veritabanındaki güncel kategorileri çekip listenin hafızasına yüklüyoruz
                var savedCategories = db.Categories.ToList();
                Categories = new ObservableCollection<Category>(savedCategories);

                // 4. SİHİRLİ BAĞLANTI: XAML'daki 'CategoryList' isimli ListView'u bu koleksiyona bağlıyoruz
                CategoryList.ItemsSource = Categories;

                // 5. İLK AÇILIŞTA ORTA LİSTEYİ AYARLAMA
                // Uygulama ilk açıldığında sağ taraftaki sohbet listesi boş kalmasın diye 
                // veritabanındaki tüm sohbetleri (Chats) çekip orta listeye (ChatList) basıyoruz.
                var savedChats = db.Chats.OrderByDescending(c => c.AddedDate).ToList();
                Chats = new ObservableCollection<Chat>(savedChats);
                ChatList.ItemsSource = Chats;
            }
        }

        // ================= YENİ EKLENEN KISIM =================
        private async void InitializeWebView()
        {
            try
            {
                // 1. Çerezlerin ve oturumun kaydedileceği klasör yolunu belirliyoruz.
                // Windows'taki "AppData/Local/AIUI_Data" klasörünü kullanacağız.
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string userDataFolder = Path.Combine(appDataPath, "AIUI_Data");

                // 2. WebView2 ortamını bu klasörle oluşturuyoruz.
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                // 3. XAML'daki 'ChatBrowser' isimli tarayıcımıza bu ayarları yüklüyoruz.
                await ChatBrowser.EnsureCoreWebView2Async(env);

                // 4. Uygulama ilk açıldığında doğrudan Gemini ana sayfasını yükleyelim ki
                // kullanıcı ilk girişini (Login) yapabilsin.
                if(Settings.Default.DefaultAI == "Chatgpt") { ChatBrowser.Source = new Uri("https://chatgpt.com"); }
                else if (Settings.Default.DefaultAI == "Claude") { ChatBrowser.Source = new Uri("https://claude.ai"); }
                else { ChatBrowser.Source = new Uri("https://gemini.google.com"); }
                    
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tarayıcı başlatılırken hata: {ex.Message}");
            }
        }
        // ======================================================

        // Listeden seçim yapıldığında çalışan kodumuz (Bu kısım aynı kalıyor)
        private void ChatBorder_LeftClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 1. Tıklanan kutucuğu (Border) yakalıyoruz
            var border = sender as Border;

            // 2. O kutucuğun içine hapsolmuş olan veriyi (Chat nesnesini) çıkartıyoruz
            var selectedChat = border?.DataContext as Chat;

            // 3. Eğer veri başarıyla alındıysa, tarayıcıda açıyoruz
            if (selectedChat != null)
            {
                try
                {
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
            // 1. Tarayıcının hazır olduğundan emin olalım
            if (ChatBrowser.CoreWebView2 == null || ChatBrowser.Source == null)
            {
                MessageBox.Show("Tarayıcı henüz yüklenmedi veya geçerli bir sayfada değil.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // O an açık olan sitenin domain adresini alıyoruz (Örn: gemini.google.com, chatgpt.com)
                string currentHost = ChatBrowser.Source.Host.ToLower();
                string jsCode = "";

                // 2. AKILLI SEÇİCİ MOTORU: Hangi sitedeysek ona özel JS kodunu hazırlıyoruz
                if (currentHost.Contains("gemini.google.com"))
                {
                    jsCode = @"
                (() => {
                    let chatList = [];
                    let links = document.querySelectorAll('a[href^=""/app/""]');
                    links.forEach(link => {
                        let title = link.textContent.trim();
                        let url = link.href;
                        if(title && title.length > 0) {
                            chatList.push({ Title: title, Url: url });
                        }
                    });
                    return JSON.stringify(chatList);
                })();";
                }
                else if (currentHost.Contains("chatgpt.com") || currentHost.Contains("openai.com"))
                {
                    // ChatGPT sol menüdeki geçmiş linkleri '/c/' ile başlar
                    jsCode = @"
                (() => {
                    let chatList = [];
                    let links = document.querySelectorAll('a[href*=""/c/""]');
                    links.forEach(link => {
                        let title = link.textContent.trim();
                        let url = link.href;
                        if(title && title.length > 0) {
                            chatList.push({ Title: title, Url: url });
                        }
                    });
                    return JSON.stringify(chatList);
                })();";
                }
                else if (currentHost.Contains("claude.ai"))
                {
                    // Claude sol menüdeki geçmiş linkleri '/chat/' ile başlar
                    jsCode = @"
                (() => {
                    let chatList = [];
                    let links = document.querySelectorAll('a[href*=""/chat/""]');
                    links.forEach(link => {
                        let title = link.textContent.trim();
                        let url = link.href;
                        if(title && title.length > 0) {
                            chatList.push({ Title: title, Url: url });
                        }
                    });
                    return JSON.stringify(chatList);
                })();";
                }
                else
                {
                    MessageBox.Show("Bu sayfada otomatik senkronizasyon desteklenmiyor. Lütfen Gemini, ChatGPT veya Claude sayfalarından birine gidin.", "Desteklenmeyen Site", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 3. Hazırlanan dinamik JavaScript'i tarayıcıda çalıştır
                string jsonResult = await ChatBrowser.CoreWebView2.ExecuteScriptAsync(jsCode);

                if (jsonResult != "null" && jsonResult != "\"[]\"")
                {
                    string cleanJson = JsonSerializer.Deserialize<string>(jsonResult);
                    var importedChats = JsonSerializer.Deserialize<List<Chat>>(cleanJson);

                    int eklenecekSayi = 0;

                    // 4. Veritabanına kayıt süreci
                    using (var db = new AppDbContext())
                    {
                        // 'İçe Aktarılanlar' kategorisinin ID'sini bul, yoksa Genel'e (1) ata
                        var iceAktarilanlarCat = db.Categories.FirstOrDefault(c => c.Name == "İçe Aktarılanlar");
                        int targetCategoryId = iceAktarilanlarCat != null ? iceAktarilanlarCat.Id : 1;

                        foreach (var chat in importedChats)
                        {
                            // Eğer bu URL veritabanında zaten yoksa ekle (Mükerrer kaydı önleme)
                            bool exists = db.Chats.Any(c => c.Url == chat.Url);

                            if (!exists)
                            {
                                chat.AddedDate = DateTime.Now;
                                chat.CategoryId = targetCategoryId;
                                db.Chats.Add(chat);
                                eklenecekSayi++;
                            }
                        }
                        db.SaveChanges();
                    }

                    if (eklenecekSayi > 0)
                    {
                        MessageBox.Show($"{eklenecekSayi} adet sohbet geçmişi başarıyla sisteme aktarıldı!", "Senkronizasyon Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        RefreshChatList();
                    }
                    else
                    {
                        MessageBox.Show("Yeni bir sohbet bulunamadı. Mevcut tüm geçmişiniz zaten güncel.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Açık olan sohbet geçmişi listesinden veri sökülemedi. Lütfen ilgili AI hesabınıza giriş yaptığınızdan emin olun.", "Veri Bulunamadı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Senkronizasyon motoru çalışırken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AISelectorWindow AISelectorWindow = new AISelectorWindow();
            AISelectorWindow.Owner = this;
            bool? result = AISelectorWindow.ShowDialog();
            if (result == true)
            {
                ChatBrowser.Source = new Uri(AISelectorWindow.SelectedUrl);
            }
            
        }
        // --- SİLME FONKSİYONU ---
        private void DeleteChat_Click(object sender, RoutedEventArgs e)
        {
            // Sağ tıklanan menü elemanını ve içindeki veriyi (Chat nesnesini) yakalıyoruz
            var menuItem = sender as MenuItem;
            var selectedChat = menuItem?.DataContext as Chat;

            if (selectedChat != null)
            {
                // Kullanıcıya yanlışlıkla silmeye karşı bir onay soralım
                var result = MessageBox.Show($"'{selectedChat.Title}' sohbetini silmek istediğine emin misin?",
                                             "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (var db = new AppDbContext())
                    {
                        db.Chats.Remove(selectedChat); // Veritabanından sil komutu
                        db.SaveChanges();              // Değişiklikleri kaydet
                    }
                    RefreshChatList(); // Listeyi yenile (Zaten yazdığımız metot)
                }
            }
        }

        // --- DÜZENLEME FONKSİYONU ---
        private void EditChat_Click(object sender, RoutedEventArgs e)
        {
            // Yine sağ tıklanan satırdaki veriyi yakalıyoruz
            var menuItem = sender as MenuItem;
            var selectedChat = menuItem?.DataContext as Chat;

            if (selectedChat != null)
            {
                // AddChatWindow'u açarken, seçilen sohbetin tüm verilerini içine fırlatıyoruz
                AddChatWindow editWindow = new AddChatWindow(selectedChat);
                editWindow.Owner = this;

                // Pencere kapanıp true dönerse (başarıyla kaydedilirse) listeyi yenile
                if (editWindow.ShowDialog() == true)
                {
                    RefreshChatList();
                }
            }
        }
        
        private void AddCategory_Click(object sender, RoutedEventArgs e) 
        {
            AddCategoryWindow addWindow = new AddCategoryWindow();

            // Pencereyi "Dialog" olarak açıyoruz (Yani bu pencere kapanmadan arkaya tıklanamaz)
            addWindow.Owner = this;
            bool? result = addWindow.ShowDialog();

            // Eğer pencere "Kaydet" butonuna basılıp başarıyla kapandıysa (DialogResult = true olduysa)
            if (result == true)
            {
                // Ana ekrandaki listemizi veritabanından tekrar çekip güncelliyoruz
                using (var db = new AppDbContext())
                {
                    Categories.Clear(); // Eski listeyi temizle
                    var currentcategories = db.Categories.OrderBy(c => c.Id).ToList();
                    foreach (var category in currentcategories)
                    {
                        Categories.Add(category); // Arayüzü güncelle
                    }
                }
            }
        }
        private void CategoryList_SelectionChanged(object sender, RoutedEventArgs e)
        {

            var selectedCategory = CategoryList.SelectedItem as Category;
            if (selectedCategory == null) return;
            using (var db = new AppDbContext())
            {
                Chats.Clear(); // Ekrandaki mevcut listeyi temizle

                List<Chat> filteredChats;
                // Eğer "Tüm Sohbetler" seçildiyse hepsini getir
                if (selectedCategory.Name == "Genel")
                {
                    filteredChats = db.Chats.ToList();
                }
                // Değilse, veritabanına sadece o kategoriye ait olanları getirmesini söyle
                else
                {
                    // LINQ Gücü: SQL'deki "WHERE Category = 'seçilen_kategori'" sorgusunu otomatik oluşturur
                    filteredChats = db.Chats.Where(c => c.CategoryId == selectedCategory.Id).ToList();
                }

                // 3. Veritabanından gelen filtrelenmiş sonuçları arayüze (ObservableCollection) ekle
                foreach (var chat in filteredChats)
                {
                    Chats.Add(chat);
                }
            }
        }
    }

}