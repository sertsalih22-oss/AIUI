using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace ThreadBase.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        // WPF UI'ın eleman eklenip çıkarılmasını izleyeceği dinamik koleksiyon
        public ObservableCollection<Chat> Chats { get; set; }

        // KRİTİK NOKTA: Constructor
        // Eğer bu nesne belleğe çıktığında koleksiyonu başlatmazsak, 
        // ".Chats.Add()" dediğin an NullReferenceException ile sistem çöker.
        public Category()
        {
            Chats = new ObservableCollection<Chat>();
        }

    }
}
