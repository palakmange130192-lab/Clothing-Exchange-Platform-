-- ReWear database setup (SQL Server / LocalDB)
-- Run this whole script once. It is safe to run again (it only creates what is missing).

IF DB_ID('ClothExchangeDb') IS NULL
    CREATE DATABASE ClothExchangeDb;
GO

-- readers do not wait for writers (avoids timeouts)
ALTER DATABASE ClothExchangeDb SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

USE ClothExchangeDb;
GO

-- 1. Users
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE Users (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(50)  NOT NULL,
    Email        NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(100) NOT NULL,
    City         NVARCHAR(50)  NOT NULL,
    CreatedAt    DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- 2. Clothes (each cloth belongs to one user)
IF OBJECT_ID('dbo.Clothes', 'U') IS NULL
CREATE TABLE Clothes (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    OwnerId     INT           NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Title       NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Category    NVARCHAR(30)  NOT NULL,
    Size        NVARCHAR(20)  NOT NULL,
    Condition   NVARCHAR(20)  NOT NULL,
    ImageUrl    NVARCHAR(500) NULL,
    Status      NVARCHAR(20)  NOT NULL DEFAULT 'Available',   -- Available / Exchanged
    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- 3. ExchangeRequests (requester wants one cloth and offers another)
-- If an old/wrong version of this table exists (different columns), remove it first
IF OBJECT_ID('dbo.ExchangeRequests', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ExchangeRequests', 'RequestedClothId') IS NULL
    DROP TABLE dbo.ExchangeRequests;
GO

IF OBJECT_ID('dbo.ExchangeRequests', 'U') IS NULL
CREATE TABLE ExchangeRequests (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    RequesterId      INT           NOT NULL FOREIGN KEY REFERENCES Users(Id),
    RequestedClothId INT           NOT NULL FOREIGN KEY REFERENCES Clothes(Id),
    OfferedClothId   INT           NOT NULL FOREIGN KEY REFERENCES Clothes(Id),
    Message          NVARCHAR(300) NULL,
    Status           NVARCHAR(20)  NOT NULL DEFAULT 'Pending',  -- Pending / Accepted / Rejected
    CreatedAt        DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- Sample data (only if the Users table is empty). Password of both users: demo123
IF NOT EXISTS (SELECT 1 FROM Users)
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, City) VALUES
    ('Sarah Jenkins', 'sarah@example.com', 'D3AD9315B7BE5DD53B31A273B3B3ABA5DEFE700808305AA16A3062B76658A791', 'Rajkot'),
    ('Alex Morgan',   'alex@example.com',  'D3AD9315B7BE5DD53B31A273B3B3ABA5DEFE700808305AA16A3062B76658A791', 'Junagadh');

    INSERT INTO Clothes (OwnerId, Title, Description, Category, Size, Condition, ImageUrl) VALUES
    (1, 'Houndstooth Blazer', 'Wool blend blazer, worn only a few times. Perfect for office or parties.', 'Women', 'M', 'Like new',
        'https://images.unsplash.com/photo-1483985988355-763728e1935b?auto=format&fit=crop&w=800&q=80'),
    (1, 'Floral Summer Dress', 'Light cotton dress with a floral print. Very comfortable.', 'Women', 'S', 'Good',
        'https://images.unsplash.com/photo-1490578474895-699cd4e2cf59?auto=format&fit=crop&w=800&q=80'),
    (2, 'Denim Jacket', 'Classic blue denim jacket, slightly faded, still strong.', 'Men', 'L', 'Good',
        'https://images.unsplash.com/photo-1514090458221-65bb69cf63e6?auto=format&fit=crop&w=800&q=80'),
    (2, 'Warm Winter Coat', 'Long black winter coat, very warm, no damage.', 'Winter Wear', 'XL', 'Like new',
        'https://images.unsplash.com/photo-1485230895905-ec40ba36b9bc?auto=format&fit=crop&w=800&q=80');
END
GO
