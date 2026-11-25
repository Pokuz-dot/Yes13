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
    private string _requestStatusFilter = "Все";

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

        // 1. Шапка раздела в зависимости от роли
        string headerTitle;
        string headerSubtitle;
        Control[] kpiCards;

        if (IsClient)
        {
            int myRequests = _data.Requests.Count(r => r.UserId == _data.CurrentUser.Id || r.RequesterName == _data.CurrentUser.FullName);
            int myApproved = _data.Orders.Count(o => o.RiderName.Equals(_data.CurrentUser.FullName, StringComparison.OrdinalIgnoreCase));
            int freeEquip = _data.Equipment.Count(e => e.Status == "Свободно");

            headerTitle = "Клуб «Аллюр» - Личный кабинет клиента";
            headerSubtitle = "Отправляйте заявки администратору на тренировку, аренду манежа и снаряжения";
            kpiCards = new Control[]
            {
                CreateKpiCard("Моих заявок", myRequests.ToString(), false),
                CreateKpiCard("Моих занятий", myApproved.ToString(), false),
                CreateKpiCard("Свободно инвентаря", freeEquip.ToString(), false)
            };
        }
        else
        {
            int pendingCount = _data.Requests.Count(r => r.Status == "На рассмотрении");
            int activeSessions = _data.Orders.Count(o => !ClubLogic.IsClosedStatus(o.Status));
            int rentedEquip = _data.Equipment.Count(e => e.Status == "В аренде");

            headerTitle = IsArenaAdmin
                ? "Клуб «Аллюр» - Панель администратора манежа"
                : "Клуб «Аллюр» - Расписание манежа и снаряжение";
            headerSubtitle = IsArenaAdmin
                ? "Обработка заявок клиентов, управление тренировками, снаряжением и ролями пользователей"
                : "Просмотр расписания тренировок по конкуру и занятости снаряжения";
            kpiCards = new Control[]
            {
                CreateKpiCard("Новых заявок", pendingCount.ToString(), false),
                CreateKpiCard("Активных занятий", activeSessions.ToString(), false),
                CreateKpiCard("Снаряжение в аренде", $"{rentedEquip} / {_data.Equipment.Count}", false)
            };
        }

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
                    Text = headerTitle,
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textDarkBrown
                },
                new TextBlock
                {
                    Text = headerSubtitle,
                    FontSize = 12,
                    Foreground = _textMediumBrown
                }
            }
        };

        var kpiRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var card in kpiCards)
            kpiRow.Children.Add(card);

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

        // 2. Вкладки по роли пользователя
        var tabsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        if (IsClient)
        {
            if (_clubTabIndex > 2) _clubTabIndex = 0;
            tabsPanel.Children.Add(CreateClubSubTabButton("Мои заявки", 0));
            tabsPanel.Children.Add(CreateClubSubTabButton("Аренда снаряжения", 1));
            tabsPanel.Children.Add(CreateClubSubTabButton("Мои занятия", 2));
        }
        else if (IsArenaAdmin)
        {
            if (_clubTabIndex > 4) _clubTabIndex = 0;
            int pending = _data.Requests.Count(r => r.Status == "На рассмотрении");
            string reqTabTitle = pending > 0 ? $"Заявки клиентов ({pending})" : "Заявки клиентов";

            tabsPanel.Children.Add(CreateClubSubTabButton(reqTabTitle, 0));
            tabsPanel.Children.Add(CreateClubSubTabButton("Аренда манежа и тренировки", 1));
            tabsPanel.Children.Add(CreateClubSubTabButton("Аренда снаряжения", 2));
            tabsPanel.Children.Add(CreateClubSubTabButton("Всадники и абонементы", 3));
            tabsPanel.Children.Add(CreateClubSubTabButton("Пользователи и роли", 4));
        }
        else
        {
            if (_clubTabIndex > 1) _clubTabIndex = 0;
            tabsPanel.Children.Add(CreateClubSubTabButton("Аренда манежа и тренировки", 0));
            tabsPanel.Children.Add(CreateClubSubTabButton("Аренда снаряжения", 1));
        }

        var tabBar = new Border
        {
            Background = Brush.Parse("#DFD3C5"),
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = tabsPanel
        };
        Grid.SetRow(tabBar, 1);
        layout.Children.Add(tabBar);

        // 3. Содержимое активной вкладки
        var tableContainer = new Border
        {
            Background = Brushes.White,
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14)
        };

        if (IsClient)
        {
            tableContainer.Child = _clubTabIndex switch
            {
                0 => BuildClientRequestsTab(),
                1 => BuildClubEquipmentTab(),
                _ => BuildClubArenaTab()
            };
        }
        else if (IsArenaAdmin)
        {
            tableContainer.Child = _clubTabIndex switch
            {
                0 => BuildAdminRequestsTab(),
                1 => BuildClubArenaTab(),
                2 => BuildClubEquipmentTab(),
                3 => BuildClubRidersTab(),
                _ => BuildAdminUsersTab()
            };
        }
        else
        {
            tableContainer.Child = _clubTabIndex switch
            {
                0 => BuildClubArenaTab(),
                _ => BuildClubEquipmentTab()
            };
        }

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

    // Вкладка заявок для клиента

    private Control BuildClientRequestsTab()
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
            PlaceholderText = "Поиск по своим заявкам (конь, манеж, снаряжение)...",
            Text = _clubSearchQuery
        };
        searchBox.Classes.Add("compact");

        var filterCombo = new ComboBox
        {
            Width = 185,
            ItemsSource = new[] { "Все", "На рассмотрении", "Одобрено", "Отклонено" },
            SelectedItem = _requestStatusFilter
        };
        filterCombo.Classes.Add("compact");

        var btnNewReq = new Button { Content = "+ Заявка на тренировку / манеж" };
        btnNewReq.Classes.Add("compact");

        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(filterCombo, 1);
        Grid.SetColumn(btnNewReq, 2);
        toolbar.Children.Add(searchBox);
        toolbar.Children.Add(filterCombo);
        toolbar.Children.Add(btnNewReq);

        Grid.SetRow(toolbar, 0);
        grid.Children.Add(toolbar);

        const string cols = "125,135,1.4*,1.1*,105,140";
        var header = CreateTableHeader(cols, new[]
        {
            ("Дата / Время", 0),
            ("Тип заявки", 1),
            ("Объект (конь / инвентарь)", 2),
            ("Манеж и комментарий", 3),
            ("Стоимость", 4),
            ("Статус", 5)
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

            var filtered = _data.Requests.Where(r =>
            {
                bool isMine = r.UserId == _data.CurrentUser.Id
                    || r.RequesterName.Equals(_data.CurrentUser.FullName, StringComparison.OrdinalIgnoreCase);
                if (!isMine)
                    return false;
                if (_requestStatusFilter != "Все" && r.Status != _requestStatusFilter)
                    return false;
                if (string.IsNullOrEmpty(q))
                    return true;
                return r.TargetName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || r.RequestType.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || r.ArenaZone.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || r.Comment.Contains(q, StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(r => r.Status == "На рассмотрении" ? 0 : r.Status == "Одобрено" ? 1 : 2)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = "У вас пока нет заявок. Нажмите кнопку справа, чтобы отправить запрос администратору.",
                    Foreground = _textMutedBrown,
                    FontSize = 13,
                    Margin = new Thickness(14, 20)
                });
                return;
            }

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var req = filtered[idx];
                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(cols),
                    ColumnSpacing = 8,
                    Margin = new Thickness(14, 10)
                };

                var cDate = new TextBlock
                {
                    Text = req.RequestedDateTime.ToString("dd.MM.yyyy HH:mm"),
                    FontSize = 12,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var cType = new TextBlock
                {
                    Text = req.RequestType,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = _textMediumBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var cTarget = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = req.TargetName,
                            FontSize = 13,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = _textDarkBrown,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = req.RequestType == "Тренировка и манеж"
                                ? $"{req.DurationMinutes} мин, {(req.HasTrainer ? "с тренером" : "без тренера")}"
                                : "Запрос администратору",
                            FontSize = 11,
                            Foreground = _textMutedBrown
                        }
                    }
                };
                var cArena = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock { Text = req.ArenaZone, FontSize = 12, Foreground = _textDarkBrown },
                        new TextBlock
                        {
                            Text = string.IsNullOrWhiteSpace(req.Comment) ? "-" : req.Comment,
                            FontSize = 11,
                            Foreground = _textMutedBrown,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    }
                };
                var cCost = new TextBlock
                {
                    Text = req.EstimatedCost > 0 ? ClubLogic.FormatMoney(req.EstimatedCost) : "По тарифу",
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var cStatus = CreateRequestStatusBadge(req.Status);

                Grid.SetColumn(cDate, 0);
                Grid.SetColumn(cType, 1);
                Grid.SetColumn(cTarget, 2);
                Grid.SetColumn(cArena, 3);
                Grid.SetColumn(cCost, 4);
                Grid.SetColumn(cStatus, 5);

                rowGrid.Children.Add(cDate);
                rowGrid.Children.Add(cType);
                rowGrid.Children.Add(cTarget);
                rowGrid.Children.Add(cArena);
                rowGrid.Children.Add(cCost);
                rowGrid.Children.Add(cStatus);

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
            _requestStatusFilter = filterCombo.SelectedItem?.ToString() ?? "Все";
            PopulateRows();
        };

        btnNewReq.Click += (_, _) => OpenNewRequestModal();

        PopulateRows();
        return grid;
    }

    // Вкладка входящих заявок для администратора манежа

    private Control BuildAdminRequestsTab()
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
            PlaceholderText = "Поиск заявки по клиенту, лошади или снаряжению...",
            Text = _clubSearchQuery
        };
        searchBox.Classes.Add("compact");

        var filterCombo = new ComboBox
        {
            Width = 195,
            ItemsSource = new[] { "Все", "На рассмотрении", "Одобрено", "Отклонено" },
            SelectedItem = _requestStatusFilter
        };
        filterCombo.Classes.Add("compact");

        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(filterCombo, 1);
        toolbar.Children.Add(searchBox);
        toolbar.Children.Add(filterCombo);

        Grid.SetRow(toolbar, 0);
        grid.Children.Add(toolbar);

        const string cols = "1.25*,130,1.25*,1.1*,100,135,175";
        var header = CreateTableHeader(cols, new[]
        {
            ("Клиент и дата", 0),
            ("Тип заявки", 1),
            ("Объект", 2),
            ("Параметры / Коммент", 3),
            ("Сумма", 4),
            ("Статус", 5),
            ("Решение", 6)
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

            var filtered = _data.Requests.Where(r =>
            {
                if (_requestStatusFilter != "Все" && r.Status != _requestStatusFilter)
                    return false;
                if (string.IsNullOrEmpty(q))
                    return true;
                return r.RequesterName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || r.TargetName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || r.RequestType.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || r.Comment.Contains(q, StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(r => r.Status == "На рассмотрении" ? 0 : r.Status == "Одобрено" ? 1 : 2)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = "Заявок по выбранному фильтру нет.",
                    Foreground = _textMutedBrown,
                    FontSize = 13,
                    Margin = new Thickness(14, 20)
                });
                return;
            }

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var req = filtered[idx];
                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(cols),
                    ColumnSpacing = 8,
                    Margin = new Thickness(14, 10)
                };

                var cClient = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = req.RequesterName,
                            FontSize = 13,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = _textDarkBrown,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = $"На {req.RequestedDateTime:dd.MM.yyyy HH:mm}",
                            FontSize = 11,
                            Foreground = _textMutedBrown
                        }
                    }
                };

                var cType = new TextBlock
                {
                    Text = req.RequestType,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = _textMediumBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var cTarget = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = req.TargetName,
                            FontSize = 12,
                            FontWeight = FontWeight.Bold,
                            Foreground = _textDarkBrown,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = req.ArenaZone,
                            FontSize = 11,
                            Foreground = _textMutedBrown
                        }
                    }
                };

                var cInfo = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = req.RequestType == "Тренировка и манеж"
                                ? $"{req.DurationMinutes} мин, {(req.HasTrainer ? "с тренером" : "без тренера")}"
                                : req.RequestType,
                            FontSize = 11,
                            Foreground = _textDarkBrown
                        },
                        new TextBlock
                        {
                            Text = string.IsNullOrWhiteSpace(req.Comment) ? "-" : req.Comment,
                            FontSize = 11,
                            Foreground = _textMutedBrown,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    }
                };

                var cCost = new TextBlock
                {
                    Text = req.EstimatedCost > 0 ? ClubLogic.FormatMoney(req.EstimatedCost) : "-",
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var cStatus = CreateRequestStatusBadge(req.Status);

                var actionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (req.Status == "На рассмотрении")
                {
                    var btnAccept = new Button { Content = "Принять" };
                    btnAccept.Classes.Add("table-success");
                    btnAccept.Click += (_, _) => ApproveClientRequest(req);

                    var btnReject = new Button { Content = "Отклонить" };
                    btnReject.Classes.Add("table-danger");
                    btnReject.Click += (_, _) =>
                    {
                        if (!Database.UpdateClientRequestStatus(req.Id, "Отклонено", out string err))
                        {
                            ShowToast($"Ошибка БД: {err}");
                            return;
                        }
                        req.Status = "Отклонено";
                        ShowToast($"Заявка от {req.RequesterName} отклонена");
                        ShowClubPage();
                    };

                    actionPanel.Children.Add(btnAccept);
                    actionPanel.Children.Add(btnReject);
                }
                else
                {
                    actionPanel.Children.Add(new TextBlock
                    {
                        Text = "Обработано",
                        FontSize = 11,
                        Foreground = _textMutedBrown,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                Grid.SetColumn(cClient, 0);
                Grid.SetColumn(cType, 1);
                Grid.SetColumn(cTarget, 2);
                Grid.SetColumn(cInfo, 3);
                Grid.SetColumn(cCost, 4);
                Grid.SetColumn(cStatus, 5);
                Grid.SetColumn(actionPanel, 6);

                rowGrid.Children.Add(cClient);
                rowGrid.Children.Add(cType);
                rowGrid.Children.Add(cTarget);
                rowGrid.Children.Add(cInfo);
                rowGrid.Children.Add(cCost);
                rowGrid.Children.Add(cStatus);
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
            _requestStatusFilter = filterCombo.SelectedItem?.ToString() ?? "Все";
            PopulateRows();
        };

        PopulateRows();
        return grid;
    }

    private void ApproveClientRequest(ClientRequestItem req)
    {
        string autoDetails = string.Empty;

        if (req.RequestType == "Тренировка и манеж")
        {
            var horse = req.HorseId.HasValue
                ? _data.Horses.FirstOrDefault(h => h.Id == req.HorseId.Value)
                : _data.Horses.FirstOrDefault(h => h.Nickname.Equals(req.TargetName, StringComparison.OrdinalIgnoreCase));

            if (horse == null)
            {
                ShowToast("Выбранная лошадь не найдена в базе");
                return;
            }
            if (!horse.IsReadyForTraining)
            {
                ShowToast($"Лошадь {horse.Nickname} на веткарантине - одобрение невозможно");
                return;
            }

            int usedMin = _data.GetHorseDailyMinutes(horse.Id, req.RequestedDateTime.Date);
            if (usedMin + req.DurationMinutes > 180)
            {
                ShowToast($"У лошади {horse.Nickname} превышен лимит 3 часа в сутки");
                return;
            }

            var rider = Database.EnsureRiderExists(_data, req.RequesterName, out string riderErr);
            if (!string.IsNullOrEmpty(riderErr))
            {
                ShowToast($"Ошибка БД: {riderErr}");
                return;
            }

            decimal total = ClubLogic.CalculateSessionCost(req.DurationMinutes, req.HasTrainer, rider.DiscountRate);
            int seq = _data.NextOrderSequence++;
            int newId = _data.Orders.Count + 1;

            var order = new RidingOrder
            {
                Id = newId,
                OrderNum = ClubLogic.FormatOrderNumber(DateTime.Now.Year, seq),
                UserId = _data.CurrentUser.Id,
                ClubRiderId = rider.Id,
                RiderName = rider.FullName,
                RiderDiscount = rider.DiscountRate,
                OrderDate = req.RequestedDateTime,
                TotalPrice = total,
                Status = ClubLogic.OrderStatuses.Booked,
                ArenaZone = req.ArenaZone,
                Items =
                {
                    new RidingSessionItem
                    {
                        Id = newId,
                        OrderId = newId,
                        HorseId = horse.Id,
                        HorseName = horse.Nickname,
                        SessionDateTime = req.RequestedDateTime,
                        DurationMinutes = req.DurationMinutes,
                        HasTrainer = req.HasTrainer,
                        ItemCost = total
                    }
                }
            };

            if (!Database.AddOrder(order, out string orderErr))
            {
                ShowToast($"Ошибка БД: {orderErr}");
                return;
            }

            _data.Orders.Insert(0, order);

            var bookedCodes = ClubLogic.AutoAssignEquipment(_data.Equipment, req.EquipmentCode, req.RequesterName, order.OrderNum);
            foreach (var code in bookedCodes)
            {
                var eq = _data.Equipment.FirstOrDefault(e => e.InventoryCode == code);
                if (eq != null)
                    Database.UpdateEquipmentStatus(eq, out _);
            }

            autoDetails = bookedCodes.Count > 0
                ? $" | Заказ {order.OrderNum} + автобронь снаряжения: {string.Join(", ", bookedCodes)}"
                : $" | Заказ {order.OrderNum} создан автоматически";
        }
        else if (req.RequestType == "Аренда снаряжения")
        {
            var equip = _data.Equipment.FirstOrDefault(e =>
                req.TargetName.StartsWith(e.InventoryCode, StringComparison.OrdinalIgnoreCase)
                || req.TargetName.Contains(e.Name, StringComparison.OrdinalIgnoreCase));

            if (equip != null)
            {
                var activeOrder = _data.Orders.FirstOrDefault(o =>
                    !ClubLogic.IsClosedStatus(o.Status)
                    && o.RiderName.Equals(req.RequesterName, StringComparison.OrdinalIgnoreCase));

                equip.Status = "В аренде";
                equip.RentedByRider = req.RequesterName;
                equip.OrderNum = activeOrder?.OrderNum ?? "Аренда по заявке";
                Database.UpdateEquipmentStatus(equip, out _);
                autoDetails = $" | {equip.InventoryCode} автоматически выдан в аренду";
            }
        }
        else if (req.RequestType == "Бронь денника")
        {
            var stall = _data.Stalls.FirstOrDefault(s =>
                req.TargetName.StartsWith(s.StallCode, StringComparison.OrdinalIgnoreCase));

            if (stall != null)
            {
                stall.Status = "Забронирован";
                stall.OwnerOrBoardingType = req.RequesterName;
                stall.HorseName = string.IsNullOrWhiteSpace(req.Comment) ? "Бронь клиента" : req.Comment;
                stall.BreedName = "По заявке";
                stall.ReservedUntil = $"с {req.RequestedDateTime:dd.MM.yyyy}";
                Database.UpdateStall(stall, out _);
                autoDetails = $" | Денник {stall.StallCode} забронирован автоматически";
            }
        }

        if (!Database.UpdateClientRequestStatus(req.Id, "Одобрено", out string err))
        {
            ShowToast($"Ошибка БД: {err}");
            return;
        }

        req.Status = "Одобрено";
        ShowToast($"Заявка от {req.RequesterName} одобрена{autoDetails}");
        ShowClubPage();
    }

    private void OpenNewRequestModal(HorseItem? preselectedHorse = null)
    {
        var readyHorses = _data.Horses.Where(h => h.IsReadyForTraining).ToList();
        if (readyHorses.Count == 0)
        {
            ShowToast("Сейчас нет лошадей с ветеринарным допуском");
            return;
        }

        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            "Заявка на тренировку и манеж",
            "После одобрения администратором манеж и выбранное снаряжение бронируются автоматически"));

        int selectedIndex = 0;
        if (preselectedHorse != null)
        {
            int idx = readyHorses.FindIndex(h => h.Id == preselectedHorse.Id);
            if (idx >= 0) selectedIndex = idx;
        }

        var horseCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = readyHorses,
            SelectedIndex = selectedIndex
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
            PlaceholderText = "Например 16:00"
        };
        timeInput.Classes.Add("compact");

        var chkTrainer = new CheckBox
        {
            Content = "Нужен тренер по конкуру (+1 000 руб.)",
            IsChecked = true,
            Foreground = _textDarkBrown,
            FontSize = 13
        };

        var freeEquip = _data.Equipment.Where(e => e.Status == "Свободно").ToList();
        var equipOptions = new System.Collections.Generic.List<string>
        {
            "Без доп. снаряжения",
            "Автоподбор комплекта (свободный шлем + седло)"
        };
        foreach (var eq in freeEquip)
            equipOptions.Add($"{eq.InventoryCode} {eq.Name} ({eq.PricePerHour:0} руб./ч)");

        var equipCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = equipOptions,
            SelectedIndex = 0
        };
        equipCombo.Classes.Add("compact");

        var commentInput = new TextBox
        {
            PlaceholderText = "Пожелания по занятию (необязательно)"
        };
        commentInput.Classes.Add("compact");

        var costPreview = new TextBlock
        {
            FontSize = 15,
            FontWeight = FontWeight.Bold,
            Foreground = _accentOrange
        };

        int SelectedMinutes() => durationCombo.SelectedIndex switch
        {
            1 => 90,
            2 => 135,
            _ => 45
        };

        decimal SelectedEquipmentCost()
        {
            int idx = equipCombo.SelectedIndex;
            if (idx <= 0)
                return 0m;

            decimal hours = SelectedMinutes() / 45m;
            if (idx == 1)
            {
                decimal hRate = freeEquip.FirstOrDefault(e => e.Category == "Шлем")?.PricePerHour ?? 0m;
                decimal sRate = freeEquip.FirstOrDefault(e => e.Category == "Седло")?.PricePerHour ?? 0m;
                return (hRate + sRate) * hours;
            }

            var item = freeEquip.ElementAtOrDefault(idx - 2);
            return item != null ? item.PricePerHour * hours : 0m;
        }

        string SelectedEquipmentCode()
        {
            int idx = equipCombo.SelectedIndex;
            if (idx <= 0) return string.Empty;
            if (idx == 1) return "AUTO";
            return freeEquip.ElementAtOrDefault(idx - 2)?.InventoryCode ?? string.Empty;
        }

        void RefreshCost()
        {
            decimal trainingCost = ClubLogic.CalculateSessionCost(SelectedMinutes(), chkTrainer.IsChecked == true, 0m);
            decimal eqCost = SelectedEquipmentCost();
            costPreview.Text = eqCost > 0
                ? $"Предварительно: {ClubLogic.FormatMoney(trainingCost)} + снаряжение {ClubLogic.FormatMoney(eqCost)}"
                : $"Предварительная стоимость: {ClubLogic.FormatMoney(trainingCost)}";
        }

        durationCombo.SelectionChanged += (_, _) => RefreshCost();
        chkTrainer.IsCheckedChanged += (_, _) => RefreshCost();
        equipCombo.SelectionChanged += (_, _) => RefreshCost();
        RefreshCost();

        var formRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 12
        };
        var leftField = CreateFormField("Длительность", durationCombo);
        var rightField = CreateFormField("Желаемое время", timeInput);
        Grid.SetColumn(leftField, 0);
        Grid.SetColumn(rightField, 1);
        formRow.Children.Add(leftField);
        formRow.Children.Add(rightField);

        var btnSend = new Button { Content = "Отправить заявку админу" };
        btnSend.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSend.Click += (_, _) =>
        {
            if (horseCombo.SelectedItem is not HorseItem horse)
                return;

            DateTime dt = DateTime.Today.AddHours(16);
            if (TimeSpan.TryParse(timeInput.Text?.Trim(), out var parsed))
                dt = DateTime.Today.Add(parsed);

            int minutes = SelectedMinutes();
            bool hasTrainer = chkTrainer.IsChecked == true;
            decimal cost = ClubLogic.CalculateSessionCost(minutes, hasTrainer, 0m);
            string eqCode = SelectedEquipmentCode();
            string baseComment = (commentInput.Text ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(eqCode) && string.IsNullOrEmpty(baseComment))
                baseComment = eqCode == "AUTO" ? "С автоподбором комплекта снаряжения" : $"Со снаряжением {eqCode}";

            var req = new ClientRequestItem
            {
                Id = _data.NextRequestId++,
                UserId = _data.CurrentUser.Id,
                RequesterName = _data.CurrentUser.FullName,
                RequestType = "Тренировка и манеж",
                TargetName = horse.Nickname,
                HorseId = horse.Id,
                ArenaZone = arenaCombo.SelectedItem?.ToString() ?? "Конкурный манеж",
                DurationMinutes = minutes,
                HasTrainer = hasTrainer,
                RequestedDateTime = dt,
                EstimatedCost = cost,
                Comment = baseComment,
                EquipmentCode = eqCode,
                Status = "На рассмотрении",
                CreatedAt = DateTime.Now
            };

            if (!Database.AddClientRequest(req, out string err))
            {
                ShowToast($"Ошибка отправки: {err}");
                return;
            }

            _data.Requests.Insert(0, req);
            CloseModal();
            ShowToast("Заявка отправлена администратору");
            ShowClubPage();
        };

        box.Children.Add(CreateFormField("Выберите лошадь", horseCombo));
        box.Children.Add(CreateFormField("Манеж / Зона", arenaCombo));
        box.Children.Add(formRow);
        box.Children.Add(chkTrainer);
        box.Children.Add(CreateFormField("Снаряжение (автобронь при одобрении)", equipCombo));
        box.Children.Add(CreateFormField("Комментарий", commentInput));
        box.Children.Add(costPreview);
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnSend }
        });

        ShowModal(box);
    }

    private void OpenEquipmentRequestModal(EquipmentRentalItem item)
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            $"Заявка на снаряжение: {item.Name}",
            $"Инв. N {item.InventoryCode}, размер {item.Size}, тариф {item.PricePerHour:0} руб./час"));

        var timeInput = new TextBox
        {
            Text = DateTime.Now.AddHours(1).ToString("HH:00"),
            PlaceholderText = "Например 16:00"
        };
        timeInput.Classes.Add("compact");

        var commentInput = new TextBox
        {
            PlaceholderText = "На сколько часов или к какому занятию"
        };
        commentInput.Classes.Add("compact");

        var btnSend = new Button { Content = "Отправить запрос" };
        btnSend.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSend.Click += (_, _) =>
        {
            DateTime dt = DateTime.Today.AddHours(16);
            if (TimeSpan.TryParse(timeInput.Text?.Trim(), out var parsed))
                dt = DateTime.Today.Add(parsed);

            var req = new ClientRequestItem
            {
                Id = _data.NextRequestId++,
                UserId = _data.CurrentUser.Id,
                RequesterName = _data.CurrentUser.FullName,
                RequestType = "Аренда снаряжения",
                TargetName = $"{item.InventoryCode} {item.Name}",
                HorseId = null,
                ArenaZone = "Склад амуниции",
                DurationMinutes = 45,
                HasTrainer = false,
                RequestedDateTime = dt,
                EstimatedCost = item.PricePerHour,
                Comment = string.IsNullOrWhiteSpace(commentInput.Text) ? $"Размер {item.Size}" : commentInput.Text.Trim(),
                EquipmentCode = item.InventoryCode,
                Status = "На рассмотрении",
                CreatedAt = DateTime.Now
            };

            if (!Database.AddClientRequest(req, out string err))
            {
                ShowToast($"Ошибка БД: {err}");
                return;
            }

            _data.Requests.Insert(0, req);
            CloseModal();
            ShowToast("Запрос на аренду снаряжения отправлен");
            ShowClubPage();
        };

        box.Children.Add(CreateFormField("К какому времени подготовить", timeInput));
        box.Children.Add(CreateFormField("Комментарий", commentInput));
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnSend }
        });

        ShowModal(box);
    }

    // Вкладка управления ролями, логинами и паролями (только для администратора)

    private Control BuildAdminUsersTab()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 10
        };

        var searchBox = new TextBox
        {
            PlaceholderText = "Поиск пользователя по ФИО, логину или роли...",
            Text = _clubSearchQuery
        };
        searchBox.Classes.Add("compact");
        Grid.SetRow(searchBox, 0);
        grid.Children.Add(searchBox);

        const string cols = "1.35*,0.95*,1.25*,1.35*,235";
        var header = CreateTableHeader(cols, new[]
        {
            ("ФИО пользователя", 0),
            ("Логин", 1),
            ("Текущая роль", 2),
            ("Назначить роль", 3),
            ("Действия", 4)
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

            var filtered = _data.Users.Where(u =>
                string.IsNullOrEmpty(q)
                || u.FullName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || u.Login.Contains(q, StringComparison.OrdinalIgnoreCase)
                || u.RoleName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = "Пользователи не найдены.",
                    Foreground = _textMutedBrown,
                    FontSize = 13,
                    Margin = new Thickness(14, 20)
                });
                return;
            }

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var user = filtered[idx];
                bool isSelf = user.Id == _data.CurrentUser.Id;

                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(cols),
                    ColumnSpacing = 8,
                    Margin = new Thickness(14, 10)
                };

                var cName = new TextBlock
                {
                    Text = isSelf ? $"{user.FullName} (Вы)" : user.FullName,
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var cLogin = new TextBlock
                {
                    Text = $"@{user.Login}",
                    FontSize = 12,
                    Foreground = _textMediumBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var roleBadge = user.RoleId switch
                {
                    1 => CreateBadge(user.RoleName, "#EFEBE9", "#4E342E", "#BCAAA4"),
                    2 => CreateBadge(user.RoleName, "#FFF3E0", "#E65100", "#FFCC80"),
                    _ => CreateBadge(user.RoleName, "#E8F5E9", "#2E7D32", "#A5D6A7")
                };

                var roleOptions = new[]
                {
                    "Клиент",
                    "Администратор манежа",
                    "Главный берейтор-зоотехник"
                };

                var roleCombo = new ComboBox
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    ItemsSource = roleOptions,
                    SelectedIndex = user.RoleId switch
                    {
                        2 => 1,
                        1 => 2,
                        _ => 0
                    },
                    IsEnabled = !isSelf
                };
                roleCombo.Classes.Add("compact");

                var actionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (!isSelf)
                {
                    var btnSaveRole = new Button { Content = "Назначить" };
                    btnSaveRole.Classes.Add("table-btn");
                    btnSaveRole.Click += (_, _) =>
                    {
                        int newRoleId = roleCombo.SelectedIndex switch
                        {
                            1 => 2,
                            2 => 1,
                            _ => 3
                        };
                        string newRoleName = roleOptions[roleCombo.SelectedIndex];

                        if (!Database.UpdateUserRole(user.Id, newRoleId, out string err))
                        {
                            ShowToast($"Ошибка БД: {err}");
                            return;
                        }

                        user.RoleId = newRoleId;
                        user.RoleName = newRoleName;
                        ShowToast($"{user.FullName}: назначена роль '{newRoleName}'");
                        ShowClubPage();
                    };
                    actionPanel.Children.Add(btnSaveRole);
                }

                var btnEditCreds = new Button { Content = "Логин / Пароль" };
                btnEditCreds.Classes.Add("table-secondary");
                btnEditCreds.Click += (_, _) => OpenEditUserCredentialsModal(user);
                actionPanel.Children.Add(btnEditCreds);

                Grid.SetColumn(cName, 0);
                Grid.SetColumn(cLogin, 1);
                Grid.SetColumn(roleBadge, 2);
                Grid.SetColumn(roleCombo, 3);
                Grid.SetColumn(actionPanel, 4);

                rowGrid.Children.Add(cName);
                rowGrid.Children.Add(cLogin);
                rowGrid.Children.Add(roleBadge);
                rowGrid.Children.Add(roleCombo);
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

        PopulateRows();
        return grid;
    }

    private void OpenEditUserCredentialsModal(ManagedUserItem user)
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            $"Учетные данные: {user.FullName}",
            $"Роль: {user.RoleName}. Оставьте пароль пустым, чтобы изменить только логин."));

        var txtLogin = new TextBox
        {
            Text = user.Login,
            PlaceholderText = "Минимум 4 символа..."
        };
        txtLogin.Classes.Add("compact");

        var txtPassword = new TextBox
        {
            PlaceholderText = "Новый пароль (минимум 6 символов)...",
            PasswordChar = '•'
        };
        txtPassword.Classes.Add("compact");

        var btnSave = new Button { Content = "Сохранить" };
        btnSave.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSave.Click += (_, _) =>
        {
            string newLogin = (txtLogin.Text ?? string.Empty).Trim();
            string newPass = txtPassword.Text ?? string.Empty;

            if (newLogin.Length < 4)
            {
                ShowPopupError(txtLogin, "Логин должен быть не менее 4 символов");
                return;
            }
            if (!string.IsNullOrEmpty(newPass) && newPass.Length < 6)
            {
                ShowPopupError(txtPassword, "Пароль должен быть не менее 6 символов");
                return;
            }

            if (_data.Users.Any(u => u.Id != user.Id && u.Login.Equals(newLogin, StringComparison.OrdinalIgnoreCase)))
            {
                ShowPopupError(txtLogin, "Такой логин уже занят");
                return;
            }

            string? passToSave = string.IsNullOrWhiteSpace(newPass) ? null : newPass;
            if (!Database.UpdateUserCredentials(user.Id, newLogin, passToSave, out string err))
            {
                ShowToast($"Ошибка БД: {err}");
                return;
            }

            user.Login = newLogin;
            if (passToSave != null)
                user.OfflinePasswordHash = Database.HashPassword(passToSave);

            if (user.Id == _data.CurrentUser.Id)
            {
                _data.CurrentUser.Login = newLogin;
                UpdateProfilePanel();
            }

            CloseModal();
            ShowToast(passToSave != null
                ? $"{user.FullName}: обновлены логин и пароль"
                : $"{user.FullName}: логин обновлен на @{newLogin}");
            ShowClubPage();
        };

        box.Children.Add(CreateFormField("Логин", txtLogin));
        box.Children.Add(CreateFormField("Новый пароль (необязательно)", txtPassword));
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnSave }
        });

        ShowModal(box);
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
            ColumnDefinitions = IsArenaAdmin
                ? new ColumnDefinitions("*,Auto,Auto")
                : new ColumnDefinitions("*,Auto"),
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

        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(filterCombo, 1);
        toolbar.Children.Add(searchBox);
        toolbar.Children.Add(filterCombo);

        if (IsArenaAdmin)
        {
            var btnNewOrder = new Button { Content = "+ Записать на тренировку" };
            btnNewOrder.Classes.Add("compact");
            btnNewOrder.Click += (_, _) => OpenNewOrderModal();
            Grid.SetColumn(btnNewOrder, 2);
            toolbar.Children.Add(btnNewOrder);
        }

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
                if (IsClient && !o.RiderName.Equals(_data.CurrentUser.FullName, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (_clubStatusFilter != "Все" && o.Status != _clubStatusFilter)
                    return false;
                if (string.IsNullOrEmpty(q))
                    return true;
                string horseNames = string.Join(" ", o.Items.Select(i => i.HorseName));
                return o.OrderNum.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || o.RiderName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || o.ArenaZone.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || horseNames.Contains(q, StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(o => ClubLogic.IsClosedStatus(o.Status) ? 1 : 0)
            .ThenByDescending(o => o.OrderDate)
            .ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = IsClient
                        ? "Подтвержденных занятий пока нет. Отправьте заявку во вкладке «Мои заявки»."
                        : "Записи по выбранному фильтру не найдены.",
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

                if (IsArenaAdmin)
                {
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
                                if (ClubLogic.IsClosedStatus(order.Status))
                                {
                                    foreach (var released in ClubLogic.ReleaseOrderEquipment(_data.Equipment, order.OrderNum))
                                        Database.UpdateEquipmentStatus(released, out _);
                                }
                                if (!Database.UpdateOrderStatus(order, out string dbErr))
                                    ShowToast($"Ошибка БД: {dbErr}");
                                else
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

            if (!Database.AddOrder(newOrder, out string dbErr))
            {
                ShowToast($"Ошибка сохранения в БД: {dbErr}");
                return;
            }

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
                foreach (var released in ClubLogic.ReleaseOrderEquipment(_data.Equipment, order.OrderNum))
                    Database.UpdateEquipmentStatus(released, out _);
                if (!Database.UpdateOrderStatus(order, out string dbErr))
                    ShowToast($"Ошибка БД: {dbErr}");
                else
                    ShowToast($"{order.OrderNum}: отмена по ветпоказаниям");
                CloseModal();
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
        var rentedForOrder = _data.Equipment.Where(e => e.OrderNum == order.OrderNum).ToList();
        string docText = ClubLogic.BuildRouteSheetText(order, rentedForOrder);
        string dateText = order.OrderDate.ToString("dd.MM.yyyy HH:mm");
        string sourceConcat = $"[{order.OrderNum}]+[{dateText}]+[{order.RiderName}]+[{order.TotalPrice:F2}]+[{order.Items.Count}]";
        string sha = ClubLogic.ComputeDocumentSha256(order.OrderNum, order.OrderDate, order.RiderName, order.TotalPrice, order.Items.Count);

        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            "Маршрутный лист тренировки",
            "Акт проверки снаряжения и контроль целостности документа"));

        // 1. Верхняя карточка документа
        var docHeaderGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 12
        };
        var docTitleStack = new StackPanel
        {
            Spacing = 2,
            Children =
            {
                new TextBlock
                {
                    Text = order.OrderNum,
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = $"КСК и ипподром «Аллюр» • от {dateText}",
                    FontSize = 11.5,
                    Foreground = _borderSand
                }
            }
        };
        var statusBadge = CreateOrderStatusBadge(order.Status);
        Grid.SetColumn(docTitleStack, 0);
        Grid.SetColumn(statusBadge, 1);
        docHeaderGrid.Children.Add(docTitleStack);
        docHeaderGrid.Children.Add(statusBadge);

        box.Children.Add(new Border
        {
            Background = _espressoDark,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 11),
            Child = docHeaderGrid
        });

        // 2. Основные реквизиты (2 колонки)
        Border MakeInfoCell(string label, string value, string? subValue = null, bool highlightOrange = false)
        {
            var stack = new StackPanel { Spacing = 2 };
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = _textMutedBrown
            });
            stack.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 13.5,
                FontWeight = FontWeight.Bold,
                Foreground = highlightOrange ? _accentOrange : _textDarkBrown,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrWhiteSpace(subValue))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = subValue,
                    FontSize = 11,
                    Foreground = _textMediumBrown
                });
            }

            return new Border
            {
                Background = Brush.Parse("#FAF6F2"),
                BorderBrush = _borderSand,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(11, 8),
                Child = stack
            };
        }

        var infoGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 10,
            RowSpacing = 10
        };

        var cRider = MakeInfoCell("Всадник (контрагент)", order.RiderName,
            order.RiderDiscount > 0 ? $"Скидка по абонементу: {order.RiderDiscount:0}%" : "Базовый тариф (без скидки)");
        var cZone = MakeInfoCell("Манеж / Зона", order.ArenaZone, $"Позиций в заказе: {order.Items.Count}");
        var cTotal = MakeInfoCell("Итоговая стоимость", ClubLogic.FormatMoney(order.TotalPrice), "С учетом длительности и тренера", highlightOrange: true);
        var cDate = MakeInfoCell("Время занятия", dateText, "Длительность и нагрузка по нормативу");

        Grid.SetRow(cRider, 0); Grid.SetColumn(cRider, 0);
        Grid.SetRow(cZone, 0); Grid.SetColumn(cZone, 1);
        Grid.SetRow(cDate, 1); Grid.SetColumn(cDate, 0);
        Grid.SetRow(cTotal, 1); Grid.SetColumn(cTotal, 1);

        infoGrid.Children.Add(cRider);
        infoGrid.Children.Add(cZone);
        infoGrid.Children.Add(cDate);
        infoGrid.Children.Add(cTotal);
        box.Children.Add(infoGrid);

        if (!string.IsNullOrWhiteSpace(order.CancelReason))
        {
            box.Children.Add(new Border
            {
                Background = Brush.Parse("#FFEBEE"),
                BorderBrush = Brush.Parse("#EF9A9A"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(11, 8),
                Child = new TextBlock
                {
                    Text = $"Причина отмены: {order.CancelReason}",
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brush.Parse("#C62828"),
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }

        // 3. Позиции тренировки
        var sessionsStack = new StackPanel { Spacing = 6 };
        sessionsStack.Children.Add(new TextBlock
        {
            Text = "Позиции тренировки",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = _textDarkBrown
        });

        foreach (var item in order.Items)
        {
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 10
            };
            var left = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Лошадь: {item.HorseName}",
                        FontSize = 13,
                        FontWeight = FontWeight.Bold,
                        Foreground = _textDarkBrown
                    },
                    new TextBlock
                    {
                        Text = $"{item.SessionDateTime:dd.MM.yyyy HH:mm} • {item.DurationMinutes} мин • {(item.HasTrainer ? "С тренером (+1 000 руб.)" : "Без тренера")}",
                        FontSize = 11.5,
                        Foreground = _textMediumBrown
                    }
                }
            };
            var right = new TextBlock
            {
                Text = ClubLogic.FormatMoney(item.ItemCost),
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = _textDarkBrown,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(left, 0);
            Grid.SetColumn(right, 1);
            row.Children.Add(left);
            row.Children.Add(right);

            sessionsStack.Children.Add(new Border
            {
                Background = Brushes.White,
                BorderBrush = _borderSand,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(11, 8),
                Child = row
            });
        }

        if (rentedForOrder.Count > 0)
        {
            var eqText = string.Join(", ", rentedForOrder.Select(e => $"{e.InventoryCode} ({e.Name}, {e.Size})"));
            sessionsStack.Children.Add(new Border
            {
                Background = Brush.Parse("#FFF8F1"),
                BorderBrush = Brush.Parse("#FFCC80"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(11, 8),
                Child = new TextBlock
                {
                    Text = $"Выданное снаряжение: {eqText}",
                    FontSize = 12,
                    Foreground = _textDarkBrown,
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }

        box.Children.Add(sessionsStack);

        // 4. Акт проверки снаряжения и подпись инструктора
        Border MakeCheckRow(string text)
        {
            var r = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    CreateBadge("OK", "#E8F5E9", "#2E7D32", "#A5D6A7"),
                    new TextBlock
                    {
                        Text = text,
                        FontSize = 12,
                        Foreground = _textDarkBrown,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            };
            return new Border { Child = r };
        }

        var actStack = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock
                {
                    Text = "Акт проверки снаряжения",
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textDarkBrown
                },
                MakeCheckRow("Седло и подпруга проверены, приструги зафиксированы"),
                MakeCheckRow("Уздечка и повод исправны, трензель подобран по размеру"),
                MakeCheckRow("Защитный шлем надет и отрегулирован перед выходом в манеж"),
                new Border
                {
                    BorderBrush = _borderSand,
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Padding = new Thickness(0, 8, 0, 0),
                    Margin = new Thickness(0, 4, 0, 0),
                    Child = new TextBlock
                    {
                        Text = "Инструктор по верховой езде: _______________________  (подпись)",
                        FontSize = 12,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = _textMediumBrown
                    }
                }
            }
        };

        box.Children.Add(new Border
        {
            Background = Brush.Parse("#FAF6F2"),
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10),
            Child = actStack
        });

        // 5. Контроль целостности (SHA-256)
        box.Children.Add(new Border
        {
            Background = Brush.Parse("#F5EFE8"),
            BorderBrush = _borderSand,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(11, 8),
            Child = new StackPanel
            {
                Spacing = 3,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Контроль целостности документа (SHA-256)",
                        FontSize = 11,
                        FontWeight = FontWeight.Bold,
                        Foreground = _textMediumBrown
                    },
                    new TextBlock
                    {
                        Text = $"Данные: {sourceConcat}",
                        FontSize = 11,
                        Foreground = _textDarkBrown,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = sha,
                        FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                        FontSize = 10.5,
                        Foreground = _textMutedBrown,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        });

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
            ColumnDefinitions = IsArenaAdmin
                ? new ColumnDefinitions("*,Auto,Auto")
                : new ColumnDefinitions("*,Auto"),
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

        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(filterCombo, 1);
        toolbar.Children.Add(searchBox);
        toolbar.Children.Add(filterCombo);

        if (IsArenaAdmin)
        {
            var btnAddEquip = new Button { Content = "+ Добавить снаряжение" };
            btnAddEquip.Classes.Add("compact");
            btnAddEquip.Click += (_, _) => OpenAddEquipmentModal();
            Grid.SetColumn(btnAddEquip, 2);
            toolbar.Children.Add(btnAddEquip);
        }

        Grid.SetRow(toolbar, 0);
        grid.Children.Add(toolbar);

        const string cols = "85,1.5*,100,95,1.3*,100,115,145";
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
            })
            .OrderBy(e => e.Status == "Свободно" ? 0 : e.Status == "На проверке" ? 1 : 2)
            .ThenBy(e => e.InventoryCode)
            .ToList();

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

                if (IsClient)
                {
                    if (item.Status == "Свободно")
                    {
                        var btnRequest = new Button { Content = "Запросить аренду" };
                        btnRequest.Classes.Add("table-btn");
                        btnRequest.Click += (_, _) => OpenEquipmentRequestModal(item);
                        actionPanel.Children.Add(btnRequest);
                    }
                    else
                    {
                        actionPanel.Children.Add(new TextBlock
                        {
                            Text = "Недоступно",
                            FontSize = 11,
                            Foreground = _textMutedBrown
                        });
                    }
                }
                else if (IsArenaAdmin)
                {
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
                            Database.UpdateEquipmentStatus(item, out _);
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
                            Database.UpdateEquipmentStatus(item, out _);
                            ShowToast($"{item.InventoryCode}: проверка пройдена");
                            ShowClubPage();
                        };
                        actionPanel.Children.Add(btnApprove);
                    }
                }
                else
                {
                    actionPanel.Children.Add(new TextBlock
                    {
                        Text = "Просмотр",
                        FontSize = 11,
                        Foreground = _textMutedBrown
                    });
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
            Database.UpdateEquipmentStatus(item, out _);

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
            var newEquip = new EquipmentRentalItem
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
            };

            if (!Database.AddEquipment(newEquip, out string dbErr))
            {
                ShowToast($"Ошибка БД: {dbErr}");
                return;
            }

            _data.Equipment.Add(newEquip);

            CloseModal();
            ShowToast($"Добавлено снаряжение {newEquip.InventoryCode}");
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
                    Margin = new Thickness(14, 10)
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

            var newRider = new ClubRider
            {
                Id = _data.NextRiderId++,
                FullName = txtName.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? "+7 (900) 000-00-00" : txtPhone.Text.Trim(),
                SkillLevel = skillCombo.SelectedItem?.ToString() ?? "Любитель",
                DiscountRate = disc,
                MembershipName = mName,
                RemainingSessions = rem
            };

            if (!Database.AddRider(newRider, out string dbErr))
            {
                ShowToast($"Ошибка сохранения в БД: {dbErr}");
                return;
            }

            _data.Riders.Add(newRider);
            CloseModal();
            ShowToast($"Добавлен всадник {newRider.FullName}");
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

    private static Border CreateRequestStatusBadge(string status) => status switch
    {
        "Одобрено" => CreateBadge(status, "#E8F5E9", "#2E7D32", "#A5D6A7"),
        "Отклонено" => CreateBadge(status, "#FFEBEE", "#C62828", "#EF9A9A"),
        _ => CreateBadge(status, "#FFF3E0", "#E65100", "#FFCC80")
    };
}
