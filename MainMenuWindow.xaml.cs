using System.IO;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;

namespace WpfApp9
{
    public partial class MainMenuWindow : Window
    {
        private readonly Users _currentUser;
        private List<Products> _allProducts;
        private bool? _sortAscending = null;

        public MainMenuWindow(Users user)
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

                // Менеджер (2) — добавление товара + Заказы
                if (user.ID_Role == 2)
                {
                    BtnAddTovar.Visibility = Visibility.Visible;
                    BtnOrders.Visibility = Visibility.Visible;
                }
                // Администратор (3) — всё + Заказы
                else if (user.ID_Role == 3)
                {
                    BtnAddTovar.Visibility = Visibility.Visible;
                    BtnDeleteTovar.Visibility = Visibility.Visible;
                    BtnOrders.Visibility = Visibility.Visible;
                }
            }

            LoadData();
            LoadSuppliers();
            ApplyFilters();
        }

        private void LoadData()
        {
            var context = ShoesStoreОоржакEntities6.GetContext();
            _allProducts = context.Products
                .Include(p => p.ProductCategories)
                .Include(p => p.ProductNames)
                .Include(p => p.Manufacturers)
                .Include(p => p.Suppliers)
                .ToList();
        }

        private void LoadSuppliers()
        {
            var context = ShoesStoreОоржакEntities6.GetContext();
            var suppliers = context.Suppliers.ToList();
            suppliers.Insert(0, new Suppliers
            {
                ID_Supplier = -1,
                SupplierName = "Все поставщики"
            });
            CmbPostavzhik.ItemsSource = suppliers;
            CmbPostavzhik.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            if (_allProducts == null) return;

            IEnumerable<Products> query = _allProducts;

            if (CmbPostavzhik.SelectedItem is Suppliers sup && sup.ID_Supplier != -1)
                query = query.Where(p => p.ID_Supplier == sup.ID_Supplier);

            string search = TxtSearch.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    (p.ProductNames != null && p.ProductNames.ProductName != null &&
                     p.ProductNames.ProductName.ToLower().Contains(search))
                    ||
                    (p.ProductDescription != null &&
                     p.ProductDescription.ToLower().Contains(search))
                    ||
                    (p.ProductCategories != null && p.ProductCategories.CategoryName != null &&
                     p.ProductCategories.CategoryName.ToLower().Contains(search))
                    ||
                    (p.Manufacturers != null && p.Manufacturers.ManufacturerName != null &&
                     p.Manufacturers.ManufacturerName.ToLower().Contains(search))
                    ||
                    (p.Suppliers != null && p.Suppliers.SupplierName != null &&
                     p.Suppliers.SupplierName.ToLower().Contains(search))
                    ||
                    (p.UnitOfMeasure != null &&
                     p.UnitOfMeasure.ToLower().Contains(search))
                    ||
                    (p.SKU != null && p.SKU.ToLower().Contains(search))
                );
            }

            if (_sortAscending == true)
                query = query.OrderBy(p => p.StockQuantity ?? 0);
            else if (_sortAscending == false)
                query = query.OrderByDescending(p => p.StockQuantity ?? 0);

            ListTovar.ItemsSource = query.ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbPostavzhik_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _sortAscending = true;
            ApplyFilters();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            _sortAscending = false;
            ApplyFilters();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = string.Empty;
            if (CmbPostavzhik.Items.Count > 0) CmbPostavzhik.SelectedIndex = 0;
            _sortAscending = null;
            ApplyFilters();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }

        private void BtnAddTovar_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null ||
                (_currentUser.ID_Role != 2 && _currentUser.ID_Role != 3))
            {
                MessageBox.Show("Недостаточно прав!");
                return;
            }

            foreach (Window w in Application.Current.Windows)
                if (w is AddEditTovarWindow) { w.Activate(); return; }

            var add = new AddEditTovarWindow(null);
            if (add.ShowDialog() == true)
            {
                LoadData();
                ApplyFilters();
            }
        }

        private void ListTovar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_currentUser == null) return;
            if (_currentUser.ID_Role != 3) return;

            if (ListTovar.SelectedItem is Products selected)
            {
                foreach (Window w in Application.Current.Windows)
                    if (w is AddEditTovarWindow) { w.Activate(); return; }

                var edit = new AddEditTovarWindow(selected);
                if (edit.ShowDialog() == true)
                {
                    LoadData();
                    ApplyFilters();
                }
            }
        }

        private void ListTovar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void BtnDeleteTovar_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentUser.ID_Role != 3)
            {
                MessageBox.Show("Удалять товары может только администратор!");
                return;
            }

            if (!(ListTovar.SelectedItem is Products selected))
            {
                MessageBox.Show("Выберите товар в списке!");
                return;
            }

            var context = ShoesStoreОоржакEntities6.GetContext();

            bool inOrders = context.Orders.Any(o => o.ID_Product == selected.ID_Product);
            if (inOrders)
            {
                MessageBox.Show("Товар присутствует в заказе, удалить нельзя!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                    $"Вы точно хотите удалить товар \"{selected.ProductNames?.ProductName}\"?",
                    "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                if (!string.IsNullOrEmpty(selected.Photo))
                {
                    string full = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "Images", selected.Photo);
                    if (File.Exists(full)) File.Delete(full);
                }

                var toDelete = context.Products.Find(selected.ID_Product);
                if (toDelete != null)
                {
                    context.Products.Remove(toDelete);
                    context.SaveChanges();
                }

                MessageBox.Show("Товар удалён!");

                LoadData();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null) return;

            // Менеджер (2) и администратор (3)
            if (_currentUser.ID_Role != 2 && _currentUser.ID_Role != 3)
            {
                MessageBox.Show("Недостаточно прав!");
                return;
            }

            var orders = new OrdersWindow(_currentUser);
            orders.Show();
            this.Close();
        }
    }
}