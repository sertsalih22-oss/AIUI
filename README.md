# AIUI - Gelişmiş Gemini Sohbet Yöneticisi 🤖

AIUI, Google Gemini ile yaptığınız sohbetleri organize etmenizi, kategorilendirmenizi ve tek bir masaüstü uygulaması üzerinden yönetmenizi sağlayan C# tabanlı modern bir WPF uygulamasıdır. 

Uygulama, API sınırlarına takılmadan yerleşik bir Chromium motoru (WebView2) kullanarak doğrudan Gemini web arayüzüyle entegre çalışır.

## ✨ Öne Çıkan Özellikler

* **🚀 Dahili Tarayıcı (WebView2):** Uygulamadan çıkmadan, entegre Chromium motoru üzerinden Gemini'ye erişim ve kesintisiz sohbet deneyimi.
* **🔄 Tek Tıkla Geçmişi Çekme:** Özel yazılmış JavaScript enjeksiyonu sayesinde, web üzerindeki eski Gemini sohbet geçmişinizi tek tıkla yerel veritabanınıza aktarır. Çift kayıtları (duplicate) otomatik engeller.
* **💾 Yerel ve Güvenli Veritabanı:** Sohbetleriniz ve kategorileriniz SQLite ve Entity Framework Core kullanılarak tamamen sizin bilgisayarınızda saklanır.
* **🔒 Güvenli Oturum Yönetimi:** Google oturum çerezleri (cookies) ve `.db` veritabanı dosyanız projenin içinde değil, Windows'un güvenli `AppData/Local` dizininde tutulur. GitHub'a kod yüklendiğinde kişisel verileriniz asla sızmaz.

## 🛠️ Kullanılan Teknolojiler

* **Dil / Framework:** C#, .NET 8, WPF (Windows Presentation Foundation)
* **Veritabanı / ORM:** SQLite, Entity Framework Core (EF Core)
* **Web Motoru:** Microsoft.Web.WebView2
* **Veri İşleme:** System.Text.Json, JavaScript DOM Manipülasyonu

## 🚀 Kurulum ve Çalıştırma

Projeyi kendi bilgisayarınızda çalıştırmak için:

1. Repoyu bilgisayarınıza klonlayın:
   ```bash
   git clone [https://github.com/sertsalih22-oss/AIUI.git](https://github.com/sertsalih22-oss/AIUI.git)