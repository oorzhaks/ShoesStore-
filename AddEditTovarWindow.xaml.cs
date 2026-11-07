using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace WpfApp9
{
    public partial class AddEditTovarWindow : Window
    {
        private readonly ShoesStoreОоржакEntities6 _context = new ShoesStoreОоржакEntities6();
        private Products _currentProduct;
        private string _photoPath;
        private readonly bool _isEdit;

        public AddEditTovarWindow(Products selectedProduct)
        {
            InitializeComponent();

            CmbCategory.ItemsSource = _context.ProductCategories.ToList();
            CmbManufacturer.ItemsSource = _context.Manufacturers.ToList();
            CmbSupplier.ItemsSource = _context.Suppliers.ToList();

            if (selectedProduct != null)
            {
                _isEdit = true;
                _currentProduct = _context.Products
                    .Include(p => p.ProductNames)
                    .First(p => p.ID_Product == selectedProduct.ID_Product);

                this.Title = "Редактирование товара";

                LblID.Visibility = Visibility.Visible;
                TxtID.Visibility = Visibility.Visible;
                BtnDelete.Visibility = Visibility.Visible;

                if (_currentProduct.ProductNames != null)
                    TxtName.Text = _currentProduct.ProductNames.ProductName;
            }
            else
            {
                _isEdit = false;
                _currentProduct = new Products();
                this.Title = "Добавление товара";

                LblID.Visibility = Visibility.Collapsed;
                TxtID.Visibility = Visibility.Collapsed;
            }

            DataContext = _currentProduct;

            if (!string.IsNullOrEmpty(_currentProduct.Photo))
            {
                string fullPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Images", _currentProduct.Photo);
                if (File.Exists(fullPath))
                    PhotoImage.Source = new BitmapImage(new Uri(fullPath));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Введите наименование товара!");
                return;
            }

            if (!decimal.TryParse(TxtPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Цена должна быть положительным числом!");
                return;
            }

            if (!int.TryParse(TxtQuantity.Text, out int qty) || qty < 0)
            {
                MessageBox.Show("Количество не может быть отрицательным!");
                return;
            }

            int discount = 0;
            if (!string.IsNullOrWhiteSpace(TxtDiscount.Text))
            {
                if (!int.TryParse(TxtDiscount.Text, out discount) || discount < 0 || discount > 100)
                {
                    MessageBox.Show("Скидка должна быть числом от 0 до 100!");
                    return;
                }
            }

            if (CmbCategory.SelectedItem == null ||
                CmbManufacturer.SelectedItem == null ||
                CmbSupplier.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию, производителя и поставщика!");
                return;
            }

            try
            {
                _currentProduct.Price = price;
                _currentProduct.StockQuantity = qty;
                _currentProduct.ActiveEnvironment = discount;
                _currentProduct.ID_Category = (int)CmbCategory.SelectedValue;
                _currentProduct.ID_Manufacturer = (int)CmbManufacturer.SelectedValue;
                _currentProduct.ID_Supplier = (int)CmbSupplier.SelectedValue;
                _currentProduct.ProductDescription = TxtDescription.Text;
                _currentProduct.UnitOfMeasure = TxtUnit.Text;

                if (!_isEdit)
                {
                    var productName = _context.ProductNames
                        .FirstOrDefault(pn => pn.ProductName == TxtName.Text);

                    if (productName == null)
                    {
                        productName = new ProductNames { ProductName = TxtName.Text };
                        _context.ProductNames.Add(productName);
                        _context.SaveChanges();
                    }

                    _currentProduct.ID_ProductName = productName.ID_ProductName;
                    _currentProduct.SKU = "SKU-" + Guid.NewGuid().ToString().Substring(0, 8);

                    if (!string.IsNullOrEmpty(_photoPath))
                        _currentProduct.Photo = _photoPath;

                    _context.Products.Add(_currentProduct);
                }
                else
                {
                    var productName = _context.ProductNames
                        .FirstOrDefault(pn => pn.ID_ProductName == _currentProduct.ID_ProductName);

                    if (productName != null)
                        productName.ProductName = TxtName.Text;
                }

                _context.SaveChanges();

                MessageBox.Show(_isEdit ? "Запись обновлена!" : "Запись добавлена!");

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

            bool inOrders = _context.Orders.Any(o => o.ID_Product == _currentProduct.ID_Product);
            if (inOrders)
            {
                MessageBox.Show("Товар присутствует в заказе, удалить нельзя!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Вы точно хотите удалить товар?",
                    "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                if (!string.IsNullOrEmpty(_currentProduct.Photo))
                {
                    string full = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "Images", _currentProduct.Photo);
                    if (File.Exists(full)) File.Delete(full);
                }

                _context.Products.Remove(_currentProduct);
                _context.SaveChanges();

                MessageBox.Show("Товар удалён!");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectImageBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    DefaultExt = ".jpg",
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
                };

                if (dialog.ShowDialog() != true) return;

                string sourceFilePath = dialog.FileName;
                _photoPath = dialog.SafeFileName;

                string targetDirectory = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Images");

                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                string newPath = Path.Combine(targetDirectory, _photoPath);

                if (!string.IsNullOrEmpty(_currentProduct.Photo))
                {
                    string oldPath = Path.Combine(targetDirectory, _currentProduct.Photo);
                    if (File.Exists(oldPath) && oldPath != newPath)
                        File.Delete(oldPath);
                }

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(sourceFilePath);
                bitmap.DecodePixelWidth = 300;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var fs = new FileStream(newPath, FileMode.Create))
                    encoder.Save(fs);

                PhotoImage.Source = new BitmapImage(new Uri(newPath));
                _currentProduct.Photo = _photoPath;

                MessageBox.Show("Изображение успешно установлено!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}