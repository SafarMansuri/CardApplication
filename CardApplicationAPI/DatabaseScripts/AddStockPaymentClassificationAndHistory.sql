SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.StockPaymentInfo', 'CompletedAt') IS NULL
    ALTER TABLE dbo.StockPaymentInfo ADD CompletedAt datetime2 NULL;

IF COL_LENGTH('dbo.StockPaymentInfo', 'CompletedByUserId') IS NULL
    ALTER TABLE dbo.StockPaymentInfo ADD CompletedByUserId int NULL;

IF COL_LENGTH('dbo.StockPaymentInfo', 'CompletedByUsername') IS NULL
    ALTER TABLE dbo.StockPaymentInfo ADD CompletedByUsername nvarchar(100) NULL;

IF COL_LENGTH('dbo.StockPaymentInfo', 'OutstandingAmount') IS NULL
    ALTER TABLE dbo.StockPaymentInfo ADD OutstandingAmount decimal(18,2) NOT NULL
        CONSTRAINT DF_StockPaymentInfo_OutstandingAmount DEFAULT 0;

IF COL_LENGTH('dbo.StockPaymentInfo', 'PaymentStatus') IS NULL
    ALTER TABLE dbo.StockPaymentInfo ADD PaymentStatus nvarchar(20) NOT NULL
        CONSTRAINT DF_StockPaymentInfo_PaymentStatus DEFAULT 'Pending';

IF COL_LENGTH('dbo.StockPaymentInfo', 'PaymentType') IS NULL
    ALTER TABLE dbo.StockPaymentInfo ADD PaymentType nvarchar(20) NOT NULL
        CONSTRAINT DF_StockPaymentInfo_PaymentType DEFAULT 'Payable';

UPDATE payment
SET payment.PaymentType = CASE
        WHEN UPPER(COALESCE(stock.[IN/OUT], '')) = 'OUT' THEN 'Receivable'
        ELSE 'Payable'
    END,
    payment.PaymentStatus = CASE
        WHEN payment.PaymentStatus = 'Completed' THEN 'Completed'
        ELSE 'Pending'
    END,
    payment.OutstandingAmount = CASE
        WHEN payment.PaymentStatus = 'Completed' THEN 0
        ELSE COALESCE(stock.[AMOUNT], 0)
    END,
    payment.[PARTY NAME] = CASE
        WHEN UPPER(COALESCE(stock.[IN/OUT], '')) = 'OUT'
            THEN NULLIF(LTRIM(RTRIM(stock.[SOLD TO])), '')
        ELSE NULLIF(LTRIM(RTRIM(stock.[PARTY NAME])), '')
    END,
    payment.[ACCOUNT NAME] = CASE
        WHEN UPPER(COALESCE(stock.[IN/OUT], '')) = 'OUT'
            THEN NULLIF(LTRIM(RTRIM(stock.[SOLD TO])), '')
        ELSE NULLIF(LTRIM(RTRIM(stock.[PARTY NAME])), '')
    END,
    payment.UpdatedAt = SYSUTCDATETIME()
FROM dbo.StockPaymentInfo payment
INNER JOIN dbo.StockItems stock ON stock.Id = payment.StockItemId;

IF OBJECT_ID('dbo.StockPaymentHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockPaymentHistory
    (
        Id int IDENTITY(1,1) NOT NULL,
        StockPaymentInfoId int NOT NULL,
        EventType nvarchar(20) NOT NULL,
        Amount decimal(18,2) NOT NULL,
        TransactionDate datetime2 NOT NULL,
        PaymentMethod nvarchar(50) NULL,
        ReferenceNo nvarchar(100) NULL,
        Remarks nvarchar(500) NULL,
        PerformedByUserId int NOT NULL,
        PerformedByUsername nvarchar(100) NOT NULL,
        CreatedAt datetime2 NOT NULL,
        CONSTRAINT PK_StockPaymentHistory PRIMARY KEY (Id),
        CONSTRAINT FK_StockPaymentHistory_StockPaymentInfo_StockPaymentInfoId
            FOREIGN KEY (StockPaymentInfoId)
            REFERENCES dbo.StockPaymentInfo(Id)
            ON DELETE CASCADE,
        CONSTRAINT CK_StockPaymentHistory_EventType
            CHECK (EventType IN ('Settlement', 'Reset'))
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_StockPaymentInfo_OutstandingAmount'
      AND parent_object_id = OBJECT_ID('dbo.StockPaymentInfo')
)
    ALTER TABLE dbo.StockPaymentInfo
        ADD CONSTRAINT CK_StockPaymentInfo_OutstandingAmount
        CHECK (OutstandingAmount >= 0);

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_StockPaymentInfo_PaymentStatus'
      AND parent_object_id = OBJECT_ID('dbo.StockPaymentInfo')
)
    ALTER TABLE dbo.StockPaymentInfo
        ADD CONSTRAINT CK_StockPaymentInfo_PaymentStatus
        CHECK (PaymentStatus IN ('Pending', 'Completed'));

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_StockPaymentInfo_PaymentType'
      AND parent_object_id = OBJECT_ID('dbo.StockPaymentInfo')
)
    ALTER TABLE dbo.StockPaymentInfo
        ADD CONSTRAINT CK_StockPaymentInfo_PaymentType
        CHECK (PaymentType IN ('Payable', 'Receivable'));

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StockPaymentHistory_StockPaymentInfoId_CreatedAt'
      AND object_id = OBJECT_ID('dbo.StockPaymentHistory')
)
    CREATE INDEX IX_StockPaymentHistory_StockPaymentInfoId_CreatedAt
        ON dbo.StockPaymentHistory(StockPaymentInfoId, CreatedAt);

IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM dbo.__EFMigrationsHistory
       WHERE MigrationId = '20261010160000_AddStockPaymentClassificationAndHistory'
   )
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory(MigrationId, ProductVersion)
    VALUES ('20261010160000_AddStockPaymentClassificationAndHistory', '10.0.12');
END;

COMMIT TRANSACTION;
