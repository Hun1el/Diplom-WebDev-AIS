-- MySqlBackup.NET 2.7.0.0
-- Dump Time: 2026-06-09 13:21:59
-- --------------------------------------
-- Server version 8.0.30 MySQL Community Server - GPL


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- 
-- Definition of Category
-- 

DROP TABLE IF EXISTS `Category`;
CREATE TABLE IF NOT EXISTS `Category` (
  `CategoryID` int NOT NULL AUTO_INCREMENT,
  `CategoryName` varchar(45) NOT NULL,
  PRIMARY KEY (`CategoryID`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 
-- Dumping data for table Category
-- 

/*!40000 ALTER TABLE `Category` DISABLE KEYS */;
INSERT INTO `Category`(`CategoryID`,`CategoryName`) VALUES(1,'Сайт-визитка'),(2,'Интернет-магазин'),(3,'Корпоративный сайт'),(4,'Лендинг (Landing Page)'),(5,'Портфолио'),(6,'Блог'),(7,'Информационный портал'),(8,'Новостной сайт'),(9,'Форум'),(10,'Персональный сайт'),(11,'Промо-сайт'),(12,'Образовательный портал');
/*!40000 ALTER TABLE `Category` ENABLE KEYS */;

-- 
-- Definition of Clients
-- 

DROP TABLE IF EXISTS `Clients`;
CREATE TABLE IF NOT EXISTS `Clients` (
  `ClientID` int NOT NULL AUTO_INCREMENT,
  `Surname` varchar(90) NOT NULL,
  `FirstName` varchar(75) NOT NULL,
  `MiddleName` varchar(90) DEFAULT NULL,
  `Email` varchar(100) NOT NULL,
  `PhoneNumber` varchar(20) NOT NULL,
  PRIMARY KEY (`ClientID`)
) ENGINE=InnoDB AUTO_INCREMENT=55 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 
-- Dumping data for table Clients
-- 

/*!40000 ALTER TABLE `Clients` DISABLE KEYS */;

/*!40000 ALTER TABLE `Clients` ENABLE KEYS */;

-- 
-- Definition of Product
-- 

DROP TABLE IF EXISTS `Product`;
CREATE TABLE IF NOT EXISTS `Product` (
  `ProductID` int NOT NULL AUTO_INCREMENT,
  `ProductName` varchar(75) NOT NULL,
  `ProductDescription` text NOT NULL,
  `ProductPhoto` text,
  `CategoryID` int NOT NULL,
  `BasePrice` decimal(9,2) NOT NULL,
  PRIMARY KEY (`ProductID`),
  KEY `fk_product_category_idx` (`CategoryID`),
  CONSTRAINT `fk_product_category` FOREIGN KEY (`CategoryID`) REFERENCES `Category` (`CategoryID`)
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 
-- Dumping data for table Product
-- 

/*!40000 ALTER TABLE `Product` DISABLE KEYS */;

/*!40000 ALTER TABLE `Product` ENABLE KEYS */;

-- 
-- Definition of OrderProduct
-- 

DROP TABLE IF EXISTS `OrderProduct`;
CREATE TABLE IF NOT EXISTS `OrderProduct` (
  `OrderID` int NOT NULL,
  `ProductID` int NOT NULL,
  `ProductCount` int NOT NULL,
  `ProductPrice` decimal(12,2) NOT NULL,
  PRIMARY KEY (`OrderID`,`ProductID`),
  KEY `fk_orderproduct_product_idx` (`ProductID`),
  KEY `fk_orderproduct_order_idx` (`OrderID`),
  CONSTRAINT `fk_orderproduct_order` FOREIGN KEY (`OrderID`) REFERENCES `Order` (`OrderID`),
  CONSTRAINT `fk_orderproduct_product` FOREIGN KEY (`ProductID`) REFERENCES `Product` (`ProductID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 
-- Dumping data for table OrderProduct
-- 

/*!40000 ALTER TABLE `OrderProduct` DISABLE KEYS */;

/*!40000 ALTER TABLE `OrderProduct` ENABLE KEYS */;

-- 
-- Definition of Role
-- 

DROP TABLE IF EXISTS `Role`;
CREATE TABLE IF NOT EXISTS `Role` (
  `RoleID` int NOT NULL AUTO_INCREMENT,
  `RoleName` varchar(30) NOT NULL,
  PRIMARY KEY (`RoleID`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 
-- Dumping data for table Role
-- 

/*!40000 ALTER TABLE `Role` DISABLE KEYS */;
INSERT INTO `Role`(`RoleID`,`RoleName`) VALUES(1,'Администратор'),(2,'Менеджер'),(3,'Директор');
/*!40000 ALTER TABLE `Role` ENABLE KEYS */;

-- 
-- Definition of Status
-- 

DROP TABLE IF EXISTS `Status`;
CREATE TABLE IF NOT EXISTS `Status` (
  `StatusID` int NOT NULL AUTO_INCREMENT,
  `StatusName` varchar(30) NOT NULL,
  PRIMARY KEY (`StatusID`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 
-- Dumping data for table Status
-- 

/*!40000 ALTER TABLE `Status` DISABLE KEYS */;
INSERT INTO `Status`(`StatusID`,`StatusName`) VALUES(1,'Новый'),(2,'В работе'),(3,'Завершён'),(4,'Отменён');
/*!40000 ALTER TABLE `Status` ENABLE KEYS */;

-- 
-- Definition of Users
-- 

DROP TABLE IF EXISTS `Users`;
CREATE TABLE IF NOT EXISTS `Users` (
  `UserID` int NOT NULL AUTO_INCREMENT,
  `Surname` varchar(90) NOT NULL,
  `FirstName` varchar(70) NOT NULL,
  `MiddleName` varchar(90) DEFAULT NULL,
  `UserLogin` varchar(20) NOT NULL,
  `UserPassword` varchar(255) NOT NULL,
  `RoleID` int NOT NULL,
  `PhoneNumber` varchar(20) NOT NULL,
  PRIMARY KEY (`UserID`),
  UNIQUE KEY `UserLogin_UNIQUE` (`UserLogin`),
  UNIQUE KEY `UserPassword_UNIQUE` (`UserPassword`),
  KEY `fk_user_role_idx` (`RoleID`),
  CONSTRAINT `fk_user_role` FOREIGN KEY (`RoleID`) REFERENCES `Role` (`RoleID`)
) ENGINE=InnoDB AUTO_INCREMENT=45 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 
-- Dumping data for table Users
-- 

/*!40000 ALTER TABLE `Users` DISABLE KEYS */;

/*!40000 ALTER TABLE `Users` ENABLE KEYS */;

-- 
-- Definition of Order
-- 

DROP TABLE IF EXISTS `Order`;
CREATE TABLE IF NOT EXISTS `Order` (
  `OrderID` int NOT NULL AUTO_INCREMENT,
  `UserID` int NOT NULL,
  `ClientID` int NOT NULL,
  `OrderDate` date NOT NULL,
  `OrderCompDate` date NOT NULL,
  `StatusID` int NOT NULL,
  `OrderCost` decimal(12,2) NOT NULL,
  `Discount` decimal(12,2) DEFAULT '0.00',
  `Surcharge` decimal(12,2) DEFAULT '0.00',
  PRIMARY KEY (`OrderID`),
  KEY `fk_order_user_idx` (`UserID`),
  KEY `fk_order_client_idx` (`ClientID`),
  KEY `fk_order_status_idx` (`StatusID`),
  CONSTRAINT `fk_order_client` FOREIGN KEY (`ClientID`) REFERENCES `Clients` (`ClientID`),
  CONSTRAINT `fk_order_status` FOREIGN KEY (`StatusID`) REFERENCES `Status` (`StatusID`),
  CONSTRAINT `fk_order_user` FOREIGN KEY (`UserID`) REFERENCES `Users` (`UserID`)
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 
-- Dumping data for table Order
-- 

/*!40000 ALTER TABLE `Order` DISABLE KEYS */;

/*!40000 ALTER TABLE `Order` ENABLE KEYS */;


/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;
/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;


-- Dump completed on 2026-06-09 13:21:59
-- Total time: 0:0:0:0:174 (d:h:m:s:ms)
