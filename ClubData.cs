using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Avalo;

// модели

public class RoleItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PermissionsSummary { get; set; } = string.Empty;
}

public class UserProfile
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
            if (parts.Length == 1 && parts[0].Length >= 2)
                return parts[0][..2].ToUpperInvariant();
            if (parts.Length == 1 && parts[0].Length == 1)
                return parts[0].ToUpperInvariant();
            return "КС";
        }
    }

    public string ShortName
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
                return $"{parts[0]} {parts[1][0]}.{parts[2][0]}.";
            if (parts.Length == 2)
                return $"{parts[0]} {parts[1][0]}.";
            return FullName;
        }
    }
}

public class HorseBreed
{
    public int Id { get; set; }
    public string BreedName { get; set; } = string.Empty;
    public string Temperament { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;

    public override string ToString() => $"{BreedName} ({OriginCountry})";
}

public class HorseItem
{
    public int Id { get; set; }
    public int BreedId { get; set; }
    public string BreedName { get; set; } = string.Empty;
    public string Temperament { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string PassportChipCode { get; set; } = string.Empty;
    public int BirthYear { get; set; }
    public string SuitColor { get; set; } = string.Empty;
    public bool IsReadyForTraining { get; set; } = true;

    public override string ToString() =>
        IsReadyForTraining ? $"{Nickname} ({BreedName})" : $"{Nickname} [Веткарантин]";
}

public class ClubRider
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string SkillLevel { get; set; } = "Любитель";
    public decimal DiscountRate { get; set; }
    public string MembershipName { get; set; } = "Разовый визит";
    public string RemainingSessions { get; set; } = "-";

    public override string ToString() =>
        DiscountRate > 0 ? $"{FullName} (скидка {DiscountRate:0}%)" : FullName;
}

public class RidingSessionItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int HorseId { get; set; }
    public string HorseName { get; set; } = string.Empty;
    public DateTime SessionDateTime { get; set; }
    public int DurationMinutes { get; set; } = 45;
    public bool HasTrainer { get; set; }
    public decimal ItemCost { get; set; }
}

public class RidingOrder
{
    public int Id { get; set; }
    public string OrderNum { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int ClubRiderId { get; set; }
    public string RiderName { get; set; } = string.Empty;
    public decimal RiderDiscount { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = ClubLogic.OrderStatuses.Booked;
    public string? CancelReason { get; set; }
    public string ArenaZone { get; set; } = "Конкурный манеж";
    public List<RidingSessionItem> Items { get; set; } = new();
}

public class EquipmentRentalItem
{
    public int Id { get; set; }
    public string InventoryCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Седло";
    public string Size { get; set; } = string.Empty;
    public decimal PricePerHour { get; set; }
    public string Status { get; set; } = "Свободно";
    public string RentedByRider { get; set; } = "-";
    public string OrderNum { get; set; } = "-";
}

public class StallItem
{
    public int Id { get; set; }
    public string StallCode { get; set; } = string.Empty;
    public string WingSection { get; set; } = string.Empty;
    public string StallType { get; set; } = string.Empty;
    public int? HorseId { get; set; }
    public string HorseName { get; set; } = "Свободен";
    public string BreedName { get; set; } = "-";
    public string OwnerOrBoardingType { get; set; } = "-";
    public string ReservedUntil { get; set; } = "-";
    public string Status { get; set; } = "Свободен";
}

public class FeedingRationItem
{
    public int Id { get; set; }
    public int HorseId { get; set; }
    public string HorseName { get; set; } = string.Empty;
    public string StallCode { get; set; } = string.Empty;
    public string MorningFeed { get; set; } = string.Empty;
    public string DayFeed { get; set; } = string.Empty;
    public string EveningFeed { get; set; } = string.Empty;
    public string DietNote { get; set; } = string.Empty;
}

public class ClientRequestItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequestType { get; set; } = "Тренировка и манеж";
    public string TargetName { get; set; } = string.Empty;
    public int? HorseId { get; set; }
    public string ArenaZone { get; set; } = "Конкурный манеж";
    public int DurationMinutes { get; set; } = 45;
    public bool HasTrainer { get; set; } = true;
    public DateTime RequestedDateTime { get; set; } = DateTime.Today.AddHours(16);
    public decimal EstimatedCost { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string EquipmentCode { get; set; } = string.Empty;
    public string Status { get; set; } = "На рассмотрении";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ManagedUserItem
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string OfflinePasswordHash { get; set; } = string.Empty;
}

public class PenaltyItem
{
    public int Id { get; set; }
    public string RiderName { get; set; } = string.Empty;
    public string IssuedByName { get; set; } = string.Empty;
    public string IssuerRole { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string TargetObject { get; set; } = "-";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Не оплачен";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

// логика и расчеты

public static class ClubLogic
{
    public static class OrderStatuses
    {
        public const string Booked = "Забронировано";
        public const string Saddled = "Лошадь оседлана";
        public const string InArena = "Тренировка в манеже";
        public const string Completed = "Завершена успешно";
        public const string CancelledVet = "Отмена по ветпоказаниям";
    }

    public static string FormatOrderNumber(int year, int sequenceNum)
    {
        return $"ПР23-КСК-{year}-{sequenceNum:D5}";
    }

    public static decimal CalculateSessionCost(int durationMinutes, bool hasTrainer, decimal discountRate)
    {
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Длительность должна быть больше 0.");

        decimal baseCost = (durationMinutes / 45m) * 1500m;
        decimal trainerFee = hasTrainer ? 1000m : 0m;
        decimal subtotal = baseCost + trainerFee;
        decimal clampedDiscount = Math.Clamp(discountRate, 0m, 100m);
        decimal total = subtotal * (1m - clampedDiscount / 100m);
        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    public static bool IsClosedStatus(string status) =>
        status == OrderStatuses.Completed || status == OrderStatuses.CancelledVet;

    public static string? GetNextPrimaryStatus(string currentStatus) => currentStatus switch
    {
        OrderStatuses.Booked => OrderStatuses.Saddled,
        OrderStatuses.Saddled => OrderStatuses.InArena,
        OrderStatuses.InArena => OrderStatuses.Completed,
        _ => null
    };

    public static string? GetNextButtonLabel(string currentStatus) => currentStatus switch
    {
        OrderStatuses.Booked => "Оседлать",
        OrderStatuses.Saddled => "В манеж",
        OrderStatuses.InArena => "Завершить",
        _ => null
    };

    public static bool CanTransition(string fromStatus, string toStatus)
    {
        if (IsClosedStatus(fromStatus))
            return false;

        if (toStatus == OrderStatuses.CancelledVet)
            return true;

        return (fromStatus, toStatus) switch
        {
            (OrderStatuses.Booked, OrderStatuses.Saddled) => true,
            (OrderStatuses.Saddled, OrderStatuses.InArena) => true,
            (OrderStatuses.InArena, OrderStatuses.Completed) => true,
            _ => false
        };
    }

    public static void EnsureOrderEditable(RidingOrder order)
    {
        if (IsClosedStatus(order.Status))
            throw new InvalidOperationException($"Нельзя изменить закрытый документ ({order.Status}).");
    }

    public static void TransitionOrderStatus(RidingOrder order, string nextStatus, bool horseReadyForTraining, string? cancelReason = null)
    {
        EnsureOrderEditable(order);

        if (nextStatus != OrderStatuses.CancelledVet && !horseReadyForTraining)
            throw new InvalidOperationException("Назначение лошади заблокировано: отсутствует ветеринарный допуск.");

        if (!CanTransition(order.Status, nextStatus))
            throw new InvalidOperationException($"Недопустимый переход статуса: '{order.Status}' -> '{nextStatus}'.");

        order.Status = nextStatus;
        if (nextStatus == OrderStatuses.CancelledVet)
            order.CancelReason = string.IsNullOrWhiteSpace(cancelReason)
                ? "Снята по ветеринарным показаниям"
                : cancelReason.Trim();
    }

    public static decimal RecalculateOrderTotal(RidingOrder order)
    {
        return Math.Round(order.Items.Sum(i => i.ItemCost), 2, MidpointRounding.AwayFromZero);
    }

    public static void AddSessionItem(RidingOrder order, RidingSessionItem item, bool horseReadyForTraining, int existingHorseDailyMinutes = 0)
    {
        EnsureOrderEditable(order);

        if (!horseReadyForTraining)
            throw new InvalidOperationException("Назначение лошади заблокировано: отсутствует ветеринарный допуск.");

        if (existingHorseDailyMinutes + item.DurationMinutes > 180)
            throw new InvalidOperationException("Превышен суточный лимит нагрузки лошади (не более 3 часов в сутки).");

        item.ItemCost = CalculateSessionCost(item.DurationMinutes, item.HasTrainer, order.RiderDiscount);
        item.OrderId = order.Id;
        order.Items.Add(item);
        order.TotalPrice = RecalculateOrderTotal(order);
    }

    public static decimal CalculateUnpaidPenaltiesTotal(IEnumerable<PenaltyItem> penalties, string? riderName = null)
    {
        return penalties
            .Where(p => p.Status == "Не оплачен"
                && (string.IsNullOrWhiteSpace(riderName) || p.RiderName.Equals(riderName, StringComparison.OrdinalIgnoreCase)))
            .Sum(p => p.Amount);
    }

    public static bool TryParseSessionTime(string? text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string trimmed = text.Trim();
        if (!TimeSpan.TryParseExact(trimmed, new[] { @"hh\:mm", @"h\:mm" }, CultureInfo.InvariantCulture, out time))
            return false;

        return time >= TimeSpan.FromHours(7) && time <= TimeSpan.FromHours(22);
    }

    public static string BuildIntegritySourceString(string orderNum, DateTime orderDate, string counterparty, decimal totalPrice, int itemCount)
    {
        string dateText = orderDate.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        string totalText = totalPrice.ToString("F2", CultureInfo.InvariantCulture);
        return $"[{orderNum}]+[{dateText}]+[{counterparty}]+[{totalText}]+[{itemCount}]";
    }

    public static string ComputeDocumentSha256(string orderNum, DateTime orderDate, string counterparty, decimal totalPrice, int itemCount)
    {
        string dateText = orderDate.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        string totalText = totalPrice.ToString("F2", CultureInfo.InvariantCulture);
        string raw = $"{orderNum}{dateText}{counterparty}{totalText}{itemCount}";
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string BuildRouteSheetText(RidingOrder order, IEnumerable<EquipmentRentalItem>? rentedEquipment = null)
    {
        string dateText = order.OrderDate.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        string sha = ComputeDocumentSha256(order.OrderNum, order.OrderDate, order.RiderName, order.TotalPrice, order.Items.Count);
        string sourceConcat = BuildIntegritySourceString(order.OrderNum, order.OrderDate, order.RiderName, order.TotalPrice, order.Items.Count);

        var sb = new StringBuilder();
        sb.AppendLine("КСК И ИППОДРОМ «АЛЛЮР»");
        sb.AppendLine("МАРШРУТНЫЙ ЛИСТ ТРЕНИРОВКИ И АКТ ПРОВЕРКИ СНАРЯЖЕНИЯ");
        sb.AppendLine();
        sb.AppendLine($"Документ N:           {order.OrderNum}");
        sb.AppendLine($"Дата оформления:      {dateText}");
        sb.AppendLine($"Всадник (контрагент): {order.RiderName} (скидка {order.RiderDiscount:0}%)");
        sb.AppendLine($"Манеж / Зона:         {order.ArenaZone}");
        sb.AppendLine($"Статус заказа:        {order.Status}");
        if (!string.IsNullOrWhiteSpace(order.CancelReason))
            sb.AppendLine($"Причина отмены:       {order.CancelReason}");
        sb.AppendLine();
        sb.AppendLine("ПОЗИЦИИ ТРЕНИРОВКИ:");
        foreach (var item in order.Items)
        {
            string trainerStr = item.HasTrainer ? "с тренером (+1000 руб.)" : "без тренера";
            sb.AppendLine($" * Лошадь: {item.HorseName}, {item.SessionDateTime:dd.MM.yyyy HH:mm}, {item.DurationMinutes} мин, {trainerStr} — {item.ItemCost:N2} руб.");
        }

        var eqList = rentedEquipment?.Where(e => e.OrderNum == order.OrderNum).ToList();
        if (eqList != null && eqList.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ВЫДАННОЕ СНАРЯЖЕНИЕ:");
            foreach (var eq in eqList)
                sb.AppendLine($" * {eq.InventoryCode} — {eq.Name} ({eq.Category}, размер {eq.Size})");
        }

        sb.AppendLine();
        sb.AppendLine($"ИТОГО К ОПЛАТЕ: {order.TotalPrice:N2} руб. (позиций: {order.Items.Count})");
        sb.AppendLine();
        sb.AppendLine("АКТ ПРОВЕРКИ СНАРЯЖЕНИЯ:");
        sb.AppendLine(" [V] Седло и подпруга проверены, приструги зафиксированы");
        sb.AppendLine(" [V] Уздечка и повод исправны, трензель подобран по размеру");
        sb.AppendLine(" [V] Защитный шлем надет и отрегулирован перед выходом в манеж");
        sb.AppendLine();
        sb.AppendLine("Инструктор по верховой езде: _____________________ / Подпись /");
        sb.AppendLine();
        sb.AppendLine("КОНТРОЛЬ ЦЕЛОСТНОСТИ (SHA-256):");
        sb.AppendLine($"Поля:    {sourceConcat}");
        sb.AppendLine($"SHA-256: {sha}");
        return sb.ToString();
    }

    public static string FormatMoney(decimal amount) =>
        $"{amount.ToString("#,##0", CultureInfo.GetCultureInfo("ru-RU"))} руб.";

    public static List<string> AutoAssignEquipment(List<EquipmentRentalItem> equipment, string? equipmentCode, string riderName, string orderNum)
    {
        var booked = new List<string>();
        if (string.IsNullOrWhiteSpace(equipmentCode))
            return booked;

        if (string.Equals(equipmentCode, "AUTO", StringComparison.OrdinalIgnoreCase))
        {
            var helmet = equipment.FirstOrDefault(e => e.Status == "Свободно" && e.Category == "Шлем");
            if (helmet != null)
            {
                helmet.Status = "В аренде";
                helmet.RentedByRider = riderName;
                helmet.OrderNum = orderNum;
                booked.Add(helmet.InventoryCode);
            }

            var saddle = equipment.FirstOrDefault(e => e.Status == "Свободно" && e.Category == "Седло");
            if (saddle != null)
            {
                saddle.Status = "В аренде";
                saddle.RentedByRider = riderName;
                saddle.OrderNum = orderNum;
                booked.Add(saddle.InventoryCode);
            }
            return booked;
        }

        var item = equipment.FirstOrDefault(e =>
            e.Status == "Свободно"
            && (e.InventoryCode.Equals(equipmentCode, StringComparison.OrdinalIgnoreCase)
                || equipmentCode.StartsWith(e.InventoryCode, StringComparison.OrdinalIgnoreCase)));
        if (item != null)
        {
            item.Status = "В аренде";
            item.RentedByRider = riderName;
            item.OrderNum = orderNum;
            booked.Add(item.InventoryCode);
        }

        return booked;
    }

    public static List<EquipmentRentalItem> ReleaseOrderEquipment(List<EquipmentRentalItem> equipment, string orderNum)
    {
        var released = equipment.Where(e => e.Status == "В аренде" && e.OrderNum == orderNum).ToList();
        foreach (var item in released)
        {
            item.Status = "Свободно";
            item.RentedByRider = "-";
            item.OrderNum = "-";
        }
        return released;
    }

    public static decimal GetDiscountForSkill(string skillLevel) => skillLevel switch
    {
        "Любитель" => 5m,
        "Опытный всадник" => 10m,
        "Спортсмен (конкур)" => 15m,
        "Льготный / Безлимит" => 20m,
        _ => 0m
    };

    public static string GetMembershipLabel(decimal discount)
    {
        if (discount >= 20m) return "Льготный / Безлимит";
        if (discount >= 15m) return "Спортсмен (конкур)";
        if (discount >= 10m) return "Опытный всадник";
        if (discount >= 5m) return "Клубный базовый";
        return "Разовый визит";
    }
}

// стартовые данные

public class MockClubData
{
    public UserProfile CurrentUser { get; set; } = new()
    {
        Id = 1,
        RoleId = 2,
        RoleName = "Администратор манежа",
        Login = "admin",
        FullName = "Иванова Анна Сергеевна"
    };

    public List<RoleItem> Roles { get; } = new()
    {
        new RoleItem
        {
            Id = 1,
            Name = "Главный берейтор-зоотехник",
            PermissionsSummary = "Рационы кормления, ветеринарные допуски, бронирование денников, штрафы по лошадям"
        },
        new RoleItem
        {
            Id = 2,
            Name = "Администратор манежа",
            PermissionsSummary = "Запись всадников, аренда снаряжения, скидки, штрафы, одобрение заявок и роли"
        },
        new RoleItem
        {
            Id = 3,
            Name = "Клиент",
            PermissionsSummary = "Заявки на тренировку, коня, аренду, постой, запрос скидки и просмотр штрафов"
        }
    };

    public List<HorseBreed> Breeds { get; } = new()
    {
        new HorseBreed { Id = 1, BreedName = "Ганноверская", Temperament = "Уравновешенный, конкурный", OriginCountry = "Германия" },
        new HorseBreed { Id = 2, BreedName = "Ахалтекинская", Temperament = "Энергичный, резвый", OriginCountry = "Туркменистан" },
        new HorseBreed { Id = 3, BreedName = "Тракененская", Temperament = "Спортивный, выносливый", OriginCountry = "Германия" },
        new HorseBreed { Id = 4, BreedName = "Орловский рысак", Temperament = "Послушный, учебный", OriginCountry = "Россия" }
    };

    public List<HorseItem> Horses { get; }
    public List<ClubRider> Riders { get; }
    public List<RidingOrder> Orders { get; }
    public List<EquipmentRentalItem> Equipment { get; }
    public List<StallItem> Stalls { get; }
    public List<FeedingRationItem> Rations { get; }
    public List<ClientRequestItem> Requests { get; }
    public List<ManagedUserItem> Users { get; }
    public List<PenaltyItem> Penalties { get; }

    public int NextOrderSequence { get; set; } = 20;
    public int NextRiderId { get; set; } = 7;
    public int NextHorseId { get; set; } = 7;
    public int NextEquipmentId { get; set; } = 9;
    public int NextRequestId { get; set; } = 8;
    public int NextPenaltyId { get; set; } = 8;

    public MockClubData()
    {
        Horses = new List<HorseItem>
        {
            new() { Id = 1, BreedId = 1, BreedName = "Ганноверская", Temperament = "Уравновешенный", OriginCountry = "Германия", Nickname = "Буцефал", PassportChipCode = "CHIP-RU-2018-104", BirthYear = 2018, SuitColor = "Вороная", IsReadyForTraining = true },
            new() { Id = 2, BreedId = 3, BreedName = "Тракененская", Temperament = "Спортивный", OriginCountry = "Германия", Nickname = "Гроза", PassportChipCode = "CHIP-RU-2019-219", BirthYear = 2019, SuitColor = "Гнедая", IsReadyForTraining = true },
            new() { Id = 3, BreedId = 2, BreedName = "Ахалтекинская", Temperament = "Энергичный", OriginCountry = "Туркменистан", Nickname = "Альтаир", PassportChipCode = "CHIP-RU-2020-315", BirthYear = 2020, SuitColor = "Соловая", IsReadyForTraining = true },
            new() { Id = 4, BreedId = 4, BreedName = "Орловский рысак", Temperament = "Послушный", OriginCountry = "Россия", Nickname = "Зевс", PassportChipCode = "CHIP-RU-2017-088", BirthYear = 2017, SuitColor = "Серая в яблоках", IsReadyForTraining = false },
            new() { Id = 5, BreedId = 1, BreedName = "Ганноверская", Temperament = "Уравновешенный", OriginCountry = "Германия", Nickname = "Карамель", PassportChipCode = "CHIP-RU-2021-402", BirthYear = 2021, SuitColor = "Рыжая", IsReadyForTraining = true },
            new() { Id = 6, BreedId = 3, BreedName = "Тракененская", Temperament = "Спортивный", OriginCountry = "Германия", Nickname = "Барон", PassportChipCode = "CHIP-RU-2019-511", BirthYear = 2019, SuitColor = "Тёмно-гнедая", IsReadyForTraining = true }
        };

        Riders = new List<ClubRider>
        {
            new() { Id = 1, FullName = "Соколов Дмитрий Андреевич", Phone = "+7 (903) 412-55-10", SkillLevel = "Спортсмен (конкур)", DiscountRate = 15m, MembershipName = ClubLogic.GetMembershipLabel(15m), RemainingSessions = "5 зан." },
            new() { Id = 2, FullName = "Волкова Мария Игоревна", Phone = "+7 (916) 784-19-22", SkillLevel = "Опытный всадник", DiscountRate = 10m, MembershipName = ClubLogic.GetMembershipLabel(10m), RemainingSessions = "3 зан." },
            new() { Id = 3, FullName = "Кузнецов Артём Олегович", Phone = "+7 (925) 330-81-04", SkillLevel = "Начинающий", DiscountRate = 0m, MembershipName = ClubLogic.GetMembershipLabel(0m), RemainingSessions = "1 зан." },
            new() { Id = 4, FullName = "Лебедева Екатерина Павловна", Phone = "+7 (905) 619-40-77", SkillLevel = "Льготный / Безлимит", DiscountRate = 20m, MembershipName = ClubLogic.GetMembershipLabel(20m), RemainingSessions = "6 зан." },
            new() { Id = 5, FullName = "Морозов Илья Викторович", Phone = "+7 (985) 207-93-18", SkillLevel = "Любитель", DiscountRate = 5m, MembershipName = ClubLogic.GetMembershipLabel(5m), RemainingSessions = "1 зан." },
            new() { Id = 6, FullName = "Ткаченко Кирилл Александрович", Phone = "+7 (900) 555-23-23", SkillLevel = "Начинающий", DiscountRate = 0m, MembershipName = ClubLogic.GetMembershipLabel(0m), RemainingSessions = "2 зан." }
        };

        var today = DateTime.Today;
        Orders = new List<RidingOrder>
        {
            CreateSeedOrder(1, 2026, 1, Riders[0], Horses[0], today.AddHours(10), 90, true, ClubLogic.OrderStatuses.InArena, "Конкурный манеж"),
            CreateSeedOrder(2, 2026, 2, Riders[1], Horses[1], today.AddHours(11).AddMinutes(30), 45, true, ClubLogic.OrderStatuses.Saddled, "Крытый манеж 1"),
            CreateSeedOrder(3, 2026, 3, Riders[2], Horses[4], today.AddHours(14), 45, true, ClubLogic.OrderStatuses.Booked, "Учебный манеж"),
            CreateSeedOrder(4, 2026, 4, Riders[3], Horses[5], today.AddHours(9), 90, false, ClubLogic.OrderStatuses.Completed, "Открытый плац"),
            CreateSeedOrder(5, 2026, 5, Riders[4], Horses[3], today.AddHours(16), 45, false, ClubLogic.OrderStatuses.CancelledVet, "Крытый манеж 1", "Ветеринарный карантин копытного сустава"),
            CreateSeedOrder(6, 2026, 6, Riders[5], Horses[2], today.AddHours(17), 45, true, ClubLogic.OrderStatuses.Booked, "Учебный манеж"),
            CreateSeedOrder(7, 2026, 7, Riders[3], Horses[4], today.AddDays(-2).AddHours(10), 90, true, ClubLogic.OrderStatuses.Completed, "Конкурный манеж"),
            CreateSeedOrder(8, 2026, 8, Riders[0], Horses[0], today.AddDays(-2).AddHours(15), 90, true, ClubLogic.OrderStatuses.Completed, "Конкурный манеж"),
            CreateSeedOrder(9, 2026, 9, Riders[3], Horses[2], today.AddDays(-4).AddHours(11), 45, false, ClubLogic.OrderStatuses.Completed, "Открытый плац"),
            CreateSeedOrder(10, 2026, 10, Riders[0], Horses[1], today.AddDays(-5).AddHours(16), 90, true, ClubLogic.OrderStatuses.Completed, "Конкурный манеж"),
            CreateSeedOrder(11, 2026, 11, Riders[1], Horses[5], today.AddDays(-5).AddHours(13), 45, true, ClubLogic.OrderStatuses.Completed, "Крытый манеж 1"),
            CreateSeedOrder(12, 2026, 12, Riders[3], Horses[0], today.AddDays(-7).AddHours(10), 90, true, ClubLogic.OrderStatuses.Completed, "Конкурный манеж"),
            CreateSeedOrder(13, 2026, 13, Riders[5], Horses[4], today.AddDays(-7).AddHours(14), 45, true, ClubLogic.OrderStatuses.Completed, "Учебный манеж"),
            CreateSeedOrder(14, 2026, 14, Riders[4], Horses[2], today.AddDays(-9).AddHours(12), 45, false, ClubLogic.OrderStatuses.Completed, "Открытый плац"),
            CreateSeedOrder(15, 2026, 15, Riders[0], Horses[5], today.AddDays(-10).AddHours(17), 90, true, ClubLogic.OrderStatuses.Completed, "Конкурный манеж"),
            CreateSeedOrder(16, 2026, 16, Riders[3], Horses[1], today.AddDays(-11).AddHours(11), 90, false, ClubLogic.OrderStatuses.Completed, "Открытый плац"),
            CreateSeedOrder(17, 2026, 17, Riders[1], Horses[4], today.AddDays(-12).AddHours(15), 45, true, ClubLogic.OrderStatuses.Completed, "Крытый манеж 1"),
            CreateSeedOrder(18, 2026, 18, Riders[0], Horses[0], today.AddDays(-14).AddHours(16), 90, true, ClubLogic.OrderStatuses.Completed, "Конкурный манеж"),
            CreateSeedOrder(19, 2026, 19, Riders[3], Horses[5], today.AddDays(-16).AddHours(10), 90, true, ClubLogic.OrderStatuses.Completed, "Конкурный манеж")
        };

        Equipment = new List<EquipmentRentalItem>
        {
            new() { Id = 1, InventoryCode = "EQ-001", Name = "Седло конкурное Prestige X-Paris", Category = "Седло", Size = "17.5\"", PricePerHour = 500m, Status = "В аренде", RentedByRider = "Соколов Д.А.", OrderNum = "ПР23-КСК-2026-00001" },
            new() { Id = 2, InventoryCode = "EQ-002", Name = "Шлем защитный Uvex Perfexxion II", Category = "Шлем", Size = "M (55-58)", PricePerHour = 250m, Status = "В аренде", RentedByRider = "Волкова М.И.", OrderNum = "ПР23-КСК-2026-00002" },
            new() { Id = 3, InventoryCode = "EQ-003", Name = "Шлем тренировочный Fouganza C500", Category = "Шлем", Size = "L (58-61)", PricePerHour = 200m, Status = "Свободно", RentedByRider = "-", OrderNum = "-" },
            new() { Id = 4, InventoryCode = "EQ-004", Name = "Седло универсальное Kieffer Wien", Category = "Седло", Size = "17.0\"", PricePerHour = 400m, Status = "Свободно", RentedByRider = "-", OrderNum = "-" },
            new() { Id = 5, InventoryCode = "EQ-005", Name = "Защитный жилет для конкура Hit-Air", Category = "Защита", Size = "M", PricePerHour = 350m, Status = "В аренде", RentedByRider = "Соколов Д.А.", OrderNum = "ПР23-КСК-2026-00001" },
            new() { Id = 6, InventoryCode = "EQ-006", Name = "Краги кожаные Tattini Close", Category = "Амуниция", Size = "M / High", PricePerHour = 180m, Status = "Свободно", RentedByRider = "-", OrderNum = "-" },
            new() { Id = 7, InventoryCode = "EQ-007", Name = "Шлем детский Casco Nori", Category = "Шлем", Size = "S (52-55)", PricePerHour = 200m, Status = "На проверке", RentedByRider = "-", OrderNum = "-" },
            new() { Id = 8, InventoryCode = "EQ-008", Name = "Седло конкурное Stubben Zaria", Category = "Седло", Size = "18.0\"", PricePerHour = 450m, Status = "Свободно", RentedByRider = "-", OrderNum = "-" }
        };

        Stalls = new List<StallItem>
        {
            new() { Id = 1, StallCode = "Д-01", WingSection = "Крыло А", StallType = "Спортивный", HorseId = 1, HorseName = "Буцефал", BreedName = "Ганноверская", OwnerOrBoardingType = "КСК «Аллюр»", ReservedUntil = "Постоянный", Status = "Занят" },
            new() { Id = 2, StallCode = "Д-02", WingSection = "Крыло А", StallType = "Спортивный", HorseId = 2, HorseName = "Гроза", BreedName = "Тракененская", OwnerOrBoardingType = "КСК «Аллюр»", ReservedUntil = "Постоянный", Status = "Занят" },
            new() { Id = 3, StallCode = "Д-03", WingSection = "Крыло А", StallType = "Спортивный", HorseId = 3, HorseName = "Альтаир", BreedName = "Ахалтекинская", OwnerOrBoardingType = "Частный постой (Орлов Д.В.)", ReservedUntil = "до 30.12.2026", Status = "Занят" },
            new() { Id = 4, StallCode = "Д-04", WingSection = "Ветблок", StallType = "Изолятор", HorseId = 4, HorseName = "Зевс", BreedName = "Орловский рысак", OwnerOrBoardingType = "КСК «Аллюр»", ReservedUntil = "до 28.11.2026", Status = "Занят" },
            new() { Id = 5, StallCode = "Д-05", WingSection = "Крыло Б", StallType = "Стандарт", HorseId = 5, HorseName = "Карамель", BreedName = "Ганноверская", OwnerOrBoardingType = "КСК «Аллюр»", ReservedUntil = "Постоянный", Status = "Занят" },
            new() { Id = 6, StallCode = "Д-06", WingSection = "Крыло Б", StallType = "Стандарт", HorseId = 6, HorseName = "Барон", BreedName = "Тракененская", OwnerOrBoardingType = "Частный постой (Ким А.Р.)", ReservedUntil = "до 15.01.2027", Status = "Занят" },
            new() { Id = 7, StallCode = "Д-07", WingSection = "Крыло Б", StallType = "Стандарт", HorseId = null, HorseName = "Бронь: Вектор", BreedName = "Будённовская", OwnerOrBoardingType = "Частный постой (Титов С.)", ReservedUntil = "с 25.11.2026", Status = "Забронирован" },
            new() { Id = 8, StallCode = "Д-08", WingSection = "Крыло Б", StallType = "Стандарт", HorseId = null, HorseName = "Свободен", BreedName = "-", OwnerOrBoardingType = "-", ReservedUntil = "-", Status = "Свободен" },
            new() { Id = 9, StallCode = "Д-09", WingSection = "Крыло А", StallType = "Спортивный", HorseId = null, HorseName = "Свободен", BreedName = "-", OwnerOrBoardingType = "-", ReservedUntil = "-", Status = "Свободен" }
        };

        Rations = new List<FeedingRationItem>
        {
            new() { Id = 1, HorseId = 1, HorseName = "Буцефал", StallCode = "Д-01", MorningFeed = "Овёс 2.5 кг, Сено луговое 4 кг", DayFeed = "Мэш 1.5 кг, Сено 4 кг, Морковь 1 кг", EveningFeed = "Овёс 2.0 кг, Сено 5 кг, Электролиты", DietNote = "Спортивный (конкур)" },
            new() { Id = 2, HorseId = 2, HorseName = "Гроза", StallCode = "Д-02", MorningFeed = "Овёс 2.0 кг, Сено 4 кг", DayFeed = "Мюсли без овса 1.5 кг, Сено 4 кг", EveningFeed = "Овёс 2.0 кг, Сено 4.5 кг, Биотин", DietNote = "Конкурный стандарт" },
            new() { Id = 3, HorseId = 3, HorseName = "Альтаир", StallCode = "Д-03", MorningFeed = "Мюсли спорт 2.0 кг, Люцерна 3 кг", DayFeed = "Ячмень плющёный 1.0 кг, Сено 4 кг", EveningFeed = "Мюсли 2.0 кг, Сено 4.5 кг, Лён", DietNote = "Повышенный белок" },
            new() { Id = 4, HorseId = 4, HorseName = "Зевс", StallCode = "Д-04", MorningFeed = "ВТМ 1.5 кг, Сено запаренное 4 кг", DayFeed = "Мэш тёплый 1.5 кг, Сено 4 кг", EveningFeed = "Отруби 1.0 кг, Сено 5 кг, Хондропротектор", DietNote = "Щадящий (веткарантин)" },
            new() { Id = 5, HorseId = 5, HorseName = "Карамель", StallCode = "Д-05", MorningFeed = "Овёс 1.5 кг, Сено 3.5 кг", DayFeed = "Мюсли лайт 1.0 кг, Яблоки 0.5 кг", EveningFeed = "Овёс 1.5 кг, Сено 4 кг", DietNote = "Учебный рацион" },
            new() { Id = 6, HorseId = 6, HorseName = "Барон", StallCode = "Д-06", MorningFeed = "Овёс 2.0 кг, Сено 4 кг", DayFeed = "Каша овсяная 1.5 кг, Сено 4 кг", EveningFeed = "Овёс 2.0 кг, Сено 5 кг, Соль-лизунец", DietNote = "Стандартный тренировочный" }
        };

        Users = new List<ManagedUserItem>
        {
            new() { Id = 1, RoleId = 1, RoleName = "Главный берейтор-зоотехник", Login = "bereitor", FullName = "Орлов Алексей Викторович", OfflinePasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92" },
            new() { Id = 2, RoleId = 2, RoleName = "Администратор манежа", Login = "admin", FullName = "Иванова Анна Сергеевна", OfflinePasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92" },
            new() { Id = 3, RoleId = 3, RoleName = "Клиент", Login = "client", FullName = "Петров Максим Игоревич", OfflinePasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92" },
            new() { Id = 4, RoleId = 3, RoleName = "Клиент", Login = "Pokuz", FullName = "Ткаченко Кирилл Александрович", OfflinePasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92" }
        };

        Requests = new List<ClientRequestItem>
        {
            new()
            {
                Id = 1,
                UserId = 3,
                RequesterName = "Петров Максим Игоревич",
                RequestType = "Тренировка и манеж",
                TargetName = "Буцефал",
                HorseId = 1,
                ArenaZone = "Конкурный манеж",
                DurationMinutes = 45,
                HasTrainer = true,
                RequestedDateTime = today.AddHours(16),
                EstimatedCost = 2500m,
                Comment = "Первое занятие по конкуру с тренером (автоподбор комплекта)",
                EquipmentCode = "AUTO",
                Status = "На рассмотрении",
                CreatedAt = today.AddHours(9)
            },
            new()
            {
                Id = 2,
                UserId = 3,
                RequesterName = "Петров Максим Игоревич",
                RequestType = "Аренда снаряжения",
                TargetName = "EQ-003 Шлем тренировочный Fouganza C500",
                HorseId = null,
                ArenaZone = "Склад амуниции",
                DurationMinutes = 45,
                HasTrainer = false,
                RequestedDateTime = today.AddHours(16),
                EstimatedCost = 200m,
                Comment = "Размер L на время занятия",
                Status = "На рассмотрении",
                CreatedAt = today.AddHours(9).AddMinutes(10)
            },
            new()
            {
                Id = 3,
                UserId = 4,
                RequesterName = "Ткаченко Кирилл Александрович",
                RequestType = "Запрос скидки",
                TargetName = "Любитель",
                HorseId = null,
                ArenaZone = "Скидка 5%",
                DurationMinutes = 0,
                HasTrainer = false,
                RequestedDateTime = today.AddHours(12),
                EstimatedCost = 0m,
                Comment = "Прошел вводный курс, прошу перевести на уровень Любитель",
                EquipmentCode = "+7 (900) 555-23-23",
                Status = "На рассмотрении",
                CreatedAt = today.AddHours(12)
            },
            new()
            {
                Id = 4,
                UserId = 4,
                RequesterName = "Ткаченко Кирилл Александрович",
                RequestType = "Тренировка и манеж",
                TargetName = "Альтаир",
                HorseId = 3,
                ArenaZone = "Учебный манеж",
                DurationMinutes = 45,
                HasTrainer = true,
                RequestedDateTime = today.AddHours(17),
                EstimatedCost = 2500m,
                Comment = "Закрепление посадки на рыси",
                EquipmentCode = "AUTO",
                Status = "Одобрено",
                CreatedAt = today.AddHours(10)
            },
            new()
            {
                Id = 5,
                UserId = 3,
                RequesterName = "Петров Максим Игоревич",
                RequestType = "Тренировка и манеж",
                TargetName = "Карамель",
                HorseId = 5,
                ArenaZone = "Учебный манеж",
                DurationMinutes = 45,
                HasTrainer = true,
                RequestedDateTime = today.AddDays(-4).AddHours(14),
                EstimatedCost = 2500m,
                Comment = "Вводное занятие",
                Status = "Одобрено",
                CreatedAt = today.AddDays(-5)
            },
            new()
            {
                Id = 6,
                UserId = 4,
                RequesterName = "Ткаченко Кирилл Александрович",
                RequestType = "Аренда снаряжения",
                TargetName = "EQ-006 Краги кожаные Tattini Close",
                HorseId = null,
                ArenaZone = "Склад амуниции",
                DurationMinutes = 45,
                HasTrainer = false,
                RequestedDateTime = today.AddDays(1).AddHours(18),
                EstimatedCost = 180m,
                Comment = "На завтрашнюю тренировку",
                EquipmentCode = "EQ-006",
                Status = "На рассмотрении",
                CreatedAt = today.AddHours(14)
            },
            new()
            {
                Id = 7,
                UserId = 3,
                RequesterName = "Петров Максим Игоревич",
                RequestType = "Бронь денника",
                TargetName = "Д-08 (Крыло Б)",
                HorseId = null,
                ArenaZone = "Стандарт",
                DurationMinutes = 0,
                HasTrainer = false,
                RequestedDateTime = today.AddDays(2).AddHours(10),
                EstimatedCost = 0m,
                Comment = "Гром — постой на 1 месяц",
                Status = "На рассмотрении",
                CreatedAt = today.AddHours(15)
            }
        };

        Penalties = new List<PenaltyItem>
        {
            new()
            {
                Id = 1,
                RiderName = "Кузнецов Артём Олегович",
                IssuedByName = "Иванова Анна Сергеевна",
                IssuerRole = "Администратор манежа",
                Category = "Задержка / переработка времени",
                Reason = "Превышение времени катания в манеже (+20 мин сверх смены)",
                TargetObject = "ПР23-КСК-2026-00003",
                Amount = 1000m,
                Status = "Не оплачен",
                CreatedAt = today.AddHours(15)
            },
            new()
            {
                Id = 2,
                RiderName = "Морозов Илья Викторович",
                IssuedByName = "Орлов Алексей Викторович",
                IssuerRole = "Главный берейтор-зоотехник",
                Category = "Самовольное кормление / денник",
                Reason = "Кормление лошади запрещённым угощением без согласования",
                TargetObject = "Зевс (Д-04)",
                Amount = 1500m,
                Status = "Не оплачен",
                CreatedAt = today.AddHours(13)
            },
            new()
            {
                Id = 3,
                RiderName = "Ткаченко Кирилл Александрович",
                IssuedByName = "Орлов Алексей Викторович",
                IssuerRole = "Главный берейтор-зоотехник",
                Category = "Нарушение правил конюшни",
                Reason = "Оставлена незакрытая калитка левады после осмотра лошадей",
                TargetObject = "Левада №1 / Д-02",
                Amount = 1000m,
                Status = "Не оплачен",
                CreatedAt = today.AddHours(11)
            },
            new()
            {
                Id = 4,
                RiderName = "Соколов Дмитрий Андреевич",
                IssuedByName = "Орлов Алексей Викторович",
                IssuerRole = "Главный берейтор-зоотехник",
                Category = "Травма / перегрузка лошади",
                Reason = "Превышение темпа галопа без разминки по твёрдому грунту",
                TargetObject = "Буцефал (CHIP-RU-2018-104)",
                Amount = 2500m,
                Status = "Оплачен",
                CreatedAt = today.AddDays(-1).AddHours(18)
            },
            new()
            {
                Id = 5,
                RiderName = "Лебедева Екатерина Павловна",
                IssuedByName = "Иванова Анна Сергеевна",
                IssuerRole = "Администратор манежа",
                Category = "Опоздание / неявка на бронь",
                Reason = "Неявка на вечернюю смену без отмены за 3 часа",
                TargetObject = "ПР23-КСК-2026-00012 (Буцефал)",
                Amount = 800m,
                Status = "Оплачен",
                CreatedAt = today.AddDays(-5).AddHours(19)
            },
            new()
            {
                Id = 6,
                RiderName = "Волкова Мария Игоревна",
                IssuedByName = "Иванова Анна Сергеевна",
                IssuerRole = "Администратор манежа",
                Category = "Порча снаряжения",
                Reason = "Повреждение ремешка застёжки на защитном шлеме",
                TargetObject = "EQ-002 Шлем защитный Uvex Perfexxion II",
                Amount = 1200m,
                Status = "Не оплачен",
                CreatedAt = today.AddHours(11)
            },
            new()
            {
                Id = 7,
                RiderName = "Ткаченко Кирилл Александрович",
                IssuedByName = "Иванова Анна Сергеевна",
                IssuerRole = "Администратор манежа",
                Category = "Порча снаряжения",
                Reason = "Утерян запасной фиксатор стремени после занятия",
                TargetObject = "EQ-003 Шлем тренировочный Fouganza C500",
                Amount = 650m,
                Status = "Оплачен",
                CreatedAt = today.AddDays(-7).AddHours(17)
            }
        };
    }

    private static RidingOrder CreateSeedOrder(
        int id,
        int year,
        int seq,
        ClubRider rider,
        HorseItem horse,
        DateTime sessionTime,
        int durationMinutes,
        bool hasTrainer,
        string status,
        string arenaZone,
        string? cancelReason = null)
    {
        decimal cost = ClubLogic.CalculateSessionCost(durationMinutes, hasTrainer, rider.DiscountRate);
        var order = new RidingOrder
        {
            Id = id,
            OrderNum = ClubLogic.FormatOrderNumber(year, seq),
            UserId = 1,
            ClubRiderId = rider.Id,
            RiderName = rider.FullName,
            RiderDiscount = rider.DiscountRate,
            OrderDate = sessionTime,
            TotalPrice = cost,
            Status = status,
            CancelReason = cancelReason,
            ArenaZone = arenaZone,
            Items = new List<RidingSessionItem>
            {
                new()
                {
                    Id = id,
                    OrderId = id,
                    HorseId = horse.Id,
                    HorseName = horse.Nickname,
                    SessionDateTime = sessionTime,
                    DurationMinutes = durationMinutes,
                    HasTrainer = hasTrainer,
                    ItemCost = cost
                }
            }
        };
        return order;
    }

    public int GetHorseDailyMinutes(int horseId, DateTime date)
    {
        return Orders
            .Where(o => o.Status != ClubLogic.OrderStatuses.CancelledVet)
            .SelectMany(o => o.Items)
            .Where(i => i.HorseId == horseId && i.SessionDateTime.Date == date.Date)
            .Sum(i => i.DurationMinutes);
    }

    public string GetHorseDailyHoursText(int horseId, DateTime date)
    {
        double hours = GetHorseDailyMinutes(horseId, date) / 60.0;
        return $"{hours:0.0} / 3.0 ч";
    }

    public ClubRider? FindRiderByName(string fullName)
    {
        return Riders.FirstOrDefault(r => r.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
    }

    public decimal GetRiderDiscount(string fullName)
    {
        return FindRiderByName(fullName)?.DiscountRate ?? 0m;
    }

    public int GetRiderSessionCount(int? riderId, string fullName)
    {
        return Orders.Count(o =>
            o.Status != ClubLogic.OrderStatuses.CancelledVet
            && ((riderId.HasValue && o.ClubRiderId == riderId.Value)
                || o.RiderName.Equals(fullName, StringComparison.OrdinalIgnoreCase)));
    }
}
