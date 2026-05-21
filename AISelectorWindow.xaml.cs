using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ThreadBase
{
    public partial class AISelectorWindow : Window
    {
        // Ana pencerenin (MainWindow) bu pencere kapandıktan sonra değerleri okuyabilmesi için Public Property'ler
        public string SelectedAIType { get; private set; }
        public string SelectedUrl { get; private set; }

        public AISelectorWindow()
        {
            InitializeComponent();

            // Kullanıcı pencereyi açtığında, daha önce kaydettiği varsayılan ayar neyse o radyo butonu otomatik seçili gelsin
            string defaultAI = Properties.Settings.Default.DefaultAI;
            if (defaultAI == "ChatGPT") rbChatGPT.IsChecked = true;
            else if (defaultAI == "Claude") rbClaude.IsChecked = true;
            else rbGemini.IsChecked = true;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Varsayılan seçimleri ayarlayalım
            SelectedAIType = "Gemini";
            SelectedUrl = "https://gemini.google.com/app"; // Doğrudan uygulama/yeni chat sayfası

            // 2. Kullanıcının hangi radyo butonunu seçtiğini kontrol edelim
            if (rbChatGPT.IsChecked == true)
            {
                SelectedAIType = "ChatGPT";
                SelectedUrl = "https://chatgpt.com";
            }
            else if (rbClaude.IsChecked == true)
            {
                SelectedAIType = "Claude";
                SelectedUrl = "https://claude.ai";
            }

            // 3. Eğer kullanıcı "Bunu varsayılan açılış modeli yap" onay kutusunu işaretlediyse diske kaydedelim
            if (chkMakeDefault.IsChecked == true)
            {
                Properties.Settings.Default.DefaultAI = SelectedAIType;
                Properties.Settings.Default.Save(); // Ayarı bilgisayara kalıcı olarak yazar
            }

            // Pencereyi başarıyla kapatıyoruz
            this.DialogResult = true;
        }
    }
}
