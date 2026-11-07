using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp9
{
    public partial class AddEditOrderWindow : Window
    {
        private readonly ShoesStoreОоржакEntities6 _context = new ShoesStoreОоржакEntities6();
        private Orders _currentOrder;
        private readonly bool _isEdit;

        public AddEditOrderWindow(Orders selectedOrder)
        {
            InitializeComponent();

            // Заполняем пункты выдачи
            CmbPickupPoint.ItemsSource = _context.PickupPoint.ToList();

            if (selectedOrder != null)
            {
                _isEdit = true;
                _currentOrder = _context.Orders
                    .Include(o => o.PickupPoint)
                    .First(o => o.ID_Order == selectedOrder.ID_Order);

                this.Title = "Редактирование заказа";
                BtnDelete.Visibility = Visibility.Visible;
            }
            else
            {
                _isEdit = false;
                _currentOrder = new Orders();
                this.Title = "Добавление заказа";
            }

            DataContext = _currentOrder;

            // Восстановить выбранный статус в ComboBox
            if (!string.IsNullOrEmpty(_currentOrder.OrderStatus))
            {
                foreach (ComboBoxItem item in CmbStatus.Items)
                {
                    if (item.Content?.ToString() == _currentOrder.OrderStatus)
                    {
                        CmbStatus.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtArticul.Text))
            {
                MessageBox.Show("Введите артикул заказа!");
                return;
            }

            if (CmbStatus.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус заказа!");
                return;
            }

            if (CmbPickupPoint.SelectedItem == null)
            {
                MessageBox.Show("Выберите пункт выдачи!");
                return;
            }

            if (DpOrderDate.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату заказа!");
                return;
            }

            try
            {
                // Артикул — целое число
                if (int.TryParse(TxtArticul.Text, out int articul))
                    _currentOrder.OrderNumber = articul;
                else
                {
                    MessageBox.Show("Артикул должен быть числом!");
                    return;
                }

                _currentOrder.OrderStatus =
                    (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();

                _currentOrder.ID_PickupPoint =
                    (int)CmbPickupPoint.SelectedValue;

                _currentOrder.OrderDate = DpOrderDate.SelectedDate;
                _currentOrder.DeluiveryDate = DpDeliveryDate.SelectedDate;

                if (!_isEdit)
                {
                    _context.Orders.Add(_currentOrder);
                }

                _context.SaveChanges();

                MessageBox.Show(_isEdit ? "Заказ обновлён!" : "Заказ добавлен!");

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEdit) return;

            if (MessageBox.Show("Вы точно хотите удалить заказ?",
                    "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                _context.Orders.Remove(_currentOrder);
                _context.SaveChanges();

                MessageBox.Show("Заказ удалён!");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}