using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MySqlConnector;

namespace Avalo;

public static class Database
{
    public const string ConnectionString = "Server=localhost;Database=Yes;Uid=root;Pwd=;";

    public static bool IsConnected { get; private set; }
    public static string LastError { get; private set; } = string.Empty;

    public static string HashPassword(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static UserProfile? Login(string login, string password, out bool dbAvailable, out string error)
    {
        dbAvailable = false;
        error = string.Empty;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            dbAvailable = true;
            IsConnected = true;
            LastError = string.Empty;

            const string sql = @"
                SELECT u.id, u.role_id, r.name AS role_name, u.login, u.password_hash, u.full_name
                FROM users u
                JOIN roles r ON r.id = u.role_id
                WHERE u.login = @login
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@login", login);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                error = "Пользователь с таким логином не найден";
                return null;
            }

            string storedHash = reader["password_hash"]?.ToString() ?? string.Empty;
            string inputHash = HashPassword(password);

            bool passwordOk = string.Equals(storedHash, inputHash, StringComparison.OrdinalIgnoreCase)
                || string.Equals(storedHash, password, StringComparison.Ordinal);

            if (!passwordOk)
            {
                error = "Неверный пароль";
                return null;
            }

            return new UserProfile
            {
                Id = Convert.ToInt32(reader["id"]),
                RoleId = Convert.ToInt32(reader["role_id"]),
                RoleName = reader["role_name"]?.ToString() ?? "Администратор манежа",
                Login = reader["login"]?.ToString() ?? login,
                FullName = reader["full_name"]?.ToString() ?? login
            };
        }
        catch (Exception ex)
        {
            IsConnected = false;
            LastError = ex.Message;
            error = ex.Message;
            return null;
        }
    }

    public static UserProfile? Register(string fullName, string login, string password, int roleId, out bool dbAvailable, out string error)
    {
        dbAvailable = false;
        error = string.Empty;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            dbAvailable = true;
            IsConnected = true;
            LastError = string.Empty;

            using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE login = @login;", conn))
            {
                checkCmd.Parameters.AddWithValue("@login", login);
                long count = Convert.ToInt64(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    error = "Такой логин уже зарегистрирован";
                    return null;
                }
            }

            const string insertSql = @"
                INSERT INTO users (role_id, login, password_hash, full_name)
                VALUES (@roleId, @login, @hash, @fullName);";

            long newId;
            using (var insertCmd = new MySqlCommand(insertSql, conn))
            {
                insertCmd.Parameters.AddWithValue("@roleId", roleId);
                insertCmd.Parameters.AddWithValue("@login", login);
                insertCmd.Parameters.AddWithValue("@hash", HashPassword(password));
                insertCmd.Parameters.AddWithValue("@fullName", fullName);
                insertCmd.ExecuteNonQuery();
                newId = insertCmd.LastInsertedId;
            }

            string roleName = roleId == 1 ? "Главный берейтор-зоотехник" : "Администратор манежа";
            using (var roleCmd = new MySqlCommand("SELECT name FROM roles WHERE id = @id LIMIT 1;", conn))
            {
                roleCmd.Parameters.AddWithValue("@id", roleId);
                var result = roleCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    roleName = result.ToString() ?? roleName;
            }

            return new UserProfile
            {
                Id = (int)newId,
                RoleId = roleId,
                RoleName = roleName,
                Login = login,
                FullName = fullName
            };
        }
        catch (Exception ex)
        {
            IsConnected = false;
            LastError = ex.Message;
            error = ex.Message;
            return null;
        }
    }

    public static bool LoadAll(MockClubData data, out string error)
    {
        error = string.Empty;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            IsConnected = true;
            LastError = string.Empty;

            // 1. Роли
            var roles = new List<RoleItem>();
            using (var cmd = new MySqlCommand("SELECT id, name FROM roles ORDER BY id;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader["id"]);
                    string name = reader["name"]?.ToString() ?? string.Empty;
                    roles.Add(new RoleItem
                    {
                        Id = id,
                        Name = name,
                        PermissionsSummary = id == 1
                            ? "Рационы кормления, ветеринарные допуски, бронирование денников"
                            : "Запись всадников на занятия, аренда снаряжения (седла, шлемы), клубные абонементы"
                    });
                }
            }
            if (roles.Count > 0)
            {
                data.Roles.Clear();
                data.Roles.AddRange(roles);
            }

            // 2. Породы лошадей
            var breeds = new List<HorseBreed>();
            using (var cmd = new MySqlCommand("SELECT id, breed_name, temperament, origin_country FROM horse_breeds ORDER BY id;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    breeds.Add(new HorseBreed
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        BreedName = reader["breed_name"]?.ToString() ?? string.Empty,
                        Temperament = reader["temperament"] == DBNull.Value ? string.Empty : reader["temperament"]?.ToString() ?? string.Empty,
                        OriginCountry = reader["origin_country"] == DBNull.Value ? string.Empty : reader["origin_country"]?.ToString() ?? string.Empty
                    });
                }
            }
            data.Breeds.Clear();
            data.Breeds.AddRange(breeds);

            // 3. Лошади
            var horses = new List<HorseItem>();
            const string horsesSql = @"
                SELECT h.id, h.breed_id, hb.breed_name, hb.temperament, hb.origin_country,
                       h.nickname, h.passport_chip_code, h.birth_year, h.suit_color, h.is_ready_for_training
                FROM horses h
                JOIN horse_breeds hb ON hb.id = h.breed_id
                ORDER BY h.id;";

            using (var cmd = new MySqlCommand(horsesSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    horses.Add(new HorseItem
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        BreedId = Convert.ToInt32(reader["breed_id"]),
                        BreedName = reader["breed_name"]?.ToString() ?? string.Empty,
                        Temperament = reader["temperament"] == DBNull.Value ? string.Empty : reader["temperament"]?.ToString() ?? string.Empty,
                        OriginCountry = reader["origin_country"] == DBNull.Value ? string.Empty : reader["origin_country"]?.ToString() ?? string.Empty,
                        Nickname = reader["nickname"]?.ToString() ?? string.Empty,
                        PassportChipCode = reader["passport_chip_code"]?.ToString() ?? string.Empty,
                        BirthYear = Convert.ToInt32(reader["birth_year"]),
                        SuitColor = reader["suit_color"] == DBNull.Value ? string.Empty : reader["suit_color"]?.ToString() ?? string.Empty,
                        IsReadyForTraining = Convert.ToBoolean(reader["is_ready_for_training"])
                    });
                }
            }
            data.Horses.Clear();
            data.Horses.AddRange(horses);
            data.NextHorseId = horses.Count > 0 ? horses.Max(h => h.Id) + 1 : 1;

            // 4. Всадники клуба
            var riders = new List<ClubRider>();
            using (var cmd = new MySqlCommand("SELECT id, full_name, phone, skill_level, discount_rate FROM club_riders ORDER BY id;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    decimal discount = Convert.ToDecimal(reader["discount_rate"]);
                    riders.Add(new ClubRider
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        FullName = reader["full_name"]?.ToString() ?? string.Empty,
                        Phone = reader["phone"]?.ToString() ?? string.Empty,
                        SkillLevel = reader["skill_level"] == DBNull.Value ? "Любитель" : reader["skill_level"]?.ToString() ?? "Любитель",
                        DiscountRate = discount,
                        MembershipName = GetMembershipLabel(discount),
                        RemainingSessions = discount >= 20m ? "Безлимит" : discount > 0m ? "По абонементу" : "-"
                    });
                }
            }
            data.Riders.Clear();
            data.Riders.AddRange(riders);
            data.NextRiderId = riders.Count > 0 ? riders.Max(r => r.Id) + 1 : 1;

            // 5. Заказы и позиции тренировок
            var orders = new List<RidingOrder>();
            const string ordersSql = @"
                SELECT o.id, o.order_num, o.user_id, o.club_rider_id,
                       r.full_name AS rider_name, r.discount_rate,
                       o.order_date, o.total_price, o.status, o.cancel_reason
                FROM riding_orders o
                JOIN club_riders r ON r.id = o.club_rider_id
                ORDER BY o.id DESC;";

            using (var cmd = new MySqlCommand(ordersSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    orders.Add(new RidingOrder
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        OrderNum = reader["order_num"]?.ToString() ?? string.Empty,
                        UserId = Convert.ToInt32(reader["user_id"]),
                        ClubRiderId = Convert.ToInt32(reader["club_rider_id"]),
                        RiderName = reader["rider_name"]?.ToString() ?? string.Empty,
                        RiderDiscount = Convert.ToDecimal(reader["discount_rate"]),
                        OrderDate = Convert.ToDateTime(reader["order_date"]),
                        TotalPrice = Convert.ToDecimal(reader["total_price"]),
                        Status = reader["status"]?.ToString() ?? ClubLogic.OrderStatuses.Booked,
                        CancelReason = reader["cancel_reason"] == DBNull.Value ? null : reader["cancel_reason"]?.ToString(),
                        ArenaZone = "Конкурный манеж"
                    });
                }
            }

            const string itemsSql = @"
                SELECT i.id, i.order_id, i.horse_id, h.nickname AS horse_name,
                       i.session_datetime, i.duration_minutes, i.has_trainer, i.item_cost
                FROM riding_session_items i
                JOIN horses h ON h.id = i.horse_id
                ORDER BY i.id;";

            using (var cmd = new MySqlCommand(itemsSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int orderId = Convert.ToInt32(reader["order_id"]);
                    var parent = orders.FirstOrDefault(o => o.Id == orderId);
                    if (parent == null)
                        continue;

                    parent.Items.Add(new RidingSessionItem
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        OrderId = orderId,
                        HorseId = Convert.ToInt32(reader["horse_id"]),
                        HorseName = reader["horse_name"]?.ToString() ?? string.Empty,
                        SessionDateTime = Convert.ToDateTime(reader["session_datetime"]),
                        DurationMinutes = Convert.ToInt32(reader["duration_minutes"]),
                        HasTrainer = Convert.ToBoolean(reader["has_trainer"]),
                        ItemCost = Convert.ToDecimal(reader["item_cost"])
                    });
                }
            }

            data.Orders.Clear();
            data.Orders.AddRange(orders);

            int maxSeq = 0;
            foreach (var ord in orders)
            {
                int dash = ord.OrderNum.LastIndexOf('-');
                if (dash >= 0 && int.TryParse(ord.OrderNum[(dash + 1)..], out int seq) && seq > maxSeq)
                    maxSeq = seq;
            }
            data.NextOrderSequence = maxSeq + 1;

            // Синхронизируем локальные таблицы рационов и денников с лошадьми из БД
            SyncAuxiliaryTables(data);
            return true;
        }
        catch (Exception ex)
        {
            IsConnected = false;
            LastError = ex.Message;
            error = ex.Message;
            return false;
        }
    }

    public static bool AddOrder(RidingOrder order, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            const string orderSql = @"
                INSERT INTO riding_orders (order_num, user_id, club_rider_id, order_date, total_price, status, cancel_reason)
                VALUES (@num, @uid, @rid, @date, @total, @status, @reason);";

            using (var cmd = new MySqlCommand(orderSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@num", order.OrderNum);
                cmd.Parameters.AddWithValue("@uid", order.UserId);
                cmd.Parameters.AddWithValue("@rid", order.ClubRiderId);
                cmd.Parameters.AddWithValue("@date", order.OrderDate);
                cmd.Parameters.AddWithValue("@total", order.TotalPrice);
                cmd.Parameters.AddWithValue("@status", order.Status);
                cmd.Parameters.AddWithValue("@reason", (object?)order.CancelReason ?? DBNull.Value);
                cmd.ExecuteNonQuery();
                order.Id = (int)cmd.LastInsertedId;
            }

            const string itemSql = @"
                INSERT INTO riding_session_items (order_id, horse_id, session_datetime, duration_minutes, has_trainer, item_cost)
                VALUES (@oid, @hid, @dt, @dur, @trainer, @cost);";

            foreach (var item in order.Items)
            {
                item.OrderId = order.Id;
                using var itemCmd = new MySqlCommand(itemSql, conn, tx);
                itemCmd.Parameters.AddWithValue("@oid", order.Id);
                itemCmd.Parameters.AddWithValue("@hid", item.HorseId);
                itemCmd.Parameters.AddWithValue("@dt", item.SessionDateTime);
                itemCmd.Parameters.AddWithValue("@dur", item.DurationMinutes);
                itemCmd.Parameters.AddWithValue("@trainer", item.HasTrainer ? 1 : 0);
                itemCmd.Parameters.AddWithValue("@cost", item.ItemCost);
                itemCmd.ExecuteNonQuery();
                item.Id = (int)itemCmd.LastInsertedId;
            }

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool UpdateOrderStatus(RidingOrder order, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                UPDATE riding_orders
                SET status = @status, cancel_reason = @reason
                WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@status", order.Status);
            cmd.Parameters.AddWithValue("@reason", (object?)order.CancelReason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", order.Id);
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool AddRider(ClubRider rider, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                INSERT INTO club_riders (full_name, phone, skill_level, discount_rate)
                VALUES (@name, @phone, @skill, @discount);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", rider.FullName);
            cmd.Parameters.AddWithValue("@phone", rider.Phone);
            cmd.Parameters.AddWithValue("@skill", rider.SkillLevel);
            cmd.Parameters.AddWithValue("@discount", rider.DiscountRate);
            cmd.ExecuteNonQuery();
            rider.Id = (int)cmd.LastInsertedId;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool AddHorse(HorseItem horse, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                INSERT INTO horses (breed_id, nickname, passport_chip_code, birth_year, suit_color, is_ready_for_training)
                VALUES (@breedId, @nickname, @chip, @year, @color, @ready);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@breedId", horse.BreedId);
            cmd.Parameters.AddWithValue("@nickname", horse.Nickname);
            cmd.Parameters.AddWithValue("@chip", horse.PassportChipCode);
            cmd.Parameters.AddWithValue("@year", horse.BirthYear);
            cmd.Parameters.AddWithValue("@color", horse.SuitColor);
            cmd.Parameters.AddWithValue("@ready", horse.IsReadyForTraining ? 1 : 0);
            cmd.ExecuteNonQuery();
            horse.Id = (int)cmd.LastInsertedId;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool UpdateHorseVetStatus(int horseId, bool isReady, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = "UPDATE horses SET is_ready_for_training = @ready WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ready", isReady ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", horseId);
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string GetMembershipLabel(decimal discount)
    {
        if (discount >= 20m) return "Клубный безлимит";
        if (discount >= 15m) return "Конкур PRO (12 зан.)";
        if (discount >= 10m) return "Опытный (8 зан.)";
        if (discount >= 5m) return "Базовый (4 зан.)";
        return "Разовый визит";
    }

    private static void SyncAuxiliaryTables(MockClubData data)
    {
        foreach (var horse in data.Horses)
        {
            bool hasRation = data.Rations.Any(r => r.HorseId == horse.Id || r.HorseName == horse.Nickname);
            if (!hasRation)
            {
                data.Rations.Add(new FeedingRationItem
                {
                    Id = horse.Id,
                    HorseId = horse.Id,
                    HorseName = horse.Nickname,
                    StallCode = $"Д-{horse.Id:D2}",
                    MorningFeed = "Овёс 2.0 кг, Сено 4 кг",
                    DayFeed = "Мюсли 1.5 кг, Сено 4 кг",
                    EveningFeed = "Овёс 2.0 кг, Сено 4.5 кг",
                    DietNote = horse.IsReadyForTraining ? "Стандартный спортивный" : "Щадящий (веткарантин)"
                });
            }
        }
    }
}
