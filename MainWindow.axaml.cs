using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia;

namespace Avalo;

public partial class MainWindow : Window
{
    private readonly IBrush _textDarkBrown = new SolidColorBrush(Color.Parse("#3E2723"));

    public MainWindow()
    {
        InitializeComponent();
        ShowLogin(); 

        BtnClub.Click += (_, _) => ShowClubPage();
        BtnHippodrome.Click += (_, _) => ShowHippodromePage();
    }

    private void ShowPopupError(Control target, string message)
    {
        var popup = new Popup
        {
            PlacementTarget = target,
            Placement = PlacementMode.Bottom,
            IsLightDismissEnabled = true,
            Child = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse("#E65100")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 5),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush(Color.Parse("#E65100")),
                    FontSize = 13,
                    FontWeight = FontWeight.Bold
                }
            }
        };

        popup.Open();
        
        var timer = new System.Timers.Timer(3000);
        timer.Elapsed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => popup.Close());
            timer.Dispose();
        };
        timer.AutoReset = false;
        timer.Start();
    }

    // ==========================================
    // СТРАНИЦА: АВТОРИЗАЦИЯ
    // ==========================================
    private void ShowLogin()
    {
        TopPanel.IsVisible = false;

        var formContainer = new StackPanel
        {
            Width = 320,
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var title = new TextBlock
        {
            Text = "Авторизация",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = _textDarkBrown,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };

        var txtLogin = new TextBox
        {
            PlaceholderText = "Введите логин...",
            Margin = new Avalonia.Thickness(0, 20, 0, 0)
        };

        var txtPassword = new TextBox
        {
            PlaceholderText = "Введите пароль...",
            PasswordChar = '•',
            Margin = new Avalonia.Thickness(0, 35, 0, 0)
        };

        var btnLogin = new Button
        {
            Content = "Войти",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Avalonia.Thickness(0, 35, 0, 0)
        };

        var linkRegister = new TextBlock
        {
            Text = "Нет аккаунта? Создать",
            Foreground = _textDarkBrown,
            TextDecorations = TextDecorations.Underline,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Margin = new Avalonia.Thickness(0, 15, 0, 0)
        };

        btnLogin.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                ShowPopupError(txtLogin, "Пустая строка");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                ShowPopupError(txtPassword, "Пустая строка");
                return;
            }

            OnLoginSuccess();
        };

        linkRegister.PointerPressed += (_, _) => ShowRegister();

        formContainer.Children.Add(title);
        formContainer.Children.Add(txtLogin);
        formContainer.Children.Add(txtPassword);
        formContainer.Children.Add(btnLogin);
        formContainer.Children.Add(linkRegister);

        MainContent.Content = formContainer;
    }

    // ==========================================
    // СТРАНИЦА: РЕГИСТРАЦИЯ
    // ==========================================
    private void ShowRegister()
    {
        TopPanel.IsVisible = false;

        var formContainer = new StackPanel
        {
            Width = 320,
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var title = new TextBlock
        {
            Text = "Регистрация",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = _textDarkBrown,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };

        var txtFullName = CreateTextBox("Введите полное ФИО...");
        var txtLogin = CreateTextBox("Введите логин...");
        var txtPassword = CreateTextBox("Введите пароль...", true);

        var btnRegister = new Button
        {
            Content = "Зарегистрироваться",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Avalonia.Thickness(0, 15, 0, 0)
        };

        var linkLogin = new TextBlock
        {
            Text = "Уже есть аккаунт? Войти",
            Foreground = _textDarkBrown,
            TextDecorations = TextDecorations.Underline,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Margin = new Avalonia.Thickness(0, 15, 0, 0)
        };

        btnRegister.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                ShowPopupError(txtFullName, "Пустая строка");
                return;
            }
            if (txtFullName.Text.Trim().Split(' ').Length < 2)
            {
                ShowPopupError(txtFullName, "Введите имя и фамилию");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                ShowPopupError(txtLogin, "Пустая строка");
                return;
            }
            if (txtLogin.Text.Length < 4)
            {
                ShowPopupError(txtLogin, "Логин должен быть не менее 4 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                ShowPopupError(txtPassword, "Пустая строка");
                return;
            }
            if (txtPassword.Text.Length < 6)
            {
                ShowPopupError(txtPassword, "Пароль должен быть не менее 6 символов");
                return;
            }

            OnLoginSuccess();
        };

        linkLogin.PointerPressed += (_, _) => ShowLogin();

        formContainer.Children.Add(title);
        formContainer.Children.Add(txtFullName);
        formContainer.Children.Add(txtLogin);
        formContainer.Children.Add(txtPassword);
        formContainer.Children.Add(btnRegister);
        formContainer.Children.Add(linkLogin);

        MainContent.Content = formContainer;
    }

    private TextBox CreateTextBox(string placeholder, bool isPassword = false)
    {
        return new TextBox
        {
            PlaceholderText = placeholder,
            PasswordChar = isPassword ? '•' : '\0',
            Margin = new Avalonia.Thickness(0, 10, 0, 0)
        };
    }

    // ==========================================
    // СТРАНИЦА: КЛУБ АЛЛЮР (ПУСТАЯ)
    // ==========================================
    private void ShowClubPage()
    {
        BtnClub.Classes.Add("active");
        BtnHippodrome.Classes.Remove("active");

        var grid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var text = new TextBlock
        {
            Text = "Страница: Клуб Аллюр",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = _textDarkBrown
        };

        grid.Children.Add(text);
        MainContent.Content = grid;
    }

    // ==========================================
    // СТРАНИЦА: ИППОДРОМ (ПУСТАЯ)
    // ==========================================
    private void ShowHippodromePage()
    {
        BtnHippodrome.Classes.Add("active");
        BtnClub.Classes.Remove("active");

        var grid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var text = new TextBlock
        {
            Text = "Страница: Ипподром",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = _textDarkBrown
        };

        grid.Children.Add(text);
        MainContent.Content = grid;
    }

    private void OnLoginSuccess()
    {
        TopPanel.IsVisible = true; 
        ShowClubPage(); 
    }
}