using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace SimpleDictaphone
{
    public partial class AboutAppPage : PhoneApplicationPage
    {
        public AboutAppPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Simple Global Update (SGU) - это название волн обновлений для приложений проекта SimpleSoftware", "ଘ(੭ˊ꒳​ˋ)੭✧", MessageBoxButton.OK);
        }
    }
}