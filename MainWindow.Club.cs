using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalo;

public partial class MainWindow
{
    private string _clubSearchQuery = string.Empty;
    private string _clubStatusFilter = "Все";
    private string _equipStatusFilter = "Все";

    // Страница Клуб Аллюр

    private void ShowClubPage()
    {
        BtnClub.Classes.Add("active");
        BtnHippodrome.Classes.Remove("active");

        var root = new Border
        {
            Background = _clubBg,
            Padding = new Thickness(22, 16)
        };

        var layout = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 12
        };

        var activeSessions = _data.Orders.Count(o => !ClubLogic.IsClosedStatus(o.Status));
        var rentedEquip = _data.Equipment.Count(e => e.Status == "В аренде");
        var totalRevenue = _data.Orders
            .Where(o => o.Status != ClubLogic.OrderStatuses.CancelledVet)
            .Sum(o => o.TotalPrice);

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 16
        };

        var titleStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 3,
            Children =
            {
                new TextBlock
                {
                    Text = "Клуб «Аллюр» - Манеж, тренировки и снаряжение",
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textDarkBrown
                },
                new TextBlock
                {
                    Text = "Запись всадников на конкур, аренда манежа и выдача снаряжения (седла, шлемы)",
                    FontSize = 12,
                    Foreground = _textMediumBrown
                }
            }
        };

        var kpiRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                CreateKpiCard("Активных занятий", activeSessions.ToString(), false),
                CreateKpiCard("Снаряжение в аренде", $"{rentedEquip} / {_data.Equipment.Count}", false),
                CreateKpiCard("Сумма по заказам", ClubLogic.FormatMoney(totalRevenue), false)
            }
        };

        Grid.SetColumn(titleStack, 0);
        Grid.SetColumn(kpiRow, 1);
        headerGrid.Children.Add(titleStack);
        headerGrid.Children.Add(kpiRow);

        var leftStripe = new Border { Background = _accentOrange };
        var rightBanner = new Border
        {
            Padding = new Thickness(18, 14),
            Child = headerGrid
        };
        Grid.SetColumn(leftStripe, 0);
        Grid.SetColumn(rightBanner, 1);

        var bannerCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("5,*"),
                Children = { leftStripe, rightBanner }
            }
        };
        Grid.SetRow(bannerCard, 0);
        layout.Children.Add(bannerCard);

        var tabBar = new Border
        {
            Background = Brush.Parse("#DFD3C5"),
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    CreateClubSubTabButton("Аренда манежа и тренировки", 0),
                    CreateClubSubTabButton("Аренда снаряжения", 1),
                    CreateClubSubTabButton("Всадники и абонементы", 2)
                }
            }
        };
        Grid.SetRow(tabBar, 1);
        layout.Children.Add(tabBar);

        var tableContainer = new Border
        {
            Background = Brushes.White,
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14)
        };

        tableContainer.Child = _clubTabIndex switch
        {
            0 => BuildClubArenaTab(),
            1 => BuildClubEquipmentTab(),
            _ => BuildClubRidersTab()
        };

        Grid.SetRow(tableContainer, 2);
        layout.Children.Add(tableContainer);

        root.Child = layout;
        MainContent.Content = root;
    }

    private Button CreateClubSubTabButton(string title, int index)
    {
        var btn = new Button { Content = title };
        btn.Classes.Add("subtab-club");
        if (_clubTabIndex == index)
            btn.Classes.Add("active");

        btn.Click += (_, _) =>
        {
            _clubTabIndex = index;
            _clubSearchQuery = string.Empty;
            ShowClubPage();
        };
        return btn;
    }

    // Вкладка аренды манежа и расписания тренировок

    private Control BuildClubArenaTab()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 10
        };

        var toolbar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 10
        };

        var searchBox = new TextBox
        {
            PlaceholderText = "Поиск по N документа, всаднику или лошади...",
            Text = _clubSearchQuery
        };
        searchBox.Classes.Add("compact");

        var filterCombo = new ComboBox
        {
            Width = 220,
            ItemsSource = new[]
            {
                "Все",
                ClubLogic.OrderStatuses.Booked,
                ClubLogic.OrderStatuses.Saddled,
                ClubLogic.OrderStatuses.InArena,
                ClubLogic.OrderStatuses.Completed,
                ClubLogic.OrderStatuses.CancelledVet
            },
            SelectedItem = _clubStatusFilter
        };
        filterCombo.Classes.Add("compact");

        var btnNewOrder = new Button { Content = "+ Записать на тренировку" };
        btnNewOrder.Classes.Add("compact");

        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(filterCombo, 1);
        Grid.SetColumn(btnNewOrder, 2);
        toolbar.Children.Add(searchBox);
        toolbar.Children.Add(filterCombo);
        toolbar.Children.Add(btnNewOrder);

        Grid.SetRow(toolbar, 0);
        grid.Children.Add(toolbar);

        const string cols = "165,1.25*,1.2*,85,105,165,220";
        var header = CreateTableHeader(cols, new[]
        {
            ("N Документа", 0),
            ("Всадник", 1),
            ("Лошадь и манеж", 2),
            ("Время", 3),
            ("Сумма", 4),
            ("Статус", 5),
            ("Действия", 6)
        }, darkStyle: false);

        Grid.SetRow(header, 1);
        grid.Children.Add(header);

        var rowsHost = new StackPanel { Spacing = 0 };
        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = rowsHost
        };
        Grid.SetRow(scroller, 2);
        grid.Children.Add(scroller);

        void PopulateRows()
        {
            rowsHost.Children.Clear();
            string q = _clubSearchQuery.Trim();

            var filtered = _data.Orders.Where(o =>
            {
                if (_clubStatusFilter != "Все" && o.Status != _clubStatusFilter)
                    return false;
                if (string.IsNullOrEmpty(q))
                    return true;
                string horseNames = string.Join(" ", o.Items.Select(i => i.HorseName));
                return o.OrderNum.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || o.RiderName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || o.ArenaZone.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || horseNames.Contains(q, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = "Записи по выбранному фильтру не найдены.",
                    Foreground = _textMutedBrown,
                    FontSize = 13,
                    Margin = new Thickness(14, 20)
                });
                return;
            }

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var order = filtered[idx];
                var firstItem = order.Items.FirstOrDefault();
                var horse = firstItem != null
                    ? _data.Horses.FirstOrDefault(h => h.Id == firstItem.HorseId)
                    : null;

                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(cols),
                    ColumnSpacing = 8,
                    Margin = new Thickness(14, 10)
                };

                var colDoc = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = order.OrderNum,
                            FontSize = 12,
                            FontWeight = FontWeight.Bold,
                            Foreground = _textDarkBrown
                        },
                        new TextBlock
                        {
                            Text = order.OrderDate.ToString("dd.MM.yyyy HH:mm"),
                            FontSize = 11,
                            Foreground = _textMutedBrown
                        }
                    }
                };

                var colRider = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = order.RiderName,
                            FontSize = 13,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = _textDarkBrown,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = order.RiderDiscount > 0 ? $"Скидка всадника: {order.RiderDiscount:0}%" : "Без льготной скидки",
                            FontSize = 11,
                            Foreground = _textMutedBrown
                        }
                    }
                };

                string horseName = firstItem?.HorseName ?? "-";
                string vetNote = horse != null && !horse.IsReadyForTraining ? " (веткарантин)" : "";
                string trainerLabel = firstItem is { HasTrainer: true } ? "с тренером" : "самостоятельно";
                var colHorse = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"{horseName}{vetNote}",
                            FontSize = 13,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = horse != null && !horse.IsReadyForTraining ? Brush.Parse("#C62828") : _textDarkBrown,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = $"{order.ArenaZone}, {trainerLabel}",
                            FontSize = 11,
                            Foreground = _textMediumBrown,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    }
                };

                var colDuration = new TextBlock
                {
                    Text = firstItem != null ? $"{firstItem.DurationMinutes} мин" : "45 мин",
                    FontSize = 12,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var colPrice = new TextBlock
                {
                    Text = ClubLogic.FormatMoney(order.TotalPrice),
                    FontSize = 13,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var colStatus = CreateOrderStatusBadge(order.Status);

                var actions = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    VerticalAlignment = VerticalAlignment.Center
                };

                string? nextLabel = ClubLogic.GetNextButtonLabel(order.Status);
                string? nextStatus = ClubLogic.GetNextPrimaryStatus(order.Status);
                if (nextLabel != null && nextStatus != null)
                {
                    var btnNext = new Button { Content = nextLabel };
                    btnNext.Classes.Add("table-btn");
                    btnNext.Click += (_, _) =>
                    {
                        bool ready = horse?.IsReadyForTraining ?? true;
                        try
                        {
                            ClubLogic.TransitionOrderStatus(order, nextStatus, ready);
                            ShowToast($"{order.OrderNum}: статус изменен на '{order.Status}'");
                            ShowClubPage();
                        }
                        catch (InvalidOperationException ex)
                        {
                            ShowToast(ex.Message);
                        }
                    };
                    actions.Children.Add(btnNext);
                }

                if (!ClubLogic.IsClosedStatus(order.Status))
                {
                    var btnCancel = new Button { Content = "Отмена" };
                    btnCancel.Classes.Add("table-danger");
                    ToolTip.SetTip(btnCancel, "Отменить по ветеринарным показаниям");
                    btnCancel.Click += (_, _) => OpenCancelOrderModal(order);
                    actions.Children.Add(btnCancel);
                }

                var btnSheet = new Button { Content = "Лист" };
                btnSheet.Classes.Add("table-secondary");
                ToolTip.SetTip(btnSheet, "Маршрутный лист и акт проверки (SHA256)");
                btnSheet.Click += (_, _) => OpenRouteSheetModal(order);
                actions.Children.Add(btnSheet);

                Grid.SetColumn(colDoc, 0);
                Grid.SetColumn(colRider, 1);
                Grid.SetColumn(colHorse, 2);
                Grid.SetColumn(colDuration, 3);
                Grid.SetColumn(colPrice, 4);
                Grid.SetColumn(colStatus, 5);
                Grid.SetColumn(actions, 6);

                rowGrid.Children.Add(colDoc);
                rowGrid.Children.Add(colRider);
                rowGrid.Children.Add(colHorse);
                rowGrid.Children.Add(colDuration);
                rowGrid.Children.Add(colPrice);
                rowGrid.Children.Add(colStatus);
                rowGrid.Children.Add(actions);

                rowsHost.Children.Add(new Border
                {
                    Background = idx % 2 == 0 ? Brushes.White : Brush.Parse("#FAF7F4"),
                    BorderBrush = Brush.Parse("#EFE7DE"),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Child = rowGrid
                });
            }
        }

        searchBox.TextChanged += (_, _) =>
        {
            _clubSearchQuery = searchBox.Text ?? string.Empty;
            PopulateRows();
        };

        filterCombo.SelectionChanged += (_, _) =>
        {
            _clubStatusFilter = filterCombo.SelectedItem?.ToString() ?? "Все";
            PopulateRows();
        };

        btnNewOrder.Click += (_, _) => OpenNewOrderModal();

        PopulateRows();
        return grid;
    }

    private void OpenNewOrderModal()
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            "Новая запись в манеж",
            "Расчет: (мин / 45) * 1500 руб. + тренер 1000 руб. - скидка всадника"));

        var riderCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = _data.Riders,
            SelectedIndex = 0
        };
        riderCombo.Classes.Add("compact");

        var horseCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = _data.Horses,
            SelectedIndex = 0
        };
        horseCombo.Classes.Add("compact");

        var arenaCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "Конкурный манеж", "Крытый манеж 1", "Учебный манеж", "Открытый плац" },
            SelectedIndex = 0
        };
        arenaCombo.Classes.Add("compact");

        var durationCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "45 мин (1 акад. час)", "90 мин (2 акад. часа)", "135 мин (3 акад. часа)" },
            SelectedIndex = 0
        };
        durationCombo.Classes.Add("compact");

        var timeInput = new TextBox
        {
            Text = DateTime.Now.AddHours(1).ToString("HH:00"),
            PlaceholderText = "Например 15:00"
        };
        timeInput.Classes.Add("compact");

        var chkTrainer = new CheckBox
        {
            Content = "Услуги тренера по конкуру (+1 000 руб.)",
            IsChecked = true,
            Foreground = _textDarkBrown,
            FontSize = 13
        };

        var horseStatusNote = new TextBlock
        {
            FontSize = 12,
            Foreground = _textMediumBrown,
            TextWrapping = TextWrapping.Wrap
        };

        var costPreview = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = _accentOrange
        };

        int SelectedMinutes() => durationCombo.SelectedIndex switch
        {
            1 => 90,
            2 => 135,
            _ => 45
        };

        void RefreshPreview()
        {
            var rider = riderCombo.SelectedItem as ClubRider;
            var horse = horseCombo.SelectedItem as HorseItem;
            int minutes = SelectedMinutes();
            bool hasTrainer = chkTrainer.IsChecked == true;
            decimal discount = rider?.DiscountRate ?? 0m;

            decimal total = ClubLogic.CalculateSessionCost(minutes, hasTrainer, discount);
            costPreview.Text = $"К оплате: {ClubLogic.FormatMoney(total)}";

            if (horse != null)
            {
                int usedMin = _data.GetHorseDailyMinutes(horse.Id, DateTime.Today);
                string vetTxt = horse.IsReadyForTraining ? "допущена" : "веткарантин (запись закрыта)";
                horseStatusNote.Text = $"Статус лошади: {vetTxt}, нагрузка сегодня: {usedMin / 60.0:0.0} / 3.0 ч";
                horseStatusNote.Foreground = !horse.IsReadyForTraining || usedMin + minutes > 180
                    ? Brush.Parse("#C62828")
                    : _textMediumBrown;
            }
        }

        riderCombo.SelectionChanged += (_, _) => RefreshPreview();
        horseCombo.SelectionChanged += (_, _) => RefreshPreview();
        durationCombo.SelectionChanged += (_, _) => RefreshPreview();
        chkTrainer.IsCheckedChanged += (_, _) => RefreshPreview();
        RefreshPreview();

        var formRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 12
        };
        var leftField = CreateFormField("Длительность", durationCombo);
        var rightField = CreateFormField("Время начала (сегодня)", timeInput);
        Grid.SetColumn(leftField, 0);
        Grid.SetColumn(rightField, 1);
        formRow.Children.Add(leftField);
        formRow.Children.Add(rightField);

        var summaryBox = new Border
        {
            Background = Brush.Parse("#FAF6F2"),
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10),
            Child = new StackPanel
            {
                Spacing = 4,
                Children = { horseStatusNote, costPreview }
            }
        };

        var btnSave = new Button { Content = "Создать запись" };
        btnSave.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSave.Click += (_, _) =>
        {
            var rider = riderCombo.SelectedItem as ClubRider;
            var horse = horseCombo.SelectedItem as HorseItem;
            if (rider == null || horse == null)
                return;

            if (!horse.IsReadyForTraining)
            {
                ShowPopupError(horseCombo, "Лошадь на веткарантине!");
                return;
            }

            int duration = SelectedMinutes();
            int currentMinutes = _data.GetHorseDailyMinutes(horse.Id, DateTime.Today);
            if (currentMinutes + duration > 180)
            {
                ShowPopupError(horseCombo, "Превышен лимит 3 часа/сутки!");
                return;
            }

            DateTime sessionDt = DateTime.Today.AddHours(15);
            if (TimeSpan.TryParse(timeInput.Text?.Trim(), out var parsedTime))
                sessionDt = DateTime.Today.Add(parsedTime);

            bool hasTrainer = chkTrainer.IsChecked == true;
            decimal cost = ClubLogic.CalculateSessionCost(duration, hasTrainer, rider.DiscountRate);
            int seq = _data.NextOrderSequence++;
            int newId = _data.Orders.Count + 1;

            var newOrder = new RidingOrder
            {
                Id = newId,
                OrderNum = ClubLogic.FormatOrderNumber(DateTime.Now.Year, seq),
                UserId = _data.CurrentUser.Id,
                ClubRiderId = rider.Id,
                RiderName = rider.FullName,
                RiderDiscount = rider.DiscountRate,
                OrderDate = sessionDt,
                TotalPrice = cost,
                Status = ClubLogic.OrderStatuses.Booked,
                ArenaZone = arenaCombo.SelectedItem?.ToString() ?? "Конкурный манеж",
                Items =
                {
                    new RidingSessionItem
                    {
                        Id = newId,
                        OrderId = newId,
                        HorseId = horse.Id,
                        HorseName = horse.Nickname,
                        SessionDateTime = sessionDt,
                        DurationMinutes = duration,
                        HasTrainer = hasTrainer,
                        ItemCost = cost
                    }
                }
            };

            _data.Orders.Insert(0, newOrder);
            CloseModal();
            ShowToast($"Создана запись {newOrder.OrderNum}");
            ShowClubPage();
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnSave }
        };

        box.Children.Add(CreateFormField("Всадник", riderCombo));
        box.Children.Add(CreateFormField("Лошадь", horseCombo));
        box.Children.Add(CreateFormField("Манеж / Зона", arenaCombo));
        box.Children.Add(formRow);
        box.Children.Add(chkTrainer);
        box.Children.Add(summaryBox);
        box.Children.Add(buttons);

        ShowModal(box);
    }

    private void OpenCancelOrderModal(RidingOrder order)
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            $"Отмена заказа {order.OrderNum}",
            "Перевод статуса в «Отмена по ветпоказаниям»"));

        var reasonBox = new TextBox
        {
            PlaceholderText = "Причина отмены (например: осмотр копытного сустава)..."
        };
        reasonBox.Classes.Add("compact");

        var btnConfirm = new Button { Content = "Подтвердить отмену" };
        btnConfirm.Classes.Add("compact");
        var btnClose = new Button { Content = "Назад" };
        btnClose.Classes.Add("secondary");
        btnClose.Click += (_, _) => CloseModal();

        btnConfirm.Click += (_, _) =>
        {
            try
            {
                ClubLogic.TransitionOrderStatus(
                    order,
                    ClubLogic.OrderStatuses.CancelledVet,
                    horseReadyForTraining: true,
                    cancelReason: reasonBox.Text);
                CloseModal();
                ShowToast($"{order.OrderNum}: отмена по ветпоказаниям");
                ShowClubPage();
            }
            catch (InvalidOperationException ex)
            {
                ShowToast(ex.Message);
            }
        };

        box.Children.Add(CreateFormField("Причина", reasonBox));
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnClose, btnConfirm }
        });

        ShowModal(box);
    }

    private void OpenRouteSheetModal(RidingOrder order)
    {
        string docText = ClubLogic.BuildRouteSheetText(order);

        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            "Маршрутный лист и акт проверки",
            $"Цифровая подпись SHA256 сформирована для {order.OrderNum}"));

        var previewBorder = new Border
        {
            Background = Brush.Parse("#FAF6F2"),
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            MaxHeight = 320,
            Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new TextBlock
                {
                    Text = docText,
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                    FontSize = 11.5,
                    Foreground = _textDarkBrown
                }
            }
        };

        var savedLabel = new TextBlock
        {
            FontSize = 12,
            Foreground = Brush.Parse("#2E7D32"),
            IsVisible = false,
            TextWrapping = TextWrapping.Wrap
        };

        var btnExport = new Button { Content = "Выгрузить в файл (.txt)" };
        btnExport.Classes.Add("compact");
        var btnClose = new Button { Content = "Закрыть" };
        btnClose.Classes.Add("secondary");
        btnClose.Click += (_, _) => CloseModal();

        btnExport.Click += (_, _) =>
        {
            string exportDir = Path.Combine(AppContext.BaseDirectory, "Exports");
            Directory.CreateDirectory(exportDir);
            string filePath = Path.Combine(exportDir, $"{order.OrderNum}.txt");
            File.WriteAllText(filePath, docText);
            savedLabel.Text = $"Документ выгружен: {filePath}";
            savedLabel.IsVisible = true;
            ShowToast($"Экспортирован файл {order.OrderNum}.txt");
        };

        box.Children.Add(previewBorder);
        box.Children.Add(savedLabel);
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnClose, btnExport }
        });

        ShowModal(box);
    }

    // Вкладка аренды снаряжения (седла, шлемы)

    private Control BuildClubEquipmentTab()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 10
        };

        var toolbar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 10
        };

        var searchBox = new TextBox
        {
            PlaceholderText = "Поиск по инв. номеру, названию (седло, шлем) или всаднику...",
            Text = _clubSearchQuery
        };
        searchBox.Classes.Add("compact");

        var filterCombo = new ComboBox
        {
            Width = 175,
            ItemsSource = new[] { "Все", "Свободно", "В аренде", "На проверке" },
            SelectedItem = _equipStatusFilter
        };
        filterCombo.Classes.Add("compact");

        var btnAddEquip = new Button { Content = "+ Добавить снаряжение" };
        btnAddEquip.Classes.Add("compact");

        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(filterCombo, 1);
        Grid.SetColumn(btnAddEquip, 2);
        toolbar.Children.Add(searchBox);
        toolbar.Children.Add(filterCombo);
        toolbar.Children.Add(btnAddEquip);

        Grid.SetRow(toolbar, 0);
        grid.Children.Add(toolbar);

        const string cols = "85,1.5*,100,95,1.3*,100,115,135";
        var header = CreateTableHeader(cols, new[]
        {
            ("Инв. N", 0),
            ("Наименование", 1),
            ("Категория", 2),
            ("Размер", 3),
            ("Арендатор / Заказ", 4),
            ("Тариф", 5),
            ("Статус", 6),
            ("Действие", 7)
        }, darkStyle: false);

        Grid.SetRow(header, 1);
        grid.Children.Add(header);

        var rowsHost = new StackPanel { Spacing = 0 };
        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = rowsHost
        };
        Grid.SetRow(scroller, 2);
        grid.Children.Add(scroller);

        void PopulateRows()
        {
            rowsHost.Children.Clear();
            string q = _clubSearchQuery.Trim();

            var filtered = _data.Equipment.Where(e =>
            {
                if (_equipStatusFilter != "Все" && e.Status != _equipStatusFilter)
                    return false;
                if (string.IsNullOrEmpty(q))
                    return true;
                return e.InventoryCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || e.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || e.Category.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || e.RentedByRider.Contains(q, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = "Снаряжение по выбранному запросу не найдено.",
                    Foreground = _textMutedBrown,
                    FontSize = 13,
                    Margin = new Thickness(14, 20)
                });
                return;
            }

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var item = filtered[idx];
                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(cols),
                    ColumnSpacing = 8,
                    Margin = new Thickness(14, 10)
                };

                var colCode = new TextBlock
                {
                    Text = item.InventoryCode,
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textMediumBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var colName = new TextBlock
                {
                    Text = item.Name,
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = _textDarkBrown,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var colCat = new TextBlock
                {
                    Text = item.Category,
                    FontSize = 12,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var colSize = new TextBlock
                {
                    Text = item.Size,
                    FontSize = 12,
                    Foreground = _textMediumBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var colRider = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 1,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = item.RentedByRider,
                            FontSize = 12,
                            FontWeight = item.Status == "В аренде" ? FontWeight.SemiBold : FontWeight.Normal,
                            Foreground = _textDarkBrown,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = item.OrderNum == "-" ? "Склад амуниции" : item.OrderNum,
                            FontSize = 11,
                            Foreground = _textMutedBrown
                        }
                    }
                };

                var colRate = new TextBlock
                {
                    Text = $"{item.PricePerHour:0} руб./ч",
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var statusBadge = item.Status switch
                {
                    "Свободно" => CreateBadge("Свободно", "#E8F5E9", "#2E7D32", "#A5D6A7"),
                    "В аренде" => CreateBadge("В аренде", "#FFF3E0", "#E65100", "#FFCC80"),
                    _ => CreateBadge("На проверке", "#EFEBE9", "#5D4037", "#BCAAA4")
                };

                var actionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (item.Status == "Свободно")
                {
                    var btnRent = new Button { Content = "Выдать в аренду" };
                    btnRent.Classes.Add("table-btn");
                    btnRent.Click += (_, _) => OpenRentEquipmentModal(item);
                    actionPanel.Children.Add(btnRent);
                }
                else if (item.Status == "В аренде")
                {
                    var btnReturn = new Button { Content = "Принять возврат" };
                    btnReturn.Classes.Add("table-secondary");
                    btnReturn.Click += (_, _) =>
                    {
                        item.Status = "Свободно";
                        item.RentedByRider = "-";
                        item.OrderNum = "-";
                        ShowToast($"{item.InventoryCode}: снаряжение возвращено на склад");
                        ShowClubPage();
                    };
                    actionPanel.Children.Add(btnReturn);
                }
                else
                {
                    var btnApprove = new Button { Content = "Допустить" };
                    btnApprove.Classes.Add("table-success");
                    btnApprove.Click += (_, _) =>
                    {
                        item.Status = "Свободно";
                        ShowToast($"{item.InventoryCode}: проверка пройдена");
                        ShowClubPage();
                    };
                    actionPanel.Children.Add(btnApprove);
                }

                Grid.SetColumn(colCode, 0);
                Grid.SetColumn(colName, 1);
                Grid.SetColumn(colCat, 2);
                Grid.SetColumn(colSize, 3);
                Grid.SetColumn(colRider, 4);
                Grid.SetColumn(colRate, 5);
                Grid.SetColumn(statusBadge, 6);
                Grid.SetColumn(actionPanel, 7);

                rowGrid.Children.Add(colCode);
                rowGrid.Children.Add(colName);
                rowGrid.Children.Add(colCat);
                rowGrid.Children.Add(colSize);
                rowGrid.Children.Add(colRider);
                rowGrid.Children.Add(colRate);
                rowGrid.Children.Add(statusBadge);
                rowGrid.Children.Add(actionPanel);

                rowsHost.Children.Add(new Border
                {
                    Background = idx % 2 == 0 ? Brushes.White : Brush.Parse("#FAF7F4"),
                    BorderBrush = Brush.Parse("#EFE7DE"),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Child = rowGrid
                });
            }
        }

        searchBox.TextChanged += (_, _) =>
        {
            _clubSearchQuery = searchBox.Text ?? string.Empty;
            PopulateRows();
        };

        filterCombo.SelectionChanged += (_, _) =>
        {
            _equipStatusFilter = filterCombo.SelectedItem?.ToString() ?? "Все";
            PopulateRows();
        };

        btnAddEquip.Click += (_, _) => OpenAddEquipmentModal();

        PopulateRows();
        return grid;
    }

    private void OpenRentEquipmentModal(EquipmentRentalItem item)
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            $"Выдача: {item.Name}",
            $"Инв. N {item.InventoryCode}, размер {item.Size}, {item.PricePerHour:0} руб./час"));

        var riderCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = _data.Riders,
            SelectedIndex = 0
        };
        riderCombo.Classes.Add("compact");

        var activeOrders = _data.Orders
            .Where(o => !ClubLogic.IsClosedStatus(o.Status))
            .Select(o => o.OrderNum)
            .Prepend("Без привязки к заказу")
            .ToList();

        var orderCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = activeOrders,
            SelectedIndex = 0
        };
        orderCombo.Classes.Add("compact");

        var btnConfirm = new Button { Content = "Выдать всаднику" };
        btnConfirm.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnConfirm.Click += (_, _) =>
        {
            if (riderCombo.SelectedItem is not ClubRider rider)
                return;

            string selectedOrder = orderCombo.SelectedItem?.ToString() ?? "-";
            item.Status = "В аренде";
            item.RentedByRider = rider.FullName;
            item.OrderNum = selectedOrder == "Без привязки к заказу" ? "Разовая аренда" : selectedOrder;

            CloseModal();
            ShowToast($"{item.InventoryCode} выдан: {rider.FullName}");
            ShowClubPage();
        };

        box.Children.Add(CreateFormField("Всадник", riderCombo));
        box.Children.Add(CreateFormField("Заказ на тренировку", orderCombo));
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnConfirm }
        });

        ShowModal(box);
    }

    private void OpenAddEquipmentModal()
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader("Новое снаряжение", "Добавление единицы экипировки в прокат"));

        var txtName = new TextBox { PlaceholderText = "Например: Шлем конкурный Samshield" };
        txtName.Classes.Add("compact");

        var catCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "Седло", "Шлем", "Защита", "Амуниция" },
            SelectedIndex = 0
        };
        catCombo.Classes.Add("compact");

        var txtSize = new TextBox { PlaceholderText = "Например: 17.5\" или M (55-58)" };
        txtSize.Classes.Add("compact");

        var txtRate = new TextBox { Text = "350", PlaceholderText = "Цена в руб/час" };
        txtRate.Classes.Add("compact");

        var btnSave = new Button { Content = "Добавить" };
        btnSave.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSave.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ShowPopupError(txtName, "Введите название");
                return;
            }
            decimal.TryParse(txtRate.Text, out decimal rate);
            if (rate <= 0) rate = 300m;

            int id = _data.NextEquipmentId++;
            _data.Equipment.Add(new EquipmentRentalItem
            {
                Id = id,
                InventoryCode = $"EQ-{id:D3}",
                Name = txtName.Text.Trim(),
                Category = catCombo.SelectedItem?.ToString() ?? "Седло",
                Size = string.IsNullOrWhiteSpace(txtSize.Text) ? "Универсал" : txtSize.Text.Trim(),
                PricePerHour = rate,
                Status = "Свободно",
                RentedByRider = "-",
                OrderNum = "-"
            });

            CloseModal();
            ShowToast($"Добавлено снаряжение EQ-{id:D3}");
            ShowClubPage();
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 12
        };
        var fSize = CreateFormField("Размер", txtSize);
        var fRate = CreateFormField("Тариф (руб./час)", txtRate);
        Grid.SetColumn(fSize, 0);
        Grid.SetColumn(fRate, 1);
        row.Children.Add(fSize);
        row.Children.Add(fRate);

        box.Children.Add(CreateFormField("Наименование", txtName));
        box.Children.Add(CreateFormField("Категория", catCombo));
        box.Children.Add(row);
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnSave }
        });

        ShowModal(box);
    }

    // Вкладка всадников и клубных абонементов

    private Control BuildClubRidersTab()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 10
        };

        var toolbar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 10
        };

        var searchBox = new TextBox
        {
            PlaceholderText = "Поиск всадника по ФИО, телефону или абонементу...",
            Text = _clubSearchQuery
        };
        searchBox.Classes.Add("compact");

        var btnNewRider = new Button { Content = "+ Добавить всадника" };
        btnNewRider.Classes.Add("compact");

        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(btnNewRider, 1);
        toolbar.Children.Add(searchBox);
        toolbar.Children.Add(btnNewRider);

        Grid.SetRow(toolbar, 0);
        grid.Children.Add(toolbar);

        const string cols = "1.6*,145,145,1.25*,95,105";
        var header = CreateTableHeader(cols, new[]
        {
            ("ФИО всадника", 0),
            ("Телефон", 1),
            ("Уровень подготовки", 2),
            ("Клубный абонемент", 3),
            ("Скидка", 4),
            ("Остаток", 5)
        }, darkStyle: false);

        Grid.SetRow(header, 1);
        grid.Children.Add(header);

        var rowsHost = new StackPanel { Spacing = 0 };
        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = rowsHost
        };
        Grid.SetRow(scroller, 2);
        grid.Children.Add(scroller);

        void PopulateRows()
        {
            rowsHost.Children.Clear();
            string q = _clubSearchQuery.Trim();

            var filtered = _data.Riders.Where(r =>
                string.IsNullOrEmpty(q)
                || r.FullName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.Phone.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.MembershipName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = "Всадники по выбранному запросу не найдены.",
                    Foreground = _textMutedBrown,
                    FontSize = 13,
                    Margin = new Thickness(14, 20)
                });
                return;
            }

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var rider = filtered[idx];
                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(cols),
                    ColumnSpacing = 8,
                    Margin = new Thickness(14, 11)
                };

                var cName = new TextBlock
                {
                    Text = rider.FullName,
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var cPhone = new TextBlock
                {
                    Text = rider.Phone,
                    FontSize = 12,
                    Foreground = _textMediumBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var cSkill = new TextBlock
                {
                    Text = rider.SkillLevel,
                    FontSize = 12,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var cMember = new TextBlock
                {
                    Text = rider.MembershipName,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var cDisc = rider.DiscountRate > 0
                    ? CreateBadge($"-{rider.DiscountRate:0}%", "#FFF3E0", "#E65100", "#FFCC80")
                    : CreateBadge("0%", "#F5F5F5", "#757575");

                var cRem = new TextBlock
                {
                    Text = rider.RemainingSessions,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = _textMediumBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(cName, 0);
                Grid.SetColumn(cPhone, 1);
                Grid.SetColumn(cSkill, 2);
                Grid.SetColumn(cMember, 3);
                Grid.SetColumn(cDisc, 4);
                Grid.SetColumn(cRem, 5);

                rowGrid.Children.Add(cName);
                rowGrid.Children.Add(cPhone);
                rowGrid.Children.Add(cSkill);
                rowGrid.Children.Add(cMember);
                rowGrid.Children.Add(cDisc);
                rowGrid.Children.Add(cRem);

                rowsHost.Children.Add(new Border
                {
                    Background = idx % 2 == 0 ? Brushes.White : Brush.Parse("#FAF7F4"),
                    BorderBrush = Brush.Parse("#EFE7DE"),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Child = rowGrid
                });
            }
        }

        searchBox.TextChanged += (_, _) =>
        {
            _clubSearchQuery = searchBox.Text ?? string.Empty;
            PopulateRows();
        };

        btnNewRider.Click += (_, _) => OpenAddRiderModal();

        PopulateRows();
        return grid;
    }

    private void OpenAddRiderModal()
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader("Новый всадник / абонемент", "Добавление клиента клуба"));

        var txtName = new TextBox { PlaceholderText = "ФИО полностью" };
        txtName.Classes.Add("compact");

        var txtPhone = new TextBox { PlaceholderText = "+7 (900) 000-00-00" };
        txtPhone.Classes.Add("compact");

        var skillCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "Начинающий", "Любитель", "Опытный всадник", "Спортсмен (конкур)" },
            SelectedIndex = 1
        };
        skillCombo.Classes.Add("compact");

        var memberCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "Разовый визит (0%)", "Базовый 4 зан. (5%)", "Опытный 8 зан. (10%)", "Конкур PRO 12 зан. (15%)", "Клубный безлимит (20%)" },
            SelectedIndex = 1
        };
        memberCombo.Classes.Add("compact");

        var btnSave = new Button { Content = "Сохранить всадника" };
        btnSave.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSave.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ShowPopupError(txtName, "Введите ФИО");
                return;
            }

            var (mName, disc, rem) = memberCombo.SelectedIndex switch
            {
                0 => ("Разовый визит", 0m, "-"),
                1 => ("Базовый (4 зан.)", 5m, "4 зан."),
                2 => ("Опытный (8 зан.)", 10m, "8 зан."),
                3 => ("Конкур PRO (12 зан.)", 15m, "12 зан."),
                _ => ("Клубный безлимит", 20m, "Безлимит")
            };

            _data.Riders.Add(new ClubRider
            {
                Id = _data.NextRiderId++,
                FullName = txtName.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? "+7 (900) 000-00-00" : txtPhone.Text.Trim(),
                SkillLevel = skillCombo.SelectedItem?.ToString() ?? "Любитель",
                DiscountRate = disc,
                MembershipName = mName,
                RemainingSessions = rem
            });

            CloseModal();
            ShowToast($"Добавлен всадник {txtName.Text.Trim()}");
            ShowClubPage();
        };

        box.Children.Add(CreateFormField("ФИО всадника", txtName));
        box.Children.Add(CreateFormField("Телефон", txtPhone));
        box.Children.Add(CreateFormField("Уровень подготовки", skillCombo));
        box.Children.Add(CreateFormField("Абонемент и скидка", memberCombo));
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnSave }
        });

        ShowModal(box);
    }
}
