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

namespace NetCheck
{
    /// <summary>
    /// Логика взаимодействия для Dialog.xaml
    /// </summary>
    public partial class Dialog : Window
    {
        string titleText, messageText;

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = adressTb.Text.Trim().Length > 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            adressTb.Focus();
        }

        public Dialog(string title = "Введите данные", string message = "Введите адрес")
        {
            InitializeComponent();
            titleText = title;
            messageText = message;
            Title = title;
            MessageLabel.Content = message;
        }
    }
}
