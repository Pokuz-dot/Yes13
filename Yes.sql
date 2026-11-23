SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

CREATE DATABASE IF NOT EXISTS `Yes` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `Yes`;

DROP TABLE IF EXISTS `client_requests`;
DROP TABLE IF EXISTS `riding_session_items`;
DROP TABLE IF EXISTS `riding_orders`;
DROP TABLE IF EXISTS `horses`;
DROP TABLE IF EXISTS `club_riders`;
DROP TABLE IF EXISTS `horse_breeds`;
DROP TABLE IF EXISTS `users`;
DROP TABLE IF EXISTS `roles`;

-- 1. Роли пользователей
CREATE TABLE `roles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `roles` (`id`, `name`) VALUES
(1, 'Главный берейтор-зоотехник'),
(2, 'Администратор манежа'),
(3, 'Клиент');

-- 2. Пользователи системы (пароль у тестовых аккаунтов: 123456)
CREATE TABLE `users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `role_id` int(11) NOT NULL,
  `login` varchar(50) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `full_name` varchar(150) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `login` (`login`),
  KEY `fk_users_role` (`role_id`),
  CONSTRAINT `fk_users_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `users` (`id`, `role_id`, `login`, `password_hash`, `full_name`) VALUES
(1, 1, 'bereitor', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'Орлов Алексей Викторович'),
(2, 2, 'admin', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'Иванова Анна Сергеевна'),
(3, 3, 'client', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'Петров Максим Игоревич');

-- 3. Породы лошадей
CREATE TABLE `horse_breeds` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `breed_name` varchar(100) NOT NULL,
  `temperament` varchar(100) DEFAULT NULL,
  `origin_country` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `horse_breeds` (`id`, `breed_name`, `temperament`, `origin_country`) VALUES
(1, 'Ганноверская', 'Уравновешенный, конкурный', 'Германия'),
(2, 'Тракененская', 'Спортивный, выносливый', 'Германия'),
(3, 'Орловский рысак', 'Послушный, учебный', 'Россия');

-- 4. Лошади
CREATE TABLE `horses` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `breed_id` int(11) NOT NULL,
  `nickname` varchar(100) NOT NULL,
  `passport_chip_code` varchar(50) NOT NULL,
  `birth_year` year(4) NOT NULL,
  `suit_color` varchar(50) DEFAULT NULL,
  `is_ready_for_training` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `passport_chip_code` (`passport_chip_code`),
  KEY `fk_horses_breed` (`breed_id`),
  CONSTRAINT `fk_horses_breed` FOREIGN KEY (`breed_id`) REFERENCES `horse_breeds` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `horses` (`id`, `breed_id`, `nickname`, `passport_chip_code`, `birth_year`, `suit_color`, `is_ready_for_training`) VALUES
(1, 1, 'Буцефал', 'CHIP-RU-2018-104', 2018, 'Вороная', 1),
(2, 2, 'Гроза', 'CHIP-RU-2019-219', 2019, 'Гнедая', 1),
(3, 3, 'Зевс', 'CHIP-RU-2017-088', 2017, 'Серая в яблоках', 0);

-- 5. Всадники клуба
CREATE TABLE `club_riders` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `full_name` varchar(150) NOT NULL,
  `phone` varchar(20) NOT NULL,
  `skill_level` varchar(50) DEFAULT NULL,
  `discount_rate` decimal(5,2) NOT NULL DEFAULT 0.00,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `club_riders` (`id`, `full_name`, `phone`, `skill_level`, `discount_rate`) VALUES
(1, 'Соколов Дмитрий Андреевич', '+7 (903) 412-55-10', 'Спортсмен (конкур)', 15.00),
(2, 'Волкова Мария Игоревна', '+7 (916) 784-19-22', 'Опытный всадник', 10.00),
(3, 'Петров Максим Игоревич', '+7 (925) 330-81-04', 'Начинающий', 0.00);

-- 6. Заказы на тренировки и аренду манежа
CREATE TABLE `riding_orders` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `order_num` varchar(30) NOT NULL,
  `user_id` int(11) NOT NULL,
  `club_rider_id` int(11) NOT NULL,
  `order_date` datetime NOT NULL DEFAULT current_timestamp(),
  `total_price` decimal(10,2) NOT NULL DEFAULT 0.00,
  `status` enum('Забронировано','Лошадь оседлана','Тренировка в манеже','Завершена успешно','Отмена по ветпоказаниям') NOT NULL DEFAULT 'Забронировано',
  `cancel_reason` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `order_num` (`order_num`),
  KEY `fk_orders_user` (`user_id`),
  KEY `fk_orders_rider` (`club_rider_id`),
  CONSTRAINT `fk_orders_rider` FOREIGN KEY (`club_rider_id`) REFERENCES `club_riders` (`id`) ON UPDATE CASCADE,
  CONSTRAINT `fk_orders_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `riding_orders` (`id`, `order_num`, `user_id`, `club_rider_id`, `order_date`, `total_price`, `status`, `cancel_reason`) VALUES
(1, 'ПР23-КСК-2026-00001', 2, 1, '2026-11-23 10:00:00', 3400.00, 'Тренировка в манеже', NULL),
(2, 'ПР23-КСК-2026-00002', 2, 2, '2026-11-23 11:30:00', 2250.00, 'Лошадь оседлана', NULL),
(3, 'ПР23-КСК-2026-00003', 2, 3, '2026-11-23 14:00:00', 1500.00, 'Забронировано', NULL);

-- 7. Позиции тренировочных сессий
CREATE TABLE `riding_session_items` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `order_id` int(11) NOT NULL,
  `horse_id` int(11) NOT NULL,
  `session_datetime` datetime NOT NULL,
  `duration_minutes` int(11) NOT NULL DEFAULT 45,
  `has_trainer` tinyint(1) NOT NULL DEFAULT 0,
  `item_cost` decimal(10,2) NOT NULL DEFAULT 0.00,
  PRIMARY KEY (`id`),
  KEY `fk_items_order` (`order_id`),
  KEY `fk_items_horse` (`horse_id`),
  CONSTRAINT `fk_items_horse` FOREIGN KEY (`horse_id`) REFERENCES `horses` (`id`) ON UPDATE CASCADE,
  CONSTRAINT `fk_items_order` FOREIGN KEY (`order_id`) REFERENCES `riding_orders` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `riding_session_items` (`id`, `order_id`, `horse_id`, `session_datetime`, `duration_minutes`, `has_trainer`, `item_cost`) VALUES
(1, 1, 1, '2026-11-23 10:00:00', 90, 1, 3400.00),
(2, 2, 2, '2026-11-23 11:30:00', 45, 1, 2250.00),
(3, 3, 2, '2026-11-23 14:00:00', 45, 0, 1500.00);

-- 8. Заявки пользователей на одобрение администратору
CREATE TABLE `client_requests` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) NOT NULL,
  `requester_name` varchar(150) NOT NULL,
  `request_type` varchar(60) NOT NULL,
  `target_name` varchar(150) NOT NULL,
  `horse_id` int(11) DEFAULT NULL,
  `arena_zone` varchar(100) NOT NULL DEFAULT 'Конкурный манеж',
  `duration_minutes` int(11) NOT NULL DEFAULT 45,
  `has_trainer` tinyint(1) NOT NULL DEFAULT 1,
  `requested_datetime` datetime NOT NULL,
  `estimated_cost` decimal(10,2) NOT NULL DEFAULT 0.00,
  `comment` varchar(255) DEFAULT NULL,
  `equipment_code` varchar(40) DEFAULT NULL,
  `status` varchar(40) NOT NULL DEFAULT 'На рассмотрении',
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `fk_requests_user` (`user_id`),
  CONSTRAINT `fk_requests_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `client_requests` (`id`, `user_id`, `requester_name`, `request_type`, `target_name`, `horse_id`, `arena_zone`, `duration_minutes`, `has_trainer`, `requested_datetime`, `estimated_cost`, `comment`, `equipment_code`, `status`) VALUES
(1, 3, 'Петров Максим Игоревич', 'Тренировка и манеж', 'Буцефал', 1, 'Конкурный манеж', 45, 1, '2026-11-23 16:00:00', 2500.00, 'Первое занятие по конкуру с тренером', 'AUTO', 'На рассмотрении'),
(2, 3, 'Петров Максим Игоревич', 'Аренда снаряжения', 'EQ-003 Шлем тренировочный Fouganza C500', NULL, 'Склад амуниции', 45, 0, '2026-11-23 16:00:00', 200.00, 'Размер L на время занятия', 'EQ-003', 'На рассмотрении');

COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
