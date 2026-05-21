using ThreadBase.Data;
using ThreadBase.Models;
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
    /// <summary>
    /// AddCategoryWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class AddCategoryWindow : Window
    {
        public AddCategoryWindow()
        {
            InitializeComponent();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new AppDbContext())
            {
                // Eğer hafızada düzenlenecek bir sohbet varsa (Yani 2. metot çalıştıysa)

                    Category newCategory = new Category
                    {
                        Name = txtTitle.Text,
                    };
                    db.Categories.Add(newCategory); // Yeni veri EKLE

                db.SaveChanges();
            }

            this.DialogResult = true;
        }
    }
}
