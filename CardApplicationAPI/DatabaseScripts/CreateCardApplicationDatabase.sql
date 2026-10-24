SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 50001, 'Select the target application database before running this script. System databases are not supported.', 1;

IF @@TRANCOUNT <> 0
    THROW 50002, 'Run this script outside an existing transaction.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1
        FROM sys.objects
        WHERE [name] IN (
            N'BankAccounts', N'BankTransactions', N'CardInfo', N'CashLedger',
            N'PaymentInfo', N'RefreshTokens', N'StockItems', N'StockPaymentInfo',
            N'StockPaymentHistory', N'Users', N'__EFMigrationsHistory'
        )
        AND [type] IN (N'U', N'V', N'SN')
    )
        THROW 50003, 'Fresh installation only: an application object or EF migration history already exists. No existing schema or data will be changed. Use a reviewed upgrade migration instead.', 1;

    CREATE TABLE [dbo].[BankAccounts] (
        [Id] int NOT NULL IDENTITY,
        [AccountName] nvarchar(150) NOT NULL,
        [BankName] nvarchar(150) NOT NULL,
        [AccountNo] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BankAccounts] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[CardInfo] (
        [Id] int NOT NULL IDENTITY,
        [CardName] nvarchar(100) NOT NULL,
        [CardNumber] nvarchar(100) NOT NULL,
        [IMEI] nvarchar(100) NULL,
        [PartyName] nvarchar(150) NOT NULL,
        [Deduction] decimal(18,2) NULL,
        [Profit] decimal(18,2) NULL,
        [Date] datetime2 NULL,
        [POS] nvarchar(max) NULL,
        [Model] nvarchar(200) NULL,
        [SwipePerson] nvarchar(150) NULL,
        [Merchant] nvarchar(200) NULL,
        [Remarks] nvarchar(max) NULL,
        [limitUsed] bit NOT NULL,
        [AdditionalInfo] nvarchar(max) NULL,
        [BankName] nvarchar(50) NOT NULL,
        [IsSettled] bit NOT NULL,
        [SettledAt] datetime2 NULL,
        [Createdat] datetime2 NOT NULL,
        [Updatedat] datetime2 NOT NULL,
        CONSTRAINT [PK_CardInfo] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[CashLedger] (
        [Id] bigint NOT NULL IDENTITY,
        [TransactionDate] datetime2 NOT NULL,
        [TransactionType] nvarchar(10) NOT NULL,
        [Name] nvarchar(200) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Remarks] nvarchar(500) NULL,
        [CreatedOn] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CashLedger] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CashLedger_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_CashLedger_TransactionType] CHECK ([TransactionType] IN ('OPENING', 'IN', 'OUT'))
    );

    CREATE TABLE [dbo].[PaymentInfo] (
        [Id] int NOT NULL IDENTITY,
        [Date] datetime2 NULL,
        [PartyName] nvarchar(150) NOT NULL,
        [SwipeCount] int NOT NULL,
        [SwipeAmount] decimal(18,2) NULL,
        [TotalAmount] decimal(18,2) NULL,
        [Profit] decimal(18,2) NULL,
        [TotalProfit] decimal(18,2) NULL,
        [PaidAmount] decimal(18,2) NOT NULL,
        [PendingAmount] decimal(18,2) NOT NULL,
        [PaymentStatus] nvarchar(20) NOT NULL,
        [LastPaidAt] datetime2 NULL,
        [LastPaidByUserId] int NULL,
        [LastPaidByUsername] nvarchar(100) NULL,
        [LimitUsed] bit NOT NULL,
        [CardName] nvarchar(100) NOT NULL,
        [Merchant] nvarchar(200) NULL,
        [SwipePerson] nvarchar(150) NULL,
        [Remarks] nvarchar(500) NULL,
        [Createdat] datetime2 NOT NULL,
        [Updatedat] datetime2 NOT NULL,
        CONSTRAINT [PK_PaymentInfo] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PaymentInfo_Amounts] CHECK ([PaidAmount] >= 0 AND [PendingAmount] >= 0),
        CONSTRAINT [CK_PaymentInfo_PaymentStatus] CHECK ([PaymentStatus] IN ('Pending', 'Paid'))
    );

    CREATE TABLE [dbo].[RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Token] nvarchar(max) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [ReplacedByToken] nvarchar(max) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[StockItems] (
        [Id] int NOT NULL IDENTITY,
        [NO] nvarchar(max) NOT NULL,
        [IN DATE] datetime2 NULL,
        [OUT DATE] datetime2 NULL,
        [IMEI] nvarchar(max) NULL,
        [COLOUR] nvarchar(max) NULL,
        [MODEL] nvarchar(max) NULL,
        [GST/MRP] decimal(18,2) NULL,
        [AMOUNT] decimal(18,2) NULL,
        [PARTY NAME] nvarchar(max) NULL,
        [PAYMENT] nvarchar(max) NULL,
        [IN/OUT] nvarchar(max) NULL,
        [STATUS] nvarchar(max) NULL,
        [SOLD TO] nvarchar(max) NULL,
        [REMARKS] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StockItems] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(100) NOT NULL,
        [Password] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [FullName] nvarchar(max) NULL,
        [NickName] nvarchar(max) NULL,
        [AdditionalInfo] nvarchar(max) NULL,
        [Role] nvarchar(max) NOT NULL,
        [Createdat] datetime2 NOT NULL,
        [Updatedat] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[BankTransactions] (
        [Id] int NOT NULL IDENTITY,
        [AccountId] int NOT NULL,
        [RelatedAccountId] int NULL,
        [TransactionDate] datetime2 NOT NULL,
        [TransactionType] nvarchar(20) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ReferenceNo] nvarchar(100) NULL,
        [Remarks] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BankTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BankTransaction_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_BankTransaction_TransactionType] CHECK ([TransactionType] IN ('OPENING', 'IN', 'OUT', 'TRANSFER_IN', 'TRANSFER_OUT')),
        CONSTRAINT [FK_BankTransactions_BankAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [dbo].[BankAccounts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BankTransactions_BankAccounts_RelatedAccountId] FOREIGN KEY ([RelatedAccountId]) REFERENCES [dbo].[BankAccounts] ([Id])
    );

    CREATE TABLE [dbo].[StockPaymentInfo] (
        [Id] int NOT NULL IDENTITY,
        [NO] nvarchar(max) NOT NULL,
        [ACCOUNT NAME] nvarchar(max) NULL,
        [ACCOUNT NO] nvarchar(max) NULL,
        [IFSC] nvarchar(max) NULL,
        [AMOUNT] decimal(18,2) NULL,
        [DATE] datetime2 NULL,
        [PARTY NAME] nvarchar(max) NULL,
        [BANK] nvarchar(max) NULL,
        [IN/OUT] nvarchar(max) NULL,
        [PaymentType] nvarchar(20) NOT NULL,
        [PaymentStatus] nvarchar(20) NOT NULL,
        [OutstandingAmount] decimal(18,2) NOT NULL,
        [CompletedAt] datetime2 NULL,
        [CompletedByUserId] int NULL,
        [CompletedByUsername] nvarchar(100) NULL,
        [REMARKS] nvarchar(max) NULL,
        [StockItemId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StockPaymentInfo] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockPaymentInfo_OutstandingAmount] CHECK ([OutstandingAmount] >= 0),
        CONSTRAINT [CK_StockPaymentInfo_PaymentStatus] CHECK ([PaymentStatus] IN ('Pending', 'Completed')),
        CONSTRAINT [CK_StockPaymentInfo_PaymentType] CHECK ([PaymentType] IN ('Payable', 'Receivable')),
        CONSTRAINT [FK_StockPaymentInfo_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[StockPaymentHistory] (
        [Id] int NOT NULL IDENTITY,
        [StockPaymentInfoId] int NOT NULL,
        [EventType] nvarchar(20) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransactionDate] datetime2 NOT NULL,
        [PaymentMethod] nvarchar(50) NULL,
        [ReferenceNo] nvarchar(100) NULL,
        [Remarks] nvarchar(500) NULL,
        [PerformedByUserId] int NOT NULL,
        [PerformedByUsername] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StockPaymentHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockPaymentHistory_EventType] CHECK ([EventType] IN ('Settlement', 'Reset')),
        CONSTRAINT [FK_StockPaymentHistory_StockPaymentInfo_StockPaymentInfoId] FOREIGN KEY ([StockPaymentInfoId]) REFERENCES [dbo].[StockPaymentInfo] ([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_BankAccounts_AccountName] ON [dbo].[BankAccounts] ([AccountName]);
    CREATE INDEX [IX_BankTransactions_AccountId] ON [dbo].[BankTransactions] ([AccountId]);
    CREATE INDEX [IX_BankTransactions_RelatedAccountId] ON [dbo].[BankTransactions] ([RelatedAccountId]);
    CREATE UNIQUE INDEX [IX_PaymentInfo_PartyName] ON [dbo].[PaymentInfo] ([PartyName]);
    CREATE INDEX [IX_StockPaymentHistory_StockPaymentInfoId_CreatedAt] ON [dbo].[StockPaymentHistory] ([StockPaymentInfoId], [CreatedAt]);
    CREATE UNIQUE INDEX [IX_StockPaymentInfo_StockItemId] ON [dbo].[StockPaymentInfo] ([StockItemId]);
    CREATE UNIQUE INDEX [IX_Users_Username] ON [dbo].[Users] ([Username]);

    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );

    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES
        (N'20251113030509_initialcreate', N'10.0.12'),
        (N'20251117132418_newcolumnincardinfo', N'10.0.12'),
        (N'20251117132725_datavaluechange', N'10.0.12'),
        (N'20251117132956_datavaluechange1', N'10.0.12'),
        (N'20260815124900_AddRefreshTokens', N'10.0.12'),
        (N'20260928120000_AddPartyPaymentSummaries', N'10.0.12'),
        (N'20261010141315_AddPartyPaymentSettlement', N'10.0.12'),
        (N'20261010160000_AddStockPaymentClassificationAndHistory', N'10.0.12');

    COMMIT TRANSACTION;

    SELECT DB_NAME() AS [DatabaseName],
           N'Created 10 application tables and EF migration baseline through 20261010160000. No users or business data were seeded.' AS [Result];
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
