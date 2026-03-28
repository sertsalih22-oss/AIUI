using System;
using System.Windows;
// Chat modelini tanıması için bu satır kesinlikle olmalı!
using AIUI.Models;
using AIUI.Data;

namespace AIUI
{
    public partial class AddChatWindow : Window
    {
        // Düzenlenecek sohbeti hafızada tutmak için değişken
        private Chat _chatToEdit = null;

        // 1. YAPICI METOT: Sadece URL ile yeni sohbet eklerken çalışır
        public AddChatWindow(string incomingUrl = "")
        {
            InitializeComponent();
            txtUrl.Text = incomingUrl;
        }

        // 2. YAPICI METOT (Hata veren kısmı çözen kod): Düzenleme yaparken çalışır
        public AddChatWindow(Chat chatToEdit)
        {
            InitializeComponent();

            // Gelen sohbet verisini hafızaya alıyoruz
            _chatToEdit = chatToEdit;

            // Ekrandaki kutucukları mevcut bilgilerle dolduruyoruz
            txtTitle.Text = chatToEdit.Title;
            cmbCategory.Text = chatToEdit.Category;
            txtUrl.Text = chatToEdit.Url;
        }

        // --- Kaydet Butonu Tıklanma Olayı ---
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new AppDbContext())
            {
                // Eğer hafızada düzenlenecek bir sohbet varsa (Yani 2. metot çalıştıysa)
                if (_chatToEdit != null)
                {
                    _chatToEdit.Title = txtTitle.Text;
                    _chatToEdit.Category = cmbCategory.Text;
                    _chatToEdit.Url = txtUrl.Text;

                    db.Chats.Update(_chatToEdit); // Veriyi GÜNCELLE
                }
                // Eğer hafıza boşsa (Yani 1. metot çalıştıysa)
                else
                {
                    Chat newChat = new Chat
                    {
                        Title = txtTitle.Text,
                        Category = cmbCategory.Text,
                        Url = txtUrl.Text,
                        AddedDate = DateTime.Now
                    };
                    db.Chats.Add(newChat); // Yeni veri EKLE
                }

                db.SaveChanges();
            }

            this.DialogResult = true;
        }
    }
}