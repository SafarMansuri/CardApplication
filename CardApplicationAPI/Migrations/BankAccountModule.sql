-- Run this script against the target SQL Server database.
-- This creates the dynamic bank account and bank transaction tables.
-- It does not modify the existing CashLedger table.

IF OBJECT_ID(N'[dbo].[BankAccounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BankAccounts]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [AccountName] NVARCHAR(150) NOT NULL,
        [BankName] NVARCHAR(150) NOT NULL,
        [AccountNo] NVARCHAR(50) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_BankAccounts_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BankAccounts_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BankAccounts_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_BankAccounts] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_BankAccounts_AccountName] UNIQUE NONCLUSTERED ([AccountName] ASC),
        CONSTRAINT [CK_BankAccounts_AccountName] CHECK (LEN(LTRIM(RTRIM([AccountName]))) > 0),
        CONSTRAINT [CK_BankAccounts_BankName] CHECK (LEN(LTRIM(RTRIM([BankName]))) > 0)
    );
END
GO

IF OBJECT_ID(N'[dbo].[BankTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BankTransactions]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [AccountId] INT NOT NULL,
        [RelatedAccountId] INT NULL,
        [TransactionDate] DATETIME2 NOT NULL,
        [TransactionType] NVARCHAR(20) NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [ReferenceNo] NVARCHAR(100) NULL,
        [Remarks] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BankTransactions_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BankTransactions_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_BankTransactions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_BankTransactions_TransactionType] CHECK ([TransactionType] IN ('OPENING', 'IN', 'OUT', 'TRANSFER_IN', 'TRANSFER_OUT')),
        CONSTRAINT [CK_BankTransactions_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_BankTransactions_BankAccounts_Account] FOREIGN KEY ([AccountId]) REFERENCES [dbo].[BankAccounts]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BankTransactions_BankAccounts_RelatedAccount] FOREIGN KEY ([RelatedAccountId]) REFERENCES [dbo].[BankAccounts]([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BankTransactions_AccountId_TransactionDate' AND object_id = OBJECT_ID('[dbo].[BankTransactions]'))
BEGIN
    CREATE INDEX [IX_BankTransactions_AccountId_TransactionDate]
    ON [dbo].[BankTransactions] ([AccountId], [TransactionDate] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BankTransactions_RelatedAccountId' AND object_id = OBJECT_ID('[dbo].[BankTransactions]'))
BEGIN
    CREATE INDEX [IX_BankTransactions_RelatedAccountId]
    ON [dbo].[BankTransactions] ([RelatedAccountId]);
END
GO

-- Example seed data:
-- Adjust the account names and amounts to your requirement.
IF NOT EXISTS (SELECT 1 FROM [dbo].[BankAccounts] WHERE [AccountName] = 'Main Bank Account')
BEGIN
    INSERT INTO [dbo].[BankAccounts] ([AccountName], [BankName], [AccountNo], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES ('Main Bank Account', 'State Bank', '1000000001', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

DECLARE @MainAccountId INT = (SELECT TOP (1) [Id] FROM [dbo].[BankAccounts] WHERE [AccountName] = 'Main Bank Account');

IF @MainAccountId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[BankTransactions]
        WHERE [AccountId] = @MainAccountId AND [TransactionType] = 'OPENING')
    BEGIN
        INSERT INTO [dbo].[BankTransactions] ([AccountId], [TransactionDate], [TransactionType], [Amount], [ReferenceNo], [Remarks], [CreatedAt], [UpdatedAt])
        VALUES (@MainAccountId, SYSUTCDATETIME(), 'OPENING', 0.00, 'INITIAL', 'Opening balance', SYSUTCDATETIME(), SYSUTCDATETIME());
    END
END
GO
