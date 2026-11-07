using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace WpfApp9
{
    public partial class OrdersWindow : Window
    {
        private readonly Users _currentUser;
        private List<Orders> _allOrders;

        public OrdersWindow(Users user)
        {
            InitializeComponent();
            _currentUser = user;

            if (user == null)
            {
                TxtUserName.Text = "Гость";
            }
            else
            {
                TxtUserName.Text = user.FullName;

                // Менеджер (2) — только просмотр
                if (user.ID_Role == 2)
                {
                    BtnAddOrder.Visibility = Visibility.Collapsed;
                    BtnDeleteOrder.Visibility = Visibility.Collapsed;
                }
                // Администратор (3) — добавление и удаление
                else if (user.ID_Role == 3)
                {
                    BtnAddOrder.Visibility = Visibility.Visible;
                    BtnDeleteOrder.Visibility = Visibility.Visible;
                }
            }

            LoadData();
        }

        private void LoadData()
        {
            var context = ShoesStoreОоржакEntities6.GetContext();
            _allOrders = context.Orders
                .Include(o => o.PickupPoint)
                .ToList();

            ListOrders.ItemsSource = _allOrders;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            var main = new MainMenuWindow(_currentUser);
            main.Show();
            this.Close();
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentUser.ID_Role != 3)
            {
                MessageBox.Show("Добавлять заказы может только администратор!");
                return;
            }

            foreach (Window w in Application.Current.Windows)
                if (w is AddEditOrderWindow) { w.Activate(); return; }

            var add = new AddEditOrderWindow(null);
            if (add.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void ListOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_currentUser == null) return;
            if (_currentUser.ID_Role != 3) return;

            if (ListOrders.SelectedItem is Orders selected)
            {
                foreach (Window w in Application.Current.Windows)
                    if (w is AddEditOrderWindow) { w.Activate(); return; }

                var edit = new AddEditOrderWindow(selected);
                if (edit.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentUser.ID_Role != 3)
            {
                MessageBox.Show("Удалять заказы может только администратор!");
                return;
            }

            if (!(ListOrders.SelectedItem is Orders selected))
            {
                MessageBox.Show("Выберите заказ в списке!");
                return;
            }

            if (MessageBox.Show(
                    $"Вы точно хотите удалить заказ №{selected.OrderNumber}?",
                    "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                var context = ShoesStoreОоржакEntities6.GetContext();

                var toDelete = context.Orders.Find(selected.ID_Order);
                if (toDelete != null)
                {
                    context.Orders.Remove(toDelete);
                    context.SaveChanges();
                }

                MessageBox.Show("Заказ удалён!");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}