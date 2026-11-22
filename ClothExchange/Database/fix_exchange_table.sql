-- Run this if the "Exchange requests" page says: Invalid column name 'RequestedClothId'
-- It deletes the wrong ExchangeRequests table and creates the correct one.
USE ClothExchangeDb;
GO

IF OBJECT_ID('dbo.ExchangeRequests', 'U') IS NOT NULL
    DROP TABLE dbo.ExchangeRequests;
GO

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
