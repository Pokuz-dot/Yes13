using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Avalo;

namespace Avalo.Tests;

public class ClubLogicTests
{
    [Fact]
    public void Test1_StandardSessionCostAndOrderTotal_CalculatedCorrectly()
    {
        Assert.Equal(1500m, ClubLogic.CalculateSessionCost(45, hasTrainer: false, discountRate: 0m));
        Assert.Equal(2500m, ClubLogic.CalculateSessionCost(45, hasTrainer: true, discountRate: 0m));
        Assert.Equal(4000m, ClubLogic.CalculateSessionCost(90, hasTrainer: true, discountRate: 0m));
        Assert.Equal(4500m, ClubLogic.CalculateSessionCost(135, hasTrainer: false, discountRate: 0m));

        Assert.Equal("ПР23-КСК-2026-00042", ClubLogic.FormatOrderNumber(2026, 42));

        var order = new RidingOrder
        {
            Id = 1,
            OrderNum = ClubLogic.FormatOrderNumber(2026, 42),
            RiderName = "Кузнецов Артём Олегович",
            RiderDiscount = 0m,
            OrderDate = new DateTime(2026, 11, 25, 14, 0, 0),
            Status = ClubLogic.OrderStatuses.Booked
        };

        ClubLogic.AddSessionItem(order, new RidingSessionItem
        {
            Id = 1,
            HorseId = 1,
            HorseName = "Буцефал",
            DurationMinutes = 45,
            HasTrainer = false,
            SessionDateTime = order.OrderDate
        }, horseReadyForTraining: true, existingHorseDailyMinutes: 0);

        ClubLogic.AddSessionItem(order, new RidingSessionItem
        {
            Id = 2,
            HorseId = 2,
            HorseName = "Гроза",
            DurationMinutes = 90,
            HasTrainer = true,
            SessionDateTime = order.OrderDate.AddHours(1)
        }, horseReadyForTraining: true, existingHorseDailyMinutes: 0);

        Assert.Equal(2, order.Items.Count);
        Assert.Equal(1500m, order.Items[0].ItemCost);
        Assert.Equal(4000m, order.Items[1].ItemCost);
        Assert.Equal(5500m, order.TotalPrice);
    }

    [Fact]
    public void Test2_DiscountsTrainerSurchargeAndPenalties_AppliedCorrectly()
    {
        Assert.Equal(5m, ClubLogic.GetDiscountForSkill("Любитель"));
        Assert.Equal(10m, ClubLogic.GetDiscountForSkill("Опытный всадник"));
        Assert.Equal(15m, ClubLogic.GetDiscountForSkill("Спортсмен (конкур)"));
        Assert.Equal(20m, ClubLogic.GetDiscountForSkill("Льготный / Безлимит"));

        Assert.Equal(3600m, ClubLogic.CalculateSessionCost(90, hasTrainer: true, discountRate: 10m));
        Assert.Equal(2125m, ClubLogic.CalculateSessionCost(45, hasTrainer: true, discountRate: 15m));
        decimal withTrainer = ClubLogic.CalculateSessionCost(90, hasTrainer: true, discountRate: 20m);
        decimal withoutTrainer = ClubLogic.CalculateSessionCost(90, hasTrainer: false, discountRate: 20m);
        Assert.Equal(3200m, withTrainer);
        Assert.Equal(2400m, withoutTrainer);
        Assert.Equal(800m, withTrainer - withoutTrainer);

        var penalties = new List<PenaltyItem>
        {
            new() { Id = 1, RiderName = "Соколов Дмитрий Андреевич", Amount = 1000m, Status = "Не оплачен" },
            new() { Id = 2, RiderName = "Соколов Дмитрий Андреевич", Amount = 1500m, Status = "Не оплачен" },
            new() { Id = 3, RiderName = "Соколов Дмитрий Андреевич", Amount = 2500m, Status = "Оплачен" },
            new() { Id = 4, RiderName = "Волкова Мария Игоревна", Amount = 1200m, Status = "Не оплачен" }
        };

        Assert.Equal(2500m, ClubLogic.CalculateUnpaidPenaltiesTotal(penalties, "Соколов Дмитрий Андреевич"));
    }

    [Fact]
    public void Test3_OrderFsm_ValidTransitionChain_Succeeds()
    {
        var order = new RidingOrder
        {
            Id = 10,
            OrderNum = ClubLogic.FormatOrderNumber(2026, 10),
            RiderName = "Волкова Мария Игоревна",
            Status = ClubLogic.OrderStatuses.Booked
        };

        ClubLogic.TransitionOrderStatus(order, ClubLogic.OrderStatuses.Saddled, horseReadyForTraining: true);
        Assert.Equal(ClubLogic.OrderStatuses.Saddled, order.Status);

        ClubLogic.TransitionOrderStatus(order, ClubLogic.OrderStatuses.InArena, horseReadyForTraining: true);
        Assert.Equal(ClubLogic.OrderStatuses.InArena, order.Status);

        ClubLogic.TransitionOrderStatus(order, ClubLogic.OrderStatuses.Completed, horseReadyForTraining: true);
        Assert.Equal(ClubLogic.OrderStatuses.Completed, order.Status);
        Assert.True(ClubLogic.IsClosedStatus(order.Status));

        var cancelledOrder = new RidingOrder
        {
            Id = 11,
            OrderNum = ClubLogic.FormatOrderNumber(2026, 11),
            RiderName = "Морозов Илья Викторович",
            Status = ClubLogic.OrderStatuses.Saddled
        };

        ClubLogic.TransitionOrderStatus(
            cancelledOrder,
            ClubLogic.OrderStatuses.CancelledVet,
            horseReadyForTraining: false,
            cancelReason: "Ветеринарный осмотр копытного сустава");

        Assert.Equal(ClubLogic.OrderStatuses.CancelledVet, cancelledOrder.Status);
        Assert.Equal("Ветеринарный осмотр копытного сустава", cancelledOrder.CancelReason);
        Assert.True(ClubLogic.IsClosedStatus(cancelledOrder.Status));
    }

    [Fact]
    public void Test4_InvalidFsmOrClosedOrderModification_ThrowsInvalidOperationException()
    {
        var bookedOrder = new RidingOrder
        {
            Id = 20,
            OrderNum = ClubLogic.FormatOrderNumber(2026, 20),
            Status = ClubLogic.OrderStatuses.Booked
        };

        Assert.Throws<InvalidOperationException>(() =>
            ClubLogic.TransitionOrderStatus(bookedOrder, ClubLogic.OrderStatuses.Completed, horseReadyForTraining: true));

        Assert.Throws<InvalidOperationException>(() =>
            ClubLogic.TransitionOrderStatus(bookedOrder, ClubLogic.OrderStatuses.Saddled, horseReadyForTraining: false));

        var closedOrder = new RidingOrder
        {
            Id = 21,
            OrderNum = ClubLogic.FormatOrderNumber(2026, 21),
            RiderName = "Лебедева Екатерина Павловна",
            Status = ClubLogic.OrderStatuses.Completed,
            TotalPrice = 3200m
        };

        Assert.Throws<InvalidOperationException>(() =>
            ClubLogic.TransitionOrderStatus(closedOrder, ClubLogic.OrderStatuses.Booked, horseReadyForTraining: true));

        Assert.Throws<InvalidOperationException>(() =>
            ClubLogic.EnsureOrderEditable(closedOrder));

        Assert.Throws<InvalidOperationException>(() =>
            ClubLogic.AddSessionItem(closedOrder, new RidingSessionItem
            {
                HorseId = 1,
                HorseName = "Буцефал",
                DurationMinutes = 45,
                HasTrainer = false
            }, horseReadyForTraining: true));

        Assert.Throws<InvalidOperationException>(() =>
            ClubLogic.AddSessionItem(bookedOrder, new RidingSessionItem
            {
                HorseId = 1,
                HorseName = "Буцефал",
                DurationMinutes = 90,
                HasTrainer = false
            }, horseReadyForTraining: true, existingHorseDailyMinutes: 135));
    }

    [Fact]
    public void Test5_RouteSheetExportAndSha256_ConsistentAcrossCultures()
    {
        var orderDate = new DateTime(2026, 11, 25, 16, 30, 0);
        var order = new RidingOrder
        {
            Id = 30,
            OrderNum = "ПР23-КСК-2026-00030",
            RiderName = "Соколов Дмитрий Андреевич",
            RiderDiscount = 15m,
            OrderDate = orderDate,
            TotalPrice = 3400m,
            ArenaZone = "Конкурный манеж",
            Status = ClubLogic.OrderStatuses.Booked,
            Items =
            {
                new RidingSessionItem
                {
                    Id = 30,
                    OrderId = 30,
                    HorseId = 1,
                    HorseName = "Буцефал",
                    SessionDateTime = orderDate,
                    DurationMinutes = 90,
                    HasTrainer = true,
                    ItemCost = 3400m
                }
            }
        };

        string expectedRaw = "ПР23-КСК-2026-0003025.11.2026 16:30Соколов Дмитрий Андреевич3400.001";
        string expectedSha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(expectedRaw))).ToLowerInvariant();

        var prevCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            string shaRu = ClubLogic.ComputeDocumentSha256(order.OrderNum, order.OrderDate, order.RiderName, order.TotalPrice, order.Items.Count);
            string sheetRu = ClubLogic.BuildRouteSheetText(order);

            Assert.Equal(expectedSha, shaRu);
            Assert.Contains("[ПР23-КСК-2026-00030]+[25.11.2026 16:30]+[Соколов Дмитрий Андреевич]+[3400.00]+[1]", sheetRu);
            Assert.Contains(expectedSha, sheetRu);
            Assert.Contains("АКТ ПРОВЕРКИ СНАРЯЖЕНИЯ", sheetRu);
            Assert.Contains("Инструктор по верховой езде:", sheetRu);
        }
        finally
        {
            CultureInfo.CurrentCulture = prevCulture;
        }
    }
}
