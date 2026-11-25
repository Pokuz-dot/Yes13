SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

SET NAMES utf8mb4;

CREATE DATABASE IF NOT EXISTS `Yes` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `Yes`;

DROP TABLE IF EXISTS `penalties`;
DROP TABLE IF EXISTS `client_requests`;
DROP TABLE IF EXISTS `feeding_rations`;
DROP TABLE IF EXISTS `stalls`;
DROP TABLE IF EXISTS `equipment_items`;
DROP TABLE IF EXISTS `riding_session_items`;
DROP TABLE IF EXISTS `riding_orders`;
DROP TABLE IF EXISTS `horses`;
DROP TABLE IF EXISTS `club_riders`;
DROP TABLE IF EXISTS `horse_breeds`;
DROP TABLE IF EXISTS `users`;
DROP TABLE IF EXISTS `roles`;

-- роли
CREATE TABLE `roles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- пользователи
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

-- породы лошадей
CREATE TABLE `horse_breeds` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `breed_name` varchar(100) NOT NULL,
  `temperament` varchar(100) DEFAULT NULL,
  `origin_country` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- лошади
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

-- всадники
CREATE TABLE `club_riders` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `full_name` varchar(150) NOT NULL,
  `phone` varchar(20) NOT NULL,
  `skill_level` varchar(50) DEFAULT NULL,
  `discount_rate` decimal(5,2) NOT NULL DEFAULT 0.00,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- заказы
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

-- позиции тренировок
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

-- снаряжение
CREATE TABLE `equipment_items` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `inventory_code` varchar(30) NOT NULL,
  `name` varchar(150) NOT NULL,
  `category` varchar(60) NOT NULL,
  `size` varchar(60) NOT NULL,
  `price_per_hour` decimal(10,2) NOT NULL DEFAULT 0.00,
  `status` varchar(40) NOT NULL DEFAULT 'Свободно',
  `rented_by_rider` varchar(150) NOT NULL DEFAULT '-',
  `order_num` varchar(40) NOT NULL DEFAULT '-',
  PRIMARY KEY (`id`),
  UNIQUE KEY `inventory_code` (`inventory_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- денники
CREATE TABLE `stalls` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `stall_code` varchar(30) NOT NULL,
  `wing_section` varchar(80) NOT NULL,
  `stall_type` varchar(80) NOT NULL,
  `horse_id` int(11) DEFAULT NULL,
  `horse_name` varchar(120) NOT NULL DEFAULT 'Свободен',
  `breed_name` varchar(100) NOT NULL DEFAULT '-',
  `owner_or_boarding_type` varchar(150) NOT NULL DEFAULT '-',
  `reserved_until` varchar(60) NOT NULL DEFAULT '-',
  `status` varchar(40) NOT NULL DEFAULT 'Свободен',
  PRIMARY KEY (`id`),
  UNIQUE KEY `stall_code` (`stall_code`),
  KEY `fk_stalls_horse` (`horse_id`),
  CONSTRAINT `fk_stalls_horse` FOREIGN KEY (`horse_id`) REFERENCES `horses` (`id`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- рационы
CREATE TABLE `feeding_rations` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `horse_id` int(11) NOT NULL,
  `horse_name` varchar(120) NOT NULL,
  `stall_code` varchar(30) NOT NULL,
  `morning_feed` varchar(200) NOT NULL,
  `day_feed` varchar(200) NOT NULL,
  `evening_feed` varchar(200) NOT NULL,
  `diet_note` varchar(120) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_rations_horse` (`horse_id`),
  CONSTRAINT `fk_rations_horse` FOREIGN KEY (`horse_id`) REFERENCES `horses` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- заявки
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

-- штрафы
CREATE TABLE `penalties` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `rider_name` varchar(150) NOT NULL,
  `issued_by_name` varchar(150) NOT NULL,
  `issuer_role` varchar(60) NOT NULL,
  `category` varchar(60) NOT NULL,
  `reason` varchar(255) NOT NULL,
  `target_object` varchar(150) NOT NULL DEFAULT '-',
  `amount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `status` varchar(40) NOT NULL DEFAULT 'Не оплачен',
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

COMMIT;
