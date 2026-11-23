using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalo;

public partial class MainWindow
{
    private string _hippoSearchQuery = string.Empty;
    private string _hippoVetFilter = "Все";
    private string _stallStatusFilter = "Все";

    // Страница Ипподром

    private void ShowHippodromePage()
    {
        BtnHippodrome.Classes.Add("active");
        BtnClub.Classes.Remove("active");

        var root = new Border
        {
            Background = _hippoBg,
            Padding = new Thickness(22, 16)
        };

        var layout = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 12
        };

        int readyCount = _data.Horses.Count(h => h.IsReadyForTraining);
        int quarantineCount = _data.Horses.Count(h => !h.IsReadyForTraining);
        int freeStalls = _data.Stalls.Count(s => s.Status == "Свободен");
        int occupiedStalls = _data.Stalls.Count - freeStalls;

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 16
        };

        string hippoTitle = IsClient
            ? "Ипподром - Лошади клуба и свободные денники"
            : "Ипподром - Конюшня, денники и ветконтроль";

        string hippoSubtitle = IsClient
            ? "Просматривайте допущенных лошадей для записи на тренировку или отправьте заявку на постой"
            : IsZootech
                ? "Управление ветеринарными допусками, распределение денников и рационы кормления"
                : "Справочник лошадей, свободных денников и рационов кормления (редактирует зоотехник)";

        var titleStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 3,
            Children =
            {
                new TextBlock
                {
                    Text = hippoTitle,
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = hippoSubtitle,
                    FontSize = 12,
                    Foreground = _borderSand
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
                CreateKpiCard("Допущены к работе", $"{readyCount} / {_data.Horses.Count}", true),
                CreateKpiCard(IsClient ? "Свободно денников" : "Занято денников",
                    IsClient ? $"{freeStalls} / {_data.Stalls.Count}" : $"{occupiedStalls} / {_data.Stalls.Count}",
                    true),
                CreateKpiCard("На веткарантине", quarantineCount.ToString(), true)
            }
        };

        Grid.SetColumn(titleStack, 0);
        Grid.SetColumn(kpiRow, 1);
        headerGrid.Children.Add(titleStack);
        headerGrid.Children.Add(kpiRow);

        var heroBanner = new Border
        {
            Background = _espressoDark,
            BorderBrush = _textDarkBrown,
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20, 14),
            Child = headerGrid
        };
        Grid.SetRow(heroBanner, 0);
        layout.Children.Add(heroBanner);

        var tabButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };

        if (IsClient)
        {
            if (_hippoTabIndex > 1) _hippoTabIndex = 0;
            tabButtons.Children.Add(CreateHippoSubTabButton("Лошади клуба", 0));
            tabButtons.Children.Add(CreateHippoSubTabButton("Денники и постой", 1));
        }
        else
        {
            if (_hippoTabIndex > 2) _hippoTabIndex = 0;
            tabButtons.Children.Add(CreateHippoSubTabButton("Лошади и ветдопуск", 0));
            tabButtons.Children.Add(CreateHippoSubTabButton("Денники и постой", 1));
            tabButtons.Children.Add(CreateHippoSubTabButton("Рационы кормления", 2));
        }

        var tabBar = new Border
        {
            BorderBrush = _textMutedBrown,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = tabButtons
        };
        Grid.SetRow(tabBar, 1);
        layout.Children.Add(tabBar);

        var tableContainer = new Border
        {
            Background = _hippoCardBg,
            BorderBrush = _textMutedBrown,
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14)
        };

        if (IsClient)
        {
            tableContainer.Child = _hippoTabIndex switch
            {
                0 => BuildHippoHorsesTab(),
                _ => BuildHippoStallsTab()
            };
        }
        else
        {
            tableContainer.Child = _hippoTabIndex switch
            {
                0 => BuildHippoHorsesTab(),
                1 => BuildHippoStallsTab(),
                _ => BuildHippoRationsTab()
            };
        }

        Grid.SetRow(tableContainer, 2);
        layout.Children.Add(tableContainer);

        root.Child = layout;
        MainContent.Content = root;
    }

    private Button CreateHippoSubTabButton(string title, int index)
    {
        var btn = new Button { Content = title };
        btn.Classes.Add("subtab-hippo");
        if (_hippoTabIndex == index)
            btn.Classes.Add("active");

        btn.Click += (_, _) =>
        {
            _hippoTabIndex = index;
            _hippoSearchQuery = string.Empty;
            ShowHippodromePage();
        };
        return btn;
    }

    // Вкладка лошадей и ветеринарного допуска

    private Control BuildHippoHorsesTab()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 10
        };

        var toolbar = new Grid
        {
            ColumnDefinitions = IsZootech
                ? new ColumnDefinitions("*,Auto,Auto")
                : new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 10
        };

        var searchBox = new TextBox
        {
            PlaceholderText = "Поиск лошади по кличке, породе или чипу...",
            Text = _hippoSearchQuery
        };
        searchBox.Classes.Add("compact");

        var filterCombo = new ComboBox
        {
            Width = 190,
            ItemsSource = new[] { "Все", "Допущена", "Веткарантин" },
            SelectedItem = _hippoVetFilter
        };
        filterCombo.Classes.Add("compact");

        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(filterCombo, 1);
        toolbar.Children.Add(searchBox);
        toolbar.Children.Add(filterCombo);

        if (IsZootech)
        {
            var btnAddHorse = new Button { Content = "+ Добавить лошадь" };
            btnAddHorse.Classes.Add("compact");
            btnAddHorse.Click += (_, _) => OpenAddHorseModal();
            Grid.SetColumn(btnAddHorse, 2);
            toolbar.Children.Add(btnAddHorse);
        }

        Grid.SetRow(toolbar, 0);
        grid.Children.Add(toolbar);

        const string cols = "1.15*,1.4*,145,130,130,175";
        var header = CreateTableHeader(cols, new[]
        {
            ("Кличка / Масть", 0),
            ("Порода и темперамент", 1),
            ("Чип / Паспорт", 2),
            ("Нагрузка сегодня", 3),
            ("Ветдопуск", 4),
            (IsClient ? "Запись" : "Управление", 5)
        }, darkStyle: true);

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
            string q = _hippoSearchQuery.Trim();

            var filtered = _data.Horses.Where(h =>
            {
                if (_hippoVetFilter == "Допущена" && !h.IsReadyForTraining)
                    return false;
                if (_hippoVetFilter == "Веткарантин" && h.IsReadyForTraining)
                    return false;
                if (string.IsNullOrEmpty(q))
                    return true;
                return h.Nickname.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || h.BreedName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || h.PassportChipCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || h.SuitColor.Contains(q, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = "Лошади по выбранному фильтру не найдены.",
                    Foreground = _textMutedBrown,
                    FontSize = 13,
                    Margin = new Thickness(14, 20)
                });
                return;
            }

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var horse = filtered[idx];
                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(cols),
                    ColumnSpacing = 8,
                    Margin = new Thickness(14, 10)
                };

                var colName = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = horse.Nickname,
                            FontSize = 14,
                            FontWeight = FontWeight.Bold,
                            Foreground = _textDarkBrown
                        },
                        new TextBlock
                        {
                            Text = $"{horse.BirthYear} г.р., {horse.SuitColor}",
                            FontSize = 11,
                            Foreground = _textMediumBrown
                        }
                    }
                };

                var colBreed = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"{horse.BreedName} ({horse.OriginCountry})",
                            FontSize = 12,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = _textDarkBrown
                        },
                        new TextBlock
                        {
                            Text = horse.Temperament,
                            FontSize = 11,
                            Foreground = _textMutedBrown
                        }
                    }
                };

                var colChip = new TextBlock
                {
                    Text = horse.PassportChipCode,
                    FontSize = 12,
                    Foreground = _textMediumBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                int minutesUsed = _data.GetHorseDailyMinutes(horse.Id, DateTime.Today);
                string loadText = _data.GetHorseDailyHoursText(horse.Id, DateTime.Today);
                var colLoad = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 3,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = loadText,
                            FontSize = 12,
                            FontWeight = FontWeight.Bold,
                            Foreground = minutesUsed >= 180 ? Brush.Parse("#C62828") : _textDarkBrown
                        },
                        new Border
                        {
                            Height = 5,
                            Width = 90,
                            CornerRadius = new CornerRadius(3),
                            Background = Brush.Parse("#E2D5C8"),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Child = new Border
                            {
                                Width = Math.Clamp(minutesUsed / 180.0 * 90.0, 0, 90),
                                CornerRadius = new CornerRadius(3),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                Background = minutesUsed >= 180
                                    ? Brush.Parse("#C62828")
                                    : minutesUsed >= 120
                                        ? _accentOrange
                                        : Brush.Parse("#2E7D32")
                            }
                        }
                    }
                };

                var vetBadge = horse.IsReadyForTraining
                    ? CreateBadge("Допущена", "#E8F5E9", "#2E7D32", "#A5D6A7")
                    : CreateBadge("Веткарантин", "#FFEBEE", "#C62828", "#EF9A9A");

                Control actionControl;
                if (IsZootech)
                {
                    var btnToggleVet = new Button
                    {
                        Content = horse.IsReadyForTraining ? "Снять допуск" : "Выдать допуск"
                    };
                    btnToggleVet.Classes.Add(horse.IsReadyForTraining ? "table-danger" : "table-success");
                    btnToggleVet.Click += (_, _) =>
                    {
                        bool newState = !horse.IsReadyForTraining;
                        if (!Database.UpdateHorseVetStatus(horse.Id, newState, out string dbErr))
                        {
                            ShowToast($"Ошибка БД: {dbErr}");
                            return;
                        }

                        horse.IsReadyForTraining = newState;
                        ShowToast(horse.IsReadyForTraining
                            ? $"{horse.Nickname}: допущена к тренировкам"
                            : $"{horse.Nickname}: отправлена на веткарантин (запись в манеж закрыта)");
                        ShowHippodromePage();
                    };
                    actionControl = btnToggleVet;
                }
                else if (IsClient)
                {
                    if (horse.IsReadyForTraining)
                    {
                        var btnReqTrain = new Button { Content = "Запросить занятие" };
                        btnReqTrain.Classes.Add("table-btn");
                        btnReqTrain.Click += (_, _) => OpenNewRequestModal(horse);
                        actionControl = btnReqTrain;
                    }
                    else
                    {
                        actionControl = new TextBlock
                        {
                            Text = "На отдыхе",
                            FontSize = 11,
                            Foreground = _textMutedBrown,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                    }
                }
                else
                {
                    actionControl = new TextBlock
                    {
                        Text = "Только просмотр",
                        FontSize = 11,
                        Foreground = _textMutedBrown,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }

                Grid.SetColumn(colName, 0);
                Grid.SetColumn(colBreed, 1);
                Grid.SetColumn(colChip, 2);
                Grid.SetColumn(colLoad, 3);
                Grid.SetColumn(vetBadge, 4);
                Grid.SetColumn(actionControl, 5);

                rowGrid.Children.Add(colName);
                rowGrid.Children.Add(colBreed);
                rowGrid.Children.Add(colChip);
                rowGrid.Children.Add(colLoad);
                rowGrid.Children.Add(vetBadge);
                rowGrid.Children.Add(actionControl);

                rowsHost.Children.Add(new Border
                {
                    Background = idx % 2 == 0 ? _hippoCardBg : Brush.Parse("#F3ECE4"),
                    BorderBrush = Brush.Parse("#E2D5C8"),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Child = rowGrid
                });
            }
        }

        searchBox.TextChanged += (_, _) =>
        {
            _hippoSearchQuery = searchBox.Text ?? string.Empty;
            PopulateRows();
        };

        filterCombo.SelectionChanged += (_, _) =>
        {
            _hippoVetFilter = filterCombo.SelectedItem?.ToString() ?? "Все";
            PopulateRows();
        };

        PopulateRows();
        return grid;
    }

    private void OpenAddHorseModal()
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader("Новая лошадь в конюшне", "Добавление карточки лошади и ветдопуска"));

        var txtNickname = new TextBox { PlaceholderText = "Кличка (например: Фаворит)" };
        txtNickname.Classes.Add("compact");

        var breedCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = _data.Breeds,
            SelectedIndex = 0
        };
        breedCombo.Classes.Add("compact");

        var txtChip = new TextBox { Text = $"CHIP-RU-{DateTime.Now.Year}-{100 + _data.NextHorseId * 37}" };
        txtChip.Classes.Add("compact");

        var txtYear = new TextBox { Text = "2020" };
        txtYear.Classes.Add("compact");

        var txtColor = new TextBox { PlaceholderText = "Масть (например: Гнедая)" };
        txtColor.Classes.Add("compact");

        var chkReady = new CheckBox
        {
            Content = "Ветеринарный допуск к тренировкам подтвержден",
            IsChecked = true,
            Foreground = _textDarkBrown,
            FontSize = 13
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 12
        };
        var fYear = CreateFormField("Год рождения", txtYear);
        var fColor = CreateFormField("Масть", txtColor);
        Grid.SetColumn(fYear, 0);
        Grid.SetColumn(fColor, 1);
        row.Children.Add(fYear);
        row.Children.Add(fColor);

        var btnSave = new Button { Content = "Сохранить лошадь" };
        btnSave.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSave.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtNickname.Text))
            {
                ShowPopupError(txtNickname, "Введите кличку");
                return;
            }
            if (breedCombo.SelectedItem is not HorseBreed breed)
                return;

            int.TryParse(txtYear.Text, out int birthYear);
            if (birthYear < 1995 || birthYear > DateTime.Now.Year)
                birthYear = 2020;

            int id = _data.NextHorseId++;
            string nickname = txtNickname.Text.Trim();

            var newHorse = new HorseItem
            {
                Id = id,
                BreedId = breed.Id,
                BreedName = breed.BreedName,
                Temperament = breed.Temperament,
                OriginCountry = breed.OriginCountry,
                Nickname = nickname,
                PassportChipCode = string.IsNullOrWhiteSpace(txtChip.Text) ? $"CHIP-RU-{id:D4}" : txtChip.Text.Trim(),
                BirthYear = birthYear,
                SuitColor = string.IsNullOrWhiteSpace(txtColor.Text) ? "Гнедая" : txtColor.Text.Trim(),
                IsReadyForTraining = chkReady.IsChecked == true
            };

            if (!Database.AddHorse(newHorse, out string dbErr))
            {
                ShowToast($"Ошибка сохранения в БД: {dbErr}");
                return;
            }

            _data.Horses.Add(newHorse);

            _data.Rations.Add(new FeedingRationItem
            {
                Id = newHorse.Id,
                HorseId = newHorse.Id,
                HorseName = nickname,
                StallCode = "Без денника",
                MorningFeed = "Овёс 2.0 кг, Сено 4 кг",
                DayFeed = "Мюсли 1.0 кг, Сено 4 кг",
                EveningFeed = "Овёс 2.0 кг, Сено 4.5 кг",
                DietNote = "Базовый рацион"
            });

            CloseModal();
            ShowToast($"Лошадь {nickname} добавлена в реестр");
            ShowHippodromePage();
        };

        box.Children.Add(CreateFormField("Кличка лошади", txtNickname));
        box.Children.Add(CreateFormField("Порода", breedCombo));
        box.Children.Add(CreateFormField("Паспортный чип", txtChip));
        box.Children.Add(row);
        box.Children.Add(chkReady);
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnSave }
        });

        ShowModal(box);
    }

    // Вкладка денников и постоя

    private Control BuildHippoStallsTab()
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
            PlaceholderText = "Поиск по номеру денника, кличке лошади или владельцу...",
            Text = _hippoSearchQuery
        };
        searchBox.Classes.Add("compact");

        var filterCombo = new ComboBox
        {
            Width = 185,
            ItemsSource = new[] { "Все", "Свободен", "Занят", "Забронирован" },
            SelectedItem = _stallStatusFilter
        };
        filterCombo.Classes.Add("compact");

        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(filterCombo, 1);
        toolbar.Children.Add(searchBox);
        toolbar.Children.Add(filterCombo);

        Grid.SetRow(toolbar, 0);
        grid.Children.Add(toolbar);

        const string cols = "80,1.2*,1.2*,1.35*,115,115,155";
        var header = CreateTableHeader(cols, new[]
        {
            ("Денник", 0),
            ("Крыло и тип", 1),
            ("Лошадь / Порода", 2),
            ("Владелец / Постой", 3),
            ("Срок", 4),
            ("Статус", 5),
            ("Действие", 6)
        }, darkStyle: true);

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
            string q = _hippoSearchQuery.Trim();

            var filtered = _data.Stalls.Where(s =>
            {
                if (_stallStatusFilter != "Все" && s.Status != _stallStatusFilter)
                    return false;
                if (string.IsNullOrEmpty(q))
                    return true;
                return s.StallCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || s.HorseName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || s.OwnerOrBoardingType.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || s.WingSection.Contains(q, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = "Денники по выбранному фильтру не найдены.",
                    Foreground = _textMutedBrown,
                    FontSize = 13,
                    Margin = new Thickness(14, 20)
                });
                return;
            }

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var stall = filtered[idx];
                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(cols),
                    ColumnSpacing = 8,
                    Margin = new Thickness(14, 10)
                };

                var cCode = new TextBlock
                {
                    Text = stall.StallCode,
                    FontSize = 13,
                    FontWeight = FontWeight.Bold,
                    Foreground = _textDarkBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var cType = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock { Text = stall.WingSection, FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = _textDarkBrown },
                        new TextBlock { Text = stall.StallType, FontSize = 11, Foreground = _textMutedBrown }
                    }
                };

                var cHorse = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = stall.HorseName,
                            FontSize = 12,
                            FontWeight = stall.Status == "Свободен" ? FontWeight.Normal : FontWeight.SemiBold,
                            Foreground = stall.Status == "Свободен" ? _textMutedBrown : _textDarkBrown
                        },
                        new TextBlock { Text = stall.BreedName, FontSize = 11, Foreground = _textMutedBrown }
                    }
                };

                var cOwner = new TextBlock
                {
                    Text = stall.OwnerOrBoardingType,
                    FontSize = 12,
                    Foreground = _textDarkBrown,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var cTerm = new TextBlock
                {
                    Text = stall.ReservedUntil,
                    FontSize = 12,
                    Foreground = _textMediumBrown,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var statusBadge = stall.Status switch
                {
                    "Свободен" => CreateBadge("Свободен", "#E8F5E9", "#2E7D32", "#A5D6A7"),
                    "Забронирован" => CreateBadge("Забронирован", "#FFF3E0", "#E65100", "#FFCC80"),
                    _ => CreateBadge("Занят", "#EFEBE9", "#4E342E", "#BCAAA4")
                };

                var actionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (IsClient)
                {
                    if (stall.Status == "Свободен")
                    {
                        var btnReqStall = new Button { Content = "Заявка на постой" };
                        btnReqStall.Classes.Add("table-btn");
                        btnReqStall.Click += (_, _) => OpenStallRequestModal(stall);
                        actionPanel.Children.Add(btnReqStall);
                    }
                    else
                    {
                        actionPanel.Children.Add(new TextBlock
                        {
                            Text = "Занято",
                            FontSize = 11,
                            Foreground = _textMutedBrown
                        });
                    }
                }
                else if (IsZootech)
                {
                    if (stall.Status == "Свободен")
                    {
                        var btnBook = new Button { Content = "Бронировать" };
                        btnBook.Classes.Add("table-btn");
                        btnBook.Click += (_, _) => OpenBookStallModal(stall);
                        actionPanel.Children.Add(btnBook);
                    }
                    else
                    {
                        var btnFree = new Button { Content = "Освободить" };
                        btnFree.Classes.Add("table-secondary");
                        btnFree.Click += (_, _) =>
                        {
                            stall.Status = "Свободен";
                            stall.HorseId = null;
                            stall.HorseName = "Свободен";
                            stall.BreedName = "-";
                            stall.OwnerOrBoardingType = "-";
                            stall.ReservedUntil = "-";
                            ShowToast($"Денник {stall.StallCode} освобожден");
                            ShowHippodromePage();
                        };
                        actionPanel.Children.Add(btnFree);
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

                Grid.SetColumn(cCode, 0);
                Grid.SetColumn(cType, 1);
                Grid.SetColumn(cHorse, 2);
                Grid.SetColumn(cOwner, 3);
                Grid.SetColumn(cTerm, 4);
                Grid.SetColumn(statusBadge, 5);
                Grid.SetColumn(actionPanel, 6);

                rowGrid.Children.Add(cCode);
                rowGrid.Children.Add(cType);
                rowGrid.Children.Add(cHorse);
                rowGrid.Children.Add(cOwner);
                rowGrid.Children.Add(cTerm);
                rowGrid.Children.Add(statusBadge);
                rowGrid.Children.Add(actionPanel);

                rowsHost.Children.Add(new Border
                {
                    Background = idx % 2 == 0 ? _hippoCardBg : Brush.Parse("#F3ECE4"),
                    BorderBrush = Brush.Parse("#E2D5C8"),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Child = rowGrid
                });
            }
        }

        searchBox.TextChanged += (_, _) =>
        {
            _hippoSearchQuery = searchBox.Text ?? string.Empty;
            PopulateRows();
        };

        filterCombo.SelectionChanged += (_, _) =>
        {
            _stallStatusFilter = filterCombo.SelectedItem?.ToString() ?? "Все";
            PopulateRows();
        };

        PopulateRows();
        return grid;
    }

    private void OpenStallRequestModal(StallItem stall)
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            $"Заявка на постой: денник {stall.StallCode}",
            $"{stall.WingSection}, тип: {stall.StallType}"));

        var txtHorse = new TextBox { PlaceholderText = "Кличка вашей лошади или порода" };
        txtHorse.Classes.Add("compact");

        var txtComment = new TextBox { PlaceholderText = "Предполагаемый срок заезда и пожелания" };
        txtComment.Classes.Add("compact");

        var btnSend = new Button { Content = "Отправить заявку админу" };
        btnSend.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSend.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtHorse.Text))
            {
                ShowPopupError(txtHorse, "Укажите кличку лошади");
                return;
            }

            var req = new ClientRequestItem
            {
                Id = _data.NextRequestId++,
                UserId = _data.CurrentUser.Id,
                RequesterName = _data.CurrentUser.FullName,
                RequestType = "Бронь денника",
                TargetName = $"{stall.StallCode} ({stall.WingSection})",
                HorseId = null,
                ArenaZone = stall.StallType,
                DurationMinutes = 0,
                HasTrainer = false,
                RequestedDateTime = DateTime.Today.AddDays(1),
                EstimatedCost = 0m,
                Comment = string.IsNullOrWhiteSpace(txtComment.Text)
                    ? txtHorse.Text.Trim()
                    : $"{txtHorse.Text.Trim()} — {txtComment.Text.Trim()}",
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
            ShowToast($"Заявка на денник {stall.StallCode} отправлена администратору");
            ShowHippodromePage();
        };

        box.Children.Add(CreateFormField("Лошадь для постоя", txtHorse));
        box.Children.Add(CreateFormField("Комментарий / срок", txtComment));
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnSend }
        });

        ShowModal(box);
    }

    private void OpenBookStallModal(StallItem stall)
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            $"Бронирование денника {stall.StallCode}",
            $"{stall.WingSection}, тип: {stall.StallType}"));

        var txtHorse = new TextBox { PlaceholderText = "Кличка лошади (например: Ветер)" };
        txtHorse.Classes.Add("compact");

        var txtBreed = new TextBox { PlaceholderText = "Порода (например: Будённовская)" };
        txtBreed.Classes.Add("compact");

        var txtOwner = new TextBox { PlaceholderText = "Владелец или пометка «КСК Аллюр»" };
        txtOwner.Classes.Add("compact");

        var txtTerm = new TextBox { Text = "до 31.12.2026" };
        txtTerm.Classes.Add("compact");

        var statusCombo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "Забронирован", "Занят" },
            SelectedIndex = 0
        };
        statusCombo.Classes.Add("compact");

        var btnSave = new Button { Content = "Подтвердить бронь" };
        btnSave.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSave.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtHorse.Text))
            {
                ShowPopupError(txtHorse, "Укажите кличку лошади");
                return;
            }

            stall.HorseName = txtHorse.Text.Trim();
            stall.BreedName = string.IsNullOrWhiteSpace(txtBreed.Text) ? "Спортивная" : txtBreed.Text.Trim();
            stall.OwnerOrBoardingType = string.IsNullOrWhiteSpace(txtOwner.Text) ? "Частный постой" : txtOwner.Text.Trim();
            stall.ReservedUntil = string.IsNullOrWhiteSpace(txtTerm.Text) ? "Постоянный" : txtTerm.Text.Trim();
            stall.Status = statusCombo.SelectedItem?.ToString() ?? "Забронирован";

            var ration = _data.Rations.FirstOrDefault(r => r.HorseName.Equals(stall.HorseName, StringComparison.OrdinalIgnoreCase));
            if (ration != null)
                ration.StallCode = stall.StallCode;

            CloseModal();
            ShowToast($"Денник {stall.StallCode}: оформлен ({stall.Status})");
            ShowHippodromePage();
        };

        box.Children.Add(CreateFormField("Кличка лошади", txtHorse));
        box.Children.Add(CreateFormField("Порода", txtBreed));
        box.Children.Add(CreateFormField("Владелец / Тип постоя", txtOwner));
        box.Children.Add(CreateFormField("Срок", txtTerm));
        box.Children.Add(CreateFormField("Статус размещения", statusCombo));
        box.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { btnCancel, btnSave }
        });

        ShowModal(box);
    }

    // Вкладка рационов кормления

    private Control BuildHippoRationsTab()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 10
        };

        var searchBox = new TextBox
        {
            PlaceholderText = "Поиск рациона по кличке лошади, деннику или корму...",
            Text = _hippoSearchQuery
        };
        searchBox.Classes.Add("compact");
        Grid.SetRow(searchBox, 0);
        grid.Children.Add(searchBox);

        const string cols = "1.05*,1.25*,1.25*,1.25*,1.1*,120";
        var header = CreateTableHeader(cols, new[]
        {
            ("Лошадь / Денник", 0),
            ("Утро (07:00)", 1),
            ("День (13:00)", 2),
            ("Вечер (19:00)", 3),
            ("Режим / Заметки", 4),
            ("Действие", 5)
        }, darkStyle: true);

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
            string q = _hippoSearchQuery.Trim();

            var filtered = _data.Rations.Where(r =>
                string.IsNullOrEmpty(q)
                || r.HorseName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.StallCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.DietNote.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.MorningFeed.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            if (filtered.Count == 0)
            {
                rowsHost.Children.Add(new TextBlock
                {
                    Text = "Рационы по выбранному запросу не найдены.",
                    Foreground = _textMutedBrown,
                    FontSize = 13,
                    Margin = new Thickness(14, 20)
                });
                return;
            }

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var ration = filtered[idx];
                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(cols),
                    ColumnSpacing = 8,
                    Margin = new Thickness(14, 10)
                };

                var cHorse = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock { Text = ration.HorseName, FontSize = 13, FontWeight = FontWeight.Bold, Foreground = _textDarkBrown },
                        new TextBlock { Text = $"Денник: {ration.StallCode}", FontSize = 11, Foreground = _textMediumBrown }
                    }
                };

                var cMorning = new TextBlock { Text = ration.MorningFeed, FontSize = 12, Foreground = _textDarkBrown, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
                var cDay = new TextBlock { Text = ration.DayFeed, FontSize = 12, Foreground = _textDarkBrown, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
                var cEvening = new TextBlock { Text = ration.EveningFeed, FontSize = 12, Foreground = _textDarkBrown, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
                var cNote = CreateBadge(
                    ration.DietNote,
                    ration.DietNote.Contains("вет", StringComparison.OrdinalIgnoreCase) ? "#FFEBEE" : "#EFEBE9",
                    ration.DietNote.Contains("вет", StringComparison.OrdinalIgnoreCase) ? "#C62828" : "#4E342E");

                Control actionControl;
                if (IsZootech)
                {
                    var btnEdit = new Button { Content = "Изменить" };
                    btnEdit.Classes.Add("table-secondary");
                    btnEdit.Click += (_, _) => OpenEditRationModal(ration);
                    actionControl = btnEdit;
                }
                else
                {
                    actionControl = new TextBlock
                    {
                        Text = "Просмотр",
                        FontSize = 11,
                        Foreground = _textMutedBrown,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }

                Grid.SetColumn(cHorse, 0);
                Grid.SetColumn(cMorning, 1);
                Grid.SetColumn(cDay, 2);
                Grid.SetColumn(cEvening, 3);
                Grid.SetColumn(cNote, 4);
                Grid.SetColumn(actionControl, 5);

                rowGrid.Children.Add(cHorse);
                rowGrid.Children.Add(cMorning);
                rowGrid.Children.Add(cDay);
                rowGrid.Children.Add(cEvening);
                rowGrid.Children.Add(cNote);
                rowGrid.Children.Add(actionControl);

                rowsHost.Children.Add(new Border
                {
                    Background = idx % 2 == 0 ? _hippoCardBg : Brush.Parse("#F3ECE4"),
                    BorderBrush = Brush.Parse("#E2D5C8"),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Child = rowGrid
                });
            }
        }

        searchBox.TextChanged += (_, _) =>
        {
            _hippoSearchQuery = searchBox.Text ?? string.Empty;
            PopulateRows();
        };

        PopulateRows();
        return grid;
    }

    private void OpenEditRationModal(FeedingRationItem ration)
    {
        var box = new StackPanel { Spacing = 12 };
        box.Children.Add(CreateModalHeader(
            $"Рацион: {ration.HorseName}",
            $"Денник {ration.StallCode}, корректировка норм кормления"));

        var txtMorning = new TextBox { Text = ration.MorningFeed };
        txtMorning.Classes.Add("compact");

        var txtDay = new TextBox { Text = ration.DayFeed };
        txtDay.Classes.Add("compact");

        var txtEvening = new TextBox { Text = ration.EveningFeed };
        txtEvening.Classes.Add("compact");

        var txtNote = new TextBox { Text = ration.DietNote };
        txtNote.Classes.Add("compact");

        var btnSave = new Button { Content = "Сохранить рацион" };
        btnSave.Classes.Add("compact");
        var btnCancel = new Button { Content = "Отмена" };
        btnCancel.Classes.Add("secondary");
        btnCancel.Click += (_, _) => CloseModal();

        btnSave.Click += (_, _) =>
        {
            ration.MorningFeed = (txtMorning.Text ?? string.Empty).Trim();
            ration.DayFeed = (txtDay.Text ?? string.Empty).Trim();
            ration.EveningFeed = (txtEvening.Text ?? string.Empty).Trim();
            ration.DietNote = string.IsNullOrWhiteSpace(txtNote.Text) ? "Стандартный" : txtNote.Text.Trim();

            CloseModal();
            ShowToast($"Рацион для {ration.HorseName} обновлен");
            ShowHippodromePage();
        };

        box.Children.Add(CreateFormField("Утро (07:00)", txtMorning));
        box.Children.Add(CreateFormField("День (13:00)", txtDay));
        box.Children.Add(CreateFormField("Вечер (19:00)", txtEvening));
        box.Children.Add(CreateFormField("Режим / Ветеринарные пометки", txtNote));
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
