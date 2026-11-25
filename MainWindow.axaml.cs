using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalo;

public partial class MainWindow : Window
{
    private static readonly ConditionalWeakTable<Control, string> _autoTooltipTexts = new();

    static MainWindow()
    {
        SizeChangedEvent.AddClassHandler<TextBlock>((tb, _) => UpdateAutoTooltip(tb));
        LoadedEvent.AddClassHandler<TextBlock>((tb, _) => UpdateAutoTooltip(tb));
    }

    private readonly MockClubData _data = new();

    private readonly IBrush _textDarkBrown = Brush.Parse("#3E2723");
    private readonly IBrush _textMediumBrown = Brush.Parse("#6D4C41");
    private readonly IBrush _textMutedBrown = Brush.Parse("#8D6E63");
    private readonly IBrush _accentOrange = Brush.Parse("#E65100");
    private readonly IBrush _espressoDark = Brush.Parse("#4E342E");
    private readonly IBrush _borderSand = Brush.Parse("#D7CCC8");
    private readonly IBrush _clubBg = Brush.Parse("#EAE0D5");
    private readonly IBrush _hippoBg = Brush.Parse("#E2D5C8");
    private readonly IBrush _hippoCardBg = Brush.Parse("#FAF6F2");

    private readonly DispatcherTimer _shiftTimer;
    private int _remainingSeconds = 45 * 60;
    private bool _isShiftTimerRunning;
    private bool _isTestFiveSecondsMode;

    private readonly DispatcherTimer _toastTimer;
    private Popup? _profilePopup;
    private TextBlock? _lastTooltipCheckedBlock;

    private int _clubTabIndex;
    private int _hippoTabIndex;

    private bool IsZootech => _data.CurrentUser.RoleId == 1;
    private bool IsArenaAdmin => _data.CurrentUser.RoleId == 2;
    private bool IsClient => _data.CurrentUser.RoleId == 3;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(PointerMovedEvent, OnWindowPointerMovedForTooltips, RoutingStrategies.Tunnel);

        _shiftTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _shiftTimer.Tick += OnShiftTimerTick;

        _toastTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3.2)
        };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            ToastBanner.IsVisible = false;
        };

        BtnClub.Click += (_, _) => ShowClubPage();
        BtnHippodrome.Click += (_, _) => ShowHippodromePage();
        BtnProfile.Click += (_, _) => ToggleProfilePopup();

        BtnTimerToggle.Click += (_, _) => ToggleShiftTimer();
        BtnTimerReset.Click += (_, _) => ResetShiftTimer();
        TimerValueText.PointerPressed += (_, _) => SwitchTimerMode();

        ModalOverlay.PointerPressed += (_, e) =>
        {
            if (e.Source == ModalOverlay)
                CloseModal();
        };

        UpdateTimerDisplay();
        ShowLogin();
    }

    // Таймер смены (45 минут)

    private void ToggleShiftTimer()
    {
        if (_remainingSeconds <= 0)
            ResetShiftTimer();

        _isShiftTimerRunning = !_isShiftTimerRunning;
        if (_isShiftTimerRunning)
        {
            _shiftTimer.Start();
            BtnTimerToggle.Content = "Пауза";
        }
        else
        {
            _shiftTimer.Stop();
            BtnTimerToggle.Content = "Старт";
        }
    }

    private void ResetShiftTimer()
    {
        _shiftTimer.Stop();
        _isShiftTimerRunning = false;
        _remainingSeconds = _isTestFiveSecondsMode ? 5 : 45 * 60;
        BtnTimerToggle.Content = "Старт";
        UpdateTimerDisplay();
    }

    private void SwitchTimerMode()
    {
        _isTestFiveSecondsMode = !_isTestFiveSecondsMode;
        ResetShiftTimer();
        ShowToast(_isTestFiveSecondsMode
            ? "Режим проверки таймера: 5 секунд"
            : "Стандартный таймер смены: 45 минут");
    }

    private void OnShiftTimerTick(object? sender, EventArgs e)
    {
        if (_remainingSeconds > 0)
        {
            _remainingSeconds--;
            UpdateTimerDisplay();
        }

        if (_remainingSeconds <= 0)
        {
            _shiftTimer.Stop();
            _isShiftTimerRunning = false;
            BtnTimerToggle.Content = "Старт";
            UpdateTimerDisplay();
            PlayShiftFinishedSound();
            ShowToast("Звуковой сигнал: смена (академический час) завершена!");
        }
    }

    private void UpdateTimerDisplay()
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, _remainingSeconds));
        TimerValueText.Text = $"{(int)span.TotalMinutes:D2}:{span.Seconds:D2}";

        if (_remainingSeconds <= 0)
            TimerValueText.Foreground = Brush.Parse("#C62828");
        else if (_isShiftTimerRunning)
            TimerValueText.Foreground = _accentOrange;
        else
            TimerValueText.Foreground = _textDarkBrown;
    }

    private static void PlayShiftFinishedSound()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Console.Beep(880, 300);
                Console.Beep(1046, 420);
            }
            else
            {
                Console.Beep();
            }
        }
        catch
        {
        }
    }

    // Уведомления и модальные окна

    private void ShowToast(string message)
    {
        ToastMessageText.Text = message;
        ToastBanner.IsVisible = true;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void ShowModal(Control content)
    {
        ModalHost.Content = content;
        ModalOverlay.IsVisible = true;
    }

    private void CloseModal()
    {
        ModalOverlay.IsVisible = false;
        ModalHost.Content = null;
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
                BorderBrush = _accentOrange,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 5),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = _accentOrange,
                    FontSize = 13,
                    FontWeight = FontWeight.Bold
                }
            }
        };

        popup.Open();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.8) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            popup.Close();
        };
        timer.Start();
    }

    // Профиль в верхней панели

    private void UpdateProfilePanel()
    {
        var user = _data.CurrentUser;
        ProfileInitialsText.Text = user.Initials;
        ProfileNameText.Text = user.ShortName;
        ProfileRoleText.Text = user.RoleName;

        ProfileAvatarBorder.Background = user.RoleId switch
        {
            1 => _accentOrange,
            2 => _espressoDark,
            _ => _textMutedBrown
        };
        ProfileRoleText.Foreground = user.RoleId == 1 ? _espressoDark : _accentOrange;
    }

    private void ToggleProfilePopup()
    {
        if (_profilePopup != null && _profilePopup.IsOpen)
        {
            _profilePopup.Close();
            return;
        }

        var user = _data.CurrentUser;
        var currentRole = _data.Roles.FirstOrDefault(r => r.Id == user.RoleId) ?? _data.Roles.Last();

        var cardContent = new StackPanel { Spacing = 10 };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        headerRow.Children.Add(new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(21),
            Background = user.RoleId switch
            {
                1 => _accentOrange,
                2 => _espressoDark,
                _ => _textMutedBrown
            },
            Child = new TextBlock
            {
                Text = user.Initials,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2, MaxWidth = 216 };
        nameStack.Children.Add(new TextBlock
        {
            Text = user.FullName,
            FontWeight = FontWeight.Bold,
            FontSize = 14,
            Foreground = _textDarkBrown,
            TextWrapping = TextWrapping.Wrap
        });
        nameStack.Children.Add(new TextBlock
        {
            Text = $"@{user.Login} | {user.RoleName}",
            FontSize = 12,
            Foreground = _accentOrange,
            FontWeight = FontWeight.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        headerRow.Children.Add(nameStack);
        cardContent.Children.Add(headerRow);

        var infoStack = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = "Доступные возможности:",
                    FontSize = 11,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textMediumBrown
                },
                new TextBlock
                {
                    Text = currentRole.PermissionsSummary,
                    FontSize = 12,
                    Foreground = _textDarkBrown,
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };

        cardContent.Children.Add(new Border
        {
            Background = Brush.Parse("#FAF6F2"),
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8),
            Child = infoStack
        });

        if (IsClient)
        {
            var myRider = _data.FindRiderByName(user.FullName);
            decimal myDiscount = myRider?.DiscountRate ?? 0m;
            string mySkill = myRider?.SkillLevel ?? "Начинающий";
            int mySessions = _data.GetRiderSessionCount(myRider?.Id, user.FullName);
            var unpaidPenalties = _data.Penalties
                .Where(p => p.RiderName.Equals(user.FullName, StringComparison.OrdinalIgnoreCase) && p.Status == "Не оплачен")
                .ToList();

            var clientInfoStack = new StackPanel { Spacing = 4 };
            clientInfoStack.Children.Add(new TextBlock
            {
                Text = "Карточка клиента:",
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = _textMediumBrown
            });
            clientInfoStack.Children.Add(new TextBlock
            {
                Text = $"Уровень / тариф: {mySkill} (скидка {myDiscount:0}%)",
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = _textDarkBrown,
                TextWrapping = TextWrapping.Wrap
            });
            clientInfoStack.Children.Add(new TextBlock
            {
                Text = $"Заказов тренировок: {mySessions} зан.",
                FontSize = 12,
                Foreground = _textMediumBrown
            });
            if (unpaidPenalties.Count > 0)
            {
                decimal unpaidTotal = unpaidPenalties.Sum(p => p.Amount);
                clientInfoStack.Children.Add(new TextBlock
                {
                    Text = $"Неоплаченные штрафы: {unpaidPenalties.Count} ({unpaidTotal:N0} ₽)",
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brush.Parse("#9F1239")
                });
            }

            cardContent.Children.Add(new Border
            {
                Background = Brush.Parse("#FAF6F2"),
                BorderBrush = _borderSand,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8),
                Child = clientInfoStack
            });

            var btnDiscountReq = new Button
            {
                Content = "Запросить скидку / льготу",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            btnDiscountReq.Classes.Add("compact");
            btnDiscountReq.Click += (_, _) =>
            {
                _profilePopup?.Close();
                OpenDiscountRequestModal();
            };
            cardContent.Children.Add(btnDiscountReq);
        }

        var btnLogout = new Button
        {
            Content = "Выйти из аккаунта",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        btnLogout.Classes.Add("compact");
        btnLogout.Click += (_, _) =>
        {
            _profilePopup?.Close();
            _shiftTimer.Stop();
            _isShiftTimerRunning = false;
            BtnTimerToggle.Content = "Старт";
            ShowLogin();
        };

        cardContent.Children.Add(btnLogout);

        var popupBorder = new Border
        {
            Width = 300,
            Background = Brushes.White,
            BorderBrush = _textMutedBrown,
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Child = cardContent
        };
        popupBorder.AddHandler(PointerMovedEvent, OnWindowPointerMovedForTooltips, RoutingStrategies.Tunnel);

        _profilePopup = new Popup
        {
            PlacementTarget = BtnProfile,
            Placement = PlacementMode.BottomEdgeAlignedRight,
            VerticalOffset = 6,
            IsLightDismissEnabled = true,
            Child = popupBorder
        };
        _profilePopup.Open();
    }

    // Авторизация

    private void ShowLogin()
    {
        TopPanel.IsVisible = false;
        _profilePopup?.Close();

        var formContainer = new StackPanel
        {
            Width = 330,
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
            Margin = new Thickness(0, 0, 0, 12)
        };

        var subtitle = new TextBlock
        {
            Text = "Конноспортивный клуб и ипподром «Аллюр»",
            FontSize = 13,
            Foreground = _textMediumBrown,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 14)
        };

        var txtLogin = new TextBox
        {
            PlaceholderText = "Введите логин...",
            Margin = new Thickness(0, 14, 0, 0)
        };

        var txtPassword = new TextBox
        {
            PlaceholderText = "Введите пароль...",
            PasswordChar = '•',
            Margin = new Thickness(0, 22, 0, 0)
        };

        var btnLogin = new Button
        {
            Content = "Войти",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 26, 0, 0)
        };

        var linkRegister = new TextBlock
        {
            Text = "Нет аккаунта? Создать",
            Foreground = _textDarkBrown,
            TextDecorations = TextDecorations.Underline,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
            Margin = new Thickness(0, 15, 0, 0)
        };

        btnLogin.Click += (_, _) =>
        {
            string login = (txtLogin.Text ?? string.Empty).Trim();
            string password = txtPassword.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowPopupError(txtLogin, "Пустая строка");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowPopupError(txtPassword, "Пустая строка");
                return;
            }

            var dbUser = Database.Login(login, password, out bool dbAvailable, out string dbError);
            if (dbAvailable)
            {
                if (dbUser == null)
                {
                    ShowPopupError(txtPassword, dbError);
                    return;
                }

                _data.CurrentUser = dbUser;
                Database.LoadAll(_data, out _);
                OnLoginSuccess();
                ShowToast($"Добро пожаловать, {dbUser.ShortName}");
                return;
            }

            // Автономный режим при выключенной MySQL
            var offlineMatch = _data.Users.FirstOrDefault(u =>
                u.Login.Equals(login, StringComparison.OrdinalIgnoreCase));

            if (offlineMatch == null)
            {
                ShowPopupError(txtLogin, "Пользователь не найден (или проверьте MySQL)");
                return;
            }

            if (!string.IsNullOrEmpty(offlineMatch.OfflinePasswordHash)
                && !string.Equals(offlineMatch.OfflinePasswordHash, Database.HashPassword(password), StringComparison.OrdinalIgnoreCase))
            {
                ShowPopupError(txtPassword, "Неверный пароль");
                return;
            }

            _data.CurrentUser = new UserProfile
            {
                Id = offlineMatch.Id,
                RoleId = offlineMatch.RoleId,
                RoleName = offlineMatch.RoleName,
                Login = offlineMatch.Login,
                FullName = offlineMatch.FullName
            };

            OnLoginSuccess();
            ShowToast("MySQL недоступна, открыт автономный режим");
        };

        linkRegister.PointerPressed += (_, _) => ShowRegister();

        formContainer.Children.Add(title);
        formContainer.Children.Add(subtitle);
        formContainer.Children.Add(txtLogin);
        formContainer.Children.Add(txtPassword);
        formContainer.Children.Add(btnLogin);
        formContainer.Children.Add(linkRegister);

        MainContent.Content = formContainer;
    }

    // Регистрация

    private void ShowRegister()
    {
        TopPanel.IsVisible = false;

        var formContainer = new StackPanel
        {
            Width = 330,
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
            Margin = new Thickness(0, 0, 0, 16)
        };

        var txtFullName = CreateTextBox("Введите полное ФИО...");
        var txtLogin = CreateTextBox("Введите логин...");
        var txtPassword = CreateTextBox("Введите пароль...", true);

        var btnRegister = new Button
        {
            Content = "Зарегистрироваться",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 22, 0, 0)
        };

        var linkLogin = new TextBlock
        {
            Text = "Уже есть аккаунт? Войти",
            Foreground = _textDarkBrown,
            TextDecorations = TextDecorations.Underline,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
            Margin = new Thickness(0, 15, 0, 0)
        };

        btnRegister.Click += (_, _) =>
        {
            string fullName = (txtFullName.Text ?? string.Empty).Trim();
            string login = (txtLogin.Text ?? string.Empty).Trim();
            string password = txtPassword.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowPopupError(txtFullName, "Пустая строка");
                return;
            }
            if (fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
            {
                ShowPopupError(txtFullName, "Введите имя и фамилию");
                return;
            }

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowPopupError(txtLogin, "Пустая строка");
                return;
            }
            if (login.Length < 4)
            {
                ShowPopupError(txtLogin, "Логин должен быть не менее 4 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowPopupError(txtPassword, "Пустая строка");
                return;
            }
            if (password.Length < 6)
            {
                ShowPopupError(txtPassword, "Пароль должен быть не менее 6 символов");
                return;
            }

            var dbUser = Database.RegisterClient(fullName, login, password, out bool dbAvailable, out string dbError);
            if (dbAvailable)
            {
                if (dbUser == null)
                {
                    ShowPopupError(txtLogin, dbError);
                    return;
                }

                _data.CurrentUser = dbUser;
                Database.LoadAll(_data, out _);
                OnLoginSuccess();
                ShowToast("Регистрация выполнена");
                return;
            }

            if (_data.Users.Any(u => u.Login.Equals(login, StringComparison.OrdinalIgnoreCase)))
            {
                ShowPopupError(txtLogin, "Такой логин уже зарегистрирован");
                return;
            }

            int offlineId = _data.Users.Count > 0 ? _data.Users.Max(u => u.Id) + 1 : 4;
            _data.Users.Add(new ManagedUserItem
            {
                Id = offlineId,
                RoleId = 3,
                RoleName = "Клиент",
                Login = login,
                FullName = fullName,
                OfflinePasswordHash = Database.HashPassword(password)
            });

            _data.CurrentUser = new UserProfile
            {
                Id = offlineId,
                RoleId = 3,
                RoleName = "Клиент",
                Login = login,
                FullName = fullName
            };

            OnLoginSuccess();
            ShowToast("MySQL недоступна, открыт автономный режим");
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

    private static TextBox CreateTextBox(string placeholder, bool isPassword = false)
    {
        return new TextBox
        {
            PlaceholderText = placeholder,
            PasswordChar = isPassword ? '•' : '\0',
            Margin = new Thickness(0, 10, 0, 0)
        };
    }

    private void OnLoginSuccess()
    {
        _clubTabIndex = 0;
        _hippoTabIndex = 0;
        ShiftTimerBox.IsVisible = !IsClient;
        UpdateProfilePanel();
        TopPanel.IsVisible = true;
        if (IsZootech)
            ShowHippodromePage();
        else
            ShowClubPage();
    }

    // Общие элементы оформления

    private static Border CreateBadge(string text, string bgHex, string fgHex, string? borderHex = null)
    {
        return new Border
        {
            Background = Brush.Parse(bgHex),
            BorderBrush = borderHex != null ? Brush.Parse(borderHex) : Brushes.Transparent,
            BorderThickness = new Thickness(borderHex != null ? 1 : 0),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(9, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brush.Parse(fgHex),
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        };
    }

    private static Border CreateOrderStatusBadge(string status) => status switch
    {
        ClubLogic.OrderStatuses.Booked => CreateBadge(status, "#FFF3E0", "#E65100", "#FFCC80"),
        ClubLogic.OrderStatuses.Saddled => CreateBadge(status, "#EFEBE9", "#4E342E", "#BCAAA4"),
        ClubLogic.OrderStatuses.InArena => CreateBadge(status, "#E3F2FD", "#1565C0", "#90CAF9"),
        ClubLogic.OrderStatuses.Completed => CreateBadge(status, "#E8F5E9", "#2E7D32", "#A5D6A7"),
        ClubLogic.OrderStatuses.CancelledVet => CreateBadge(status, "#FFEBEE", "#C62828", "#EF9A9A"),
        _ => CreateBadge(status, "#F5F5F5", "#424242")
    };

    private static Border CreateKpiCard(string label, string value, bool darkTheme)
    {
        return new Border
        {
            Background = darkTheme ? Brush.Parse("#3E2723") : Brush.Parse("#FAF6F2"),
            BorderBrush = darkTheme ? Brush.Parse("#6D4C41") : Brush.Parse("#E0D6CC"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 8),
            MinWidth = 130,
            Child = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock
                    {
                        Text = label,
                        FontSize = 11,
                        Foreground = darkTheme ? Brush.Parse("#D7CCC8") : Brush.Parse("#8D6E63")
                    },
                    new TextBlock
                    {
                        Text = value,
                        FontSize = 17,
                        FontWeight = FontWeight.Bold,
                        Foreground = darkTheme ? Brushes.White : Brush.Parse("#3E2723")
                    }
                }
            }
        };
    }

    private Grid CreateModalHeader(string title, string? subtitle = null)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 0, 0, 14) };

        var textStack = new StackPanel { Spacing = 3 };
        textStack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = _textDarkBrown
        });
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            textStack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 12,
                Foreground = _textMediumBrown,
                TextWrapping = TextWrapping.Wrap
            });
        }

        var btnClose = new Button
        {
            Content = "X",
            Width = 32,
            Height = 32,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Top
        };
        btnClose.Classes.Add("secondary");
        btnClose.Click += (_, _) => CloseModal();

        Grid.SetColumn(textStack, 0);
        Grid.SetColumn(btnClose, 1);
        grid.Children.Add(textStack);
        grid.Children.Add(btnClose);
        return grid;
    }

    private static StackPanel CreateFormField(string label, Control input)
    {
        return new StackPanel
        {
            Spacing = 5,
            Children =
            {
                new TextBlock
                {
                    Text = label,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brush.Parse("#5D4037")
                },
                input
            }
        };
    }

    private static Border CreateTableHeader(string columnsDef, (string Text, int Col)[] headers, bool darkStyle)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(columnsDef),
            ColumnSpacing = 8,
            Margin = new Thickness(14, 10)
        };

        foreach (var (text, col) in headers)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = darkStyle ? Brush.Parse("#FAF6F2") : Brush.Parse("#4E342E"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        return new Border
        {
            Background = darkStyle ? Brush.Parse("#4E342E") : Brush.Parse("#F5EFE8"),
            BorderBrush = darkStyle ? Brush.Parse("#3E2723") : Brush.Parse("#E6DDD3"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(10, 10, 0, 0),
            Child = grid
        };
    }

    // Глобальная авто-подсказка полного текста при обрезке/переполнении в любом месте приложения

    private void OnWindowPointerMovedForTooltips(object? sender, PointerEventArgs e)
    {
        if (e.Source is not Visual visual)
            return;

        var tb = visual as TextBlock
            ?? visual.FindAncestorOfType<TextBlock>()
            ?? (visual as Border)?.Child as TextBlock;

        if (tb == null)
        {
            _lastTooltipCheckedBlock = null;
            return;
        }

        if (ReferenceEquals(tb, _lastTooltipCheckedBlock))
            return;

        _lastTooltipCheckedBlock = tb;
        UpdateAutoTooltip(tb);
    }

    private static void UpdateAutoTooltip(TextBlock tb)
    {
        string? text = tb.Text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (tb.FindAncestorOfType<TextBox>() != null || tb.FindAncestorOfType<ToolTip>() != null)
            return;

        var parentButton = tb.FindAncestorOfType<Button>();
        if (parentButton != null && ToolTip.GetTip(parentButton) != null)
            return;

        bool clipped = IsTextClipped(tb);
        SyncAutoTooltipOnControl(tb, clipped ? text : null);

        if (tb.GetVisualParent() is Border parentBorder && ReferenceEquals(parentBorder.Child, tb))
            SyncAutoTooltipOnControl(parentBorder, clipped ? text : null);
    }

    private static void SyncAutoTooltipOnControl(Control target, string? fullText)
    {
        object? currentTip = ToolTip.GetTip(target);
        bool isOwnedByAuto = _autoTooltipTexts.TryGetValue(target, out string? savedAuto)
            && currentTip is string s
            && s == savedAuto;

        // Если на элементе уже задана ручная подсказка — не затираем её
        if (currentTip != null && !isOwnedByAuto)
            return;

        if (!string.IsNullOrWhiteSpace(fullText))
        {
            if (!isOwnedByAuto || savedAuto != fullText)
            {
                ToolTip.SetTip(target, fullText);
                _autoTooltipTexts.AddOrUpdate(target, fullText);
            }
        }
        else if (isOwnedByAuto)
        {
            ToolTip.SetTip(target, null);
            _autoTooltipTexts.Remove(target);
        }
    }

    private static bool IsTextClipped(TextBlock tb)
    {
        if (tb.Bounds.Width <= 1 || tb.Bounds.Height <= 1)
            return false;

        var layout = tb.TextLayout;
        if (layout != null)
        {
            foreach (var line in layout.TextLines)
            {
                if (line.HasCollapsed || line.HasOverflowed)
                    return true;
            }

            if (layout.WidthIncludingTrailingWhitespace > tb.Bounds.Width + 0.5)
                return true;

            if (layout.Height > tb.Bounds.Height + 0.5)
                return true;
        }

        if (!tb.IsAttachedToVisualTree())
            return false;

        Visual? ancestor = tb.GetVisualParent();
        for (int depth = 0; depth < 3 && ancestor != null; depth++)
        {
            if (ancestor is Control parentCtrl && parentCtrl.Bounds.Width > 1)
            {
                var topRight = tb.TranslatePoint(new Point(tb.Bounds.Width, 0), parentCtrl);
                if (topRight.HasValue && topRight.Value.X > parentCtrl.Bounds.Width + 1.0)
                    return true;

                var topLeft = tb.TranslatePoint(new Point(0, 0), parentCtrl);
                if (topLeft.HasValue && topLeft.Value.X < -1.0)
                    return true;
            }
            ancestor = ancestor.GetVisualParent();
        }

        return false;
    }
}
