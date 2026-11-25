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

    private static void EnsureSchema(MySqlConnection conn)
    {
        const string roleSql = @"
            INSERT IGNORE INTO roles (id, name) VALUES
            (1, 'Главный берейтор-зоотехник'),
            (2, 'Администратор манежа'),
            (3, 'Клиент');";

        using (var roleCmd = new MySqlCommand(roleSql, conn))
        {
            roleCmd.ExecuteNonQuery();
        }

        const string reqTableSql = @"
            CREATE TABLE IF NOT EXISTS client_requests (
              id int(11) NOT NULL AUTO_INCREMENT,
              user_id int(11) NOT NULL,
              requester_name varchar(150) NOT NULL,
              request_type varchar(60) NOT NULL,
              target_name varchar(150) NOT NULL,
              horse_id int(11) DEFAULT NULL,
              arena_zone varchar(100) NOT NULL DEFAULT 'Конкурный манеж',
              duration_minutes int(11) NOT NULL DEFAULT 45,
              has_trainer tinyint(1) NOT NULL DEFAULT 1,
              requested_datetime datetime NOT NULL,
              estimated_cost decimal(10,2) NOT NULL DEFAULT 0.00,
              comment varchar(255) DEFAULT NULL,
              equipment_code varchar(40) DEFAULT NULL,
              status varchar(40) NOT NULL DEFAULT 'На рассмотрении',
              created_at datetime NOT NULL DEFAULT current_timestamp(),
              PRIMARY KEY (id),
              KEY fk_requests_user (user_id),
              CONSTRAINT fk_requests_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE ON UPDATE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;";

        using (var reqCmd = new MySqlCommand(reqTableSql, conn))
        {
            reqCmd.ExecuteNonQuery();
        }

        const string checkColSql = @"
            SELECT COUNT(*)
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'client_requests'
              AND COLUMN_NAME = 'equipment_code';";

        using (var colCheckCmd = new MySqlCommand(checkColSql, conn))
        {
            long hasCol = Convert.ToInt64(colCheckCmd.ExecuteScalar());
            if (hasCol == 0)
            {
                using var alterCmd = new MySqlCommand("ALTER TABLE client_requests ADD COLUMN equipment_code varchar(40) DEFAULT NULL;", conn);
                alterCmd.ExecuteNonQuery();
            }
        }

        const string equipTableSql = @"
            CREATE TABLE IF NOT EXISTS equipment_items (
              id int(11) NOT NULL AUTO_INCREMENT,
              inventory_code varchar(30) NOT NULL,
              name varchar(150) NOT NULL,
              category varchar(60) NOT NULL,
              size varchar(60) NOT NULL,
              price_per_hour decimal(10,2) NOT NULL DEFAULT 0.00,
              status varchar(40) NOT NULL DEFAULT 'Свободно',
              rented_by_rider varchar(150) NOT NULL DEFAULT '-',
              order_num varchar(40) NOT NULL DEFAULT '-',
              PRIMARY KEY (id),
              UNIQUE KEY inventory_code (inventory_code)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;";

        using (var eqCmd = new MySqlCommand(equipTableSql, conn))
        {
            eqCmd.ExecuteNonQuery();
        }

        const string stallsTableSql = @"
            CREATE TABLE IF NOT EXISTS stalls (
              id int(11) NOT NULL AUTO_INCREMENT,
              stall_code varchar(30) NOT NULL,
              wing_section varchar(80) NOT NULL,
              stall_type varchar(80) NOT NULL,
              horse_id int(11) DEFAULT NULL,
              horse_name varchar(120) NOT NULL DEFAULT 'Свободен',
              breed_name varchar(100) NOT NULL DEFAULT '-',
              owner_or_boarding_type varchar(150) NOT NULL DEFAULT '-',
              reserved_until varchar(60) NOT NULL DEFAULT '-',
              status varchar(40) NOT NULL DEFAULT 'Свободен',
              PRIMARY KEY (id),
              UNIQUE KEY stall_code (stall_code),
              KEY fk_stalls_horse (horse_id),
              CONSTRAINT fk_stalls_horse FOREIGN KEY (horse_id) REFERENCES horses (id) ON DELETE SET NULL ON UPDATE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;";

        using (var stCmd = new MySqlCommand(stallsTableSql, conn))
        {
            stCmd.ExecuteNonQuery();
        }

        const string rationsTableSql = @"
            CREATE TABLE IF NOT EXISTS feeding_rations (
              id int(11) NOT NULL AUTO_INCREMENT,
              horse_id int(11) NOT NULL,
              horse_name varchar(120) NOT NULL,
              stall_code varchar(30) NOT NULL,
              morning_feed varchar(200) NOT NULL,
              day_feed varchar(200) NOT NULL,
              evening_feed varchar(200) NOT NULL,
              diet_note varchar(120) NOT NULL,
              PRIMARY KEY (id),
              KEY fk_rations_horse (horse_id),
              CONSTRAINT fk_rations_horse FOREIGN KEY (horse_id) REFERENCES horses (id) ON DELETE CASCADE ON UPDATE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;";

        using var ratCmd = new MySqlCommand(rationsTableSql, conn);
        ratCmd.ExecuteNonQuery();
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
            EnsureSchema(conn);

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
                RoleName = reader["role_name"]?.ToString() ?? "Клиент",
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

    public static UserProfile? RegisterClient(string fullName, string login, string password, out bool dbAvailable, out string error)
    {
        dbAvailable = false;
        error = string.Empty;
        const int clientRoleId = 3;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            dbAvailable = true;
            IsConnected = true;
            LastError = string.Empty;
            EnsureSchema(conn);

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
                insertCmd.Parameters.AddWithValue("@roleId", clientRoleId);
                insertCmd.Parameters.AddWithValue("@login", login);
                insertCmd.Parameters.AddWithValue("@hash", HashPassword(password));
                insertCmd.Parameters.AddWithValue("@fullName", fullName);
                insertCmd.ExecuteNonQuery();
                newId = insertCmd.LastInsertedId;
            }

            return new UserProfile
            {
                Id = (int)newId,
                RoleId = clientRoleId,
                RoleName = "Клиент",
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
            EnsureSchema(conn);

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
                        PermissionsSummary = id switch
                        {
                            1 => "Рационы кормления, ветеринарные допуски, бронирование денников",
                            2 => "Запись всадников на занятия, аренда снаряжения, одобрение заявок и выдача ролей",
                            _ => "Отправка заявок на тренировку, коня, аренду манежа, снаряжения и постоя"
                        }
                    });
                }
            }
            if (roles.Count > 0)
            {
                data.Roles.Clear();
                data.Roles.AddRange(roles);
            }

            // 2. Пользователи (для выдачи ролей админом)
            var users = new List<ManagedUserItem>();
            const string usersSql = @"
                SELECT u.id, u.role_id, r.name AS role_name, u.login, u.full_name
                FROM users u
                JOIN roles r ON r.id = u.role_id
                ORDER BY u.id;";

            using (var cmd = new MySqlCommand(usersSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new ManagedUserItem
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        RoleId = Convert.ToInt32(reader["role_id"]),
                        RoleName = reader["role_name"]?.ToString() ?? string.Empty,
                        Login = reader["login"]?.ToString() ?? string.Empty,
                        FullName = reader["full_name"]?.ToString() ?? string.Empty
                    });
                }
            }
            data.Users.Clear();
            data.Users.AddRange(users);

            // 3. Породы лошадей
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

            // 4. Лошади
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

            // 5. Всадники клуба
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

            // 6. Заказы и позиции тренировок
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

            // 7. Заявки пользователей
            var requests = new List<ClientRequestItem>();
            const string reqSql = @"
                SELECT id, user_id, requester_name, request_type, target_name, horse_id,
                       arena_zone, duration_minutes, has_trainer, requested_datetime,
                       estimated_cost, comment, equipment_code, status, created_at
                FROM client_requests
                ORDER BY id DESC;";

            using (var cmd = new MySqlCommand(reqSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    requests.Add(new ClientRequestItem
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        UserId = Convert.ToInt32(reader["user_id"]),
                        RequesterName = reader["requester_name"]?.ToString() ?? string.Empty,
                        RequestType = reader["request_type"]?.ToString() ?? "Тренировка и манеж",
                        TargetName = reader["target_name"]?.ToString() ?? string.Empty,
                        HorseId = reader["horse_id"] == DBNull.Value ? null : Convert.ToInt32(reader["horse_id"]),
                        ArenaZone = reader["arena_zone"]?.ToString() ?? "Конкурный манеж",
                        DurationMinutes = Convert.ToInt32(reader["duration_minutes"]),
                        HasTrainer = Convert.ToBoolean(reader["has_trainer"]),
                        RequestedDateTime = Convert.ToDateTime(reader["requested_datetime"]),
                        EstimatedCost = Convert.ToDecimal(reader["estimated_cost"]),
                        Comment = reader["comment"] == DBNull.Value ? string.Empty : reader["comment"]?.ToString() ?? string.Empty,
                        EquipmentCode = reader["equipment_code"] == DBNull.Value ? string.Empty : reader["equipment_code"]?.ToString() ?? string.Empty,
                        Status = reader["status"]?.ToString() ?? "На рассмотрении",
                        CreatedAt = Convert.ToDateTime(reader["created_at"])
                    });
                }
            }
            data.Requests.Clear();
            data.Requests.AddRange(requests);
            data.NextRequestId = requests.Count > 0 ? requests.Max(r => r.Id) + 1 : 1;

            // 8. Снаряжение
            var equipment = new List<EquipmentRentalItem>();
            const string eqSql = @"
                SELECT id, inventory_code, name, category, size, price_per_hour, status, rented_by_rider, order_num
                FROM equipment_items
                ORDER BY id;";

            using (var cmd = new MySqlCommand(eqSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    equipment.Add(new EquipmentRentalItem
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        InventoryCode = reader["inventory_code"]?.ToString() ?? string.Empty,
                        Name = reader["name"]?.ToString() ?? string.Empty,
                        Category = reader["category"]?.ToString() ?? "Седло",
                        Size = reader["size"]?.ToString() ?? string.Empty,
                        PricePerHour = Convert.ToDecimal(reader["price_per_hour"]),
                        Status = reader["status"]?.ToString() ?? "Свободно",
                        RentedByRider = reader["rented_by_rider"]?.ToString() ?? "-",
                        OrderNum = reader["order_num"]?.ToString() ?? "-"
                    });
                }
            }
            if (equipment.Count > 0)
            {
                data.Equipment.Clear();
                data.Equipment.AddRange(equipment);
                data.NextEquipmentId = equipment.Max(e => e.Id) + 1;
            }

            // 9. Денники
            var stalls = new List<StallItem>();
            const string stallsSql = @"
                SELECT id, stall_code, wing_section, stall_type, horse_id, horse_name, breed_name, owner_or_boarding_type, reserved_until, status
                FROM stalls
                ORDER BY id;";

            using (var cmd = new MySqlCommand(stallsSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    stalls.Add(new StallItem
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        StallCode = reader["stall_code"]?.ToString() ?? string.Empty,
                        WingSection = reader["wing_section"]?.ToString() ?? string.Empty,
                        StallType = reader["stall_type"]?.ToString() ?? string.Empty,
                        HorseId = reader["horse_id"] == DBNull.Value ? null : Convert.ToInt32(reader["horse_id"]),
                        HorseName = reader["horse_name"]?.ToString() ?? "Свободен",
                        BreedName = reader["breed_name"]?.ToString() ?? "-",
                        OwnerOrBoardingType = reader["owner_or_boarding_type"]?.ToString() ?? "-",
                        ReservedUntil = reader["reserved_until"]?.ToString() ?? "-",
                        Status = reader["status"]?.ToString() ?? "Свободен"
                    });
                }
            }
            if (stalls.Count > 0)
            {
                data.Stalls.Clear();
                data.Stalls.AddRange(stalls);
            }

            // 10. Рационы кормления
            var rations = new List<FeedingRationItem>();
            const string rationsSql = @"
                SELECT id, horse_id, horse_name, stall_code, morning_feed, day_feed, evening_feed, diet_note
                FROM feeding_rations
                ORDER BY id;";

            using (var cmd = new MySqlCommand(rationsSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    rations.Add(new FeedingRationItem
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        HorseId = Convert.ToInt32(reader["horse_id"]),
                        HorseName = reader["horse_name"]?.ToString() ?? string.Empty,
                        StallCode = reader["stall_code"]?.ToString() ?? string.Empty,
                        MorningFeed = reader["morning_feed"]?.ToString() ?? string.Empty,
                        DayFeed = reader["day_feed"]?.ToString() ?? string.Empty,
                        EveningFeed = reader["evening_feed"]?.ToString() ?? string.Empty,
                        DietNote = reader["diet_note"]?.ToString() ?? string.Empty
                    });
                }
            }
            if (rations.Count > 0)
            {
                data.Rations.Clear();
                data.Rations.AddRange(rations);
            }

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

    public static bool AddClientRequest(ClientRequestItem req, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                INSERT INTO client_requests
                (user_id, requester_name, request_type, target_name, horse_id, arena_zone, duration_minutes, has_trainer, requested_datetime, estimated_cost, comment, equipment_code, status)
                VALUES (@uid, @name, @type, @target, @hid, @arena, @dur, @trainer, @dt, @cost, @comment, @eq, @status);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", req.UserId);
            cmd.Parameters.AddWithValue("@name", req.RequesterName);
            cmd.Parameters.AddWithValue("@type", req.RequestType);
            cmd.Parameters.AddWithValue("@target", req.TargetName);
            cmd.Parameters.AddWithValue("@hid", (object?)req.HorseId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@arena", req.ArenaZone);
            cmd.Parameters.AddWithValue("@dur", req.DurationMinutes);
            cmd.Parameters.AddWithValue("@trainer", req.HasTrainer ? 1 : 0);
            cmd.Parameters.AddWithValue("@dt", req.RequestedDateTime);
            cmd.Parameters.AddWithValue("@cost", req.EstimatedCost);
            cmd.Parameters.AddWithValue("@comment", string.IsNullOrWhiteSpace(req.Comment) ? DBNull.Value : req.Comment);
            cmd.Parameters.AddWithValue("@eq", string.IsNullOrWhiteSpace(req.EquipmentCode) ? DBNull.Value : req.EquipmentCode);
            cmd.Parameters.AddWithValue("@status", req.Status);
            cmd.ExecuteNonQuery();
            req.Id = (int)cmd.LastInsertedId;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool UpdateClientRequestStatus(int requestId, string status, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = "UPDATE client_requests SET status = @status WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", requestId);
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool UpdateUserRole(int userId, int newRoleId, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = "UPDATE users SET role_id = @roleId WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@roleId", newRoleId);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool UpdateUserCredentials(int userId, string newLogin, string? newPassword, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE login = @login AND id <> @id;", conn))
            {
                checkCmd.Parameters.AddWithValue("@login", newLogin);
                checkCmd.Parameters.AddWithValue("@id", userId);
                long count = Convert.ToInt64(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    error = "Такой логин уже занят другим пользователем";
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                const string sql = "UPDATE users SET login = @login WHERE id = @id;";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@login", newLogin);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }
            else
            {
                const string sql = "UPDATE users SET login = @login, password_hash = @hash WHERE id = @id;";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@login", newLogin);
                cmd.Parameters.AddWithValue("@hash", HashPassword(newPassword));
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static ClubRider EnsureRiderExists(MockClubData data, string fullName, out string error)
    {
        error = string.Empty;
        var existing = data.Riders.FirstOrDefault(r =>
            r.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        var newRider = new ClubRider
        {
            Id = data.NextRiderId++,
            FullName = fullName,
            Phone = "Не указан",
            SkillLevel = "Начинающий",
            DiscountRate = 0m,
            MembershipName = "Разовый визит",
            RemainingSessions = "-"
        };

        if (AddRider(newRider, out error))
            data.Riders.Add(newRider);

        return newRider;
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

    public static bool AddEquipment(EquipmentRentalItem item, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                INSERT INTO equipment_items (inventory_code, name, category, size, price_per_hour, status, rented_by_rider, order_num)
                VALUES (@code, @name, @cat, @size, @rate, @status, @rider, @order);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@code", item.InventoryCode);
            cmd.Parameters.AddWithValue("@name", item.Name);
            cmd.Parameters.AddWithValue("@cat", item.Category);
            cmd.Parameters.AddWithValue("@size", item.Size);
            cmd.Parameters.AddWithValue("@rate", item.PricePerHour);
            cmd.Parameters.AddWithValue("@status", item.Status);
            cmd.Parameters.AddWithValue("@rider", item.RentedByRider);
            cmd.Parameters.AddWithValue("@order", item.OrderNum);
            cmd.ExecuteNonQuery();
            item.Id = (int)cmd.LastInsertedId;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool UpdateEquipmentStatus(EquipmentRentalItem item, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                UPDATE equipment_items
                SET status = @status, rented_by_rider = @rider, order_num = @order
                WHERE inventory_code = @code;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@status", item.Status);
            cmd.Parameters.AddWithValue("@rider", item.RentedByRider);
            cmd.Parameters.AddWithValue("@order", item.OrderNum);
            cmd.Parameters.AddWithValue("@code", item.InventoryCode);
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool UpdateStall(StallItem stall, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                UPDATE stalls
                SET horse_id = @hid, horse_name = @hname, breed_name = @breed,
                    owner_or_boarding_type = @owner, reserved_until = @term, status = @status
                WHERE stall_code = @code;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hid", (object?)stall.HorseId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hname", stall.HorseName);
            cmd.Parameters.AddWithValue("@breed", stall.BreedName);
            cmd.Parameters.AddWithValue("@owner", stall.OwnerOrBoardingType);
            cmd.Parameters.AddWithValue("@term", stall.ReservedUntil);
            cmd.Parameters.AddWithValue("@status", stall.Status);
            cmd.Parameters.AddWithValue("@code", stall.StallCode);
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool AddFeedingRation(FeedingRationItem ration, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                INSERT INTO feeding_rations (horse_id, horse_name, stall_code, morning_feed, day_feed, evening_feed, diet_note)
                VALUES (@hid, @hname, @stall, @morning, @day, @evening, @note);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hid", ration.HorseId);
            cmd.Parameters.AddWithValue("@hname", ration.HorseName);
            cmd.Parameters.AddWithValue("@stall", ration.StallCode);
            cmd.Parameters.AddWithValue("@morning", ration.MorningFeed);
            cmd.Parameters.AddWithValue("@day", ration.DayFeed);
            cmd.Parameters.AddWithValue("@evening", ration.EveningFeed);
            cmd.Parameters.AddWithValue("@note", ration.DietNote);
            cmd.ExecuteNonQuery();
            ration.Id = (int)cmd.LastInsertedId;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool UpdateFeedingRation(FeedingRationItem ration, out string error)
    {
        error = string.Empty;
        if (!IsConnected)
            return true;

        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                UPDATE feeding_rations
                SET stall_code = @stall, morning_feed = @morning, day_feed = @day, evening_feed = @evening, diet_note = @note
                WHERE horse_id = @hid;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@stall", ration.StallCode);
            cmd.Parameters.AddWithValue("@morning", ration.MorningFeed);
            cmd.Parameters.AddWithValue("@day", ration.DayFeed);
            cmd.Parameters.AddWithValue("@evening", ration.EveningFeed);
            cmd.Parameters.AddWithValue("@note", ration.DietNote);
            cmd.Parameters.AddWithValue("@hid", ration.HorseId);
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

        foreach (var req in data.Requests.Where(r => r.Status == "Одобрено"))
        {
            if (req.RequestType == "Тренировка и манеж" && !string.IsNullOrWhiteSpace(req.EquipmentCode))
            {
                var activeOrder = data.Orders.FirstOrDefault(o =>
                    !ClubLogic.IsClosedStatus(o.Status)
                    && o.RiderName.Equals(req.RequesterName, StringComparison.OrdinalIgnoreCase)
                    && o.OrderDate == req.RequestedDateTime);
                if (activeOrder != null)
                {
                    ClubLogic.AutoAssignEquipment(data.Equipment, req.EquipmentCode, req.RequesterName, activeOrder.OrderNum);
                }
            }
            else if (req.RequestType == "Аренда снаряжения")
            {
                var equip = data.Equipment.FirstOrDefault(e =>
                    req.TargetName.StartsWith(e.InventoryCode, StringComparison.OrdinalIgnoreCase)
                    || req.TargetName.Contains(e.Name, StringComparison.OrdinalIgnoreCase));
                if (equip != null && equip.Status == "Свободно")
                {
                    var activeOrder = data.Orders.FirstOrDefault(o =>
                        !ClubLogic.IsClosedStatus(o.Status)
                        && o.RiderName.Equals(req.RequesterName, StringComparison.OrdinalIgnoreCase));
                    equip.Status = "В аренде";
                    equip.RentedByRider = req.RequesterName;
                    equip.OrderNum = activeOrder?.OrderNum ?? "Аренда по заявке";
                }
            }
            else if (req.RequestType == "Бронь денника")
            {
                var stall = data.Stalls.FirstOrDefault(s =>
                    req.TargetName.StartsWith(s.StallCode, StringComparison.OrdinalIgnoreCase));
                if (stall != null && stall.Status == "Свободен")
                {
                    stall.Status = "Забронирован";
                    stall.OwnerOrBoardingType = req.RequesterName;
                    stall.HorseName = string.IsNullOrWhiteSpace(req.Comment) ? "Бронь клиента" : req.Comment;
                    stall.BreedName = "По заявке";
                    stall.ReservedUntil = $"с {req.RequestedDateTime:dd.MM.yyyy}";
                }
            }
        }
    }
}
