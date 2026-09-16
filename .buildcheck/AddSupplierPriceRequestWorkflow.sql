BEGIN TRANSACTION;
CREATE TABLE [PurEmailTemplate] (
    [ID] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [SubjectTemplate] nvarchar(500) NOT NULL,
    [BodyTemplate] nvarchar(max) NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NULL,
    [IsDelete] bit NULL,
    [CreateDate] datetime2 NULL,
    [DeleteDate] datetime2 NULL,
    [CreateUserID] nvarchar(max) NULL,
    [DeleteUserID] nvarchar(max) NULL,
    [DosyaID] nvarchar(max) NULL,
    [UpdateDate] datetime2 NULL,
    [UpdateUserID] nvarchar(max) NULL,
    CONSTRAINT [PK_PurEmailTemplate] PRIMARY KEY ([ID])
);

CREATE TABLE [PurPriceRequestEmail] (
    [ID] int NOT NULL IDENTITY,
    [ReferenceNumber] nvarchar(50) NOT NULL,
    [SupplierId] int NOT NULL,
    [EmailTemplateId] int NULL,
    [RecipientEmail] nvarchar(250) NOT NULL,
    [SenderName] nvarchar(150) NOT NULL,
    [SenderEmail] nvarchar(250) NOT NULL,
    [SenderAccount] nvarchar(250) NOT NULL,
    [Subject] nvarchar(500) NOT NULL,
    [BodyHtml] nvarchar(max) NOT NULL,
    [ResponseDeadline] datetime2 NULL,
    [Status] int NOT NULL,
    [SentDate] datetime2 NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [IsActive] bit NULL,
    [IsDelete] bit NULL,
    [CreateDate] datetime2 NULL,
    [DeleteDate] datetime2 NULL,
    [CreateUserID] nvarchar(max) NULL,
    [DeleteUserID] nvarchar(max) NULL,
    [DosyaID] nvarchar(max) NULL,
    [UpdateDate] datetime2 NULL,
    [UpdateUserID] nvarchar(max) NULL,
    CONSTRAINT [PK_PurPriceRequestEmail] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_PurPriceRequestEmail_PurEmailTemplate_EmailTemplateId] FOREIGN KEY ([EmailTemplateId]) REFERENCES [PurEmailTemplate] ([ID]) ON DELETE SET NULL,
    CONSTRAINT [FK_PurPriceRequestEmail_PurSupplier_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [PurSupplier] ([ID]) ON DELETE NO ACTION
);

CREATE TABLE [PurPriceRequestEmailLine] (
    [ID] int NOT NULL IDENTITY,
    [PriceRequestEmailId] int NOT NULL,
    [PurchaseRequestLineId] int NOT NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [IsActive] bit NULL,
    [IsDelete] bit NULL,
    [CreateDate] datetime2 NULL,
    [DeleteDate] datetime2 NULL,
    [CreateUserID] nvarchar(max) NULL,
    [DeleteUserID] nvarchar(max) NULL,
    [DosyaID] nvarchar(max) NULL,
    [UpdateDate] datetime2 NULL,
    [UpdateUserID] nvarchar(max) NULL,
    CONSTRAINT [PK_PurPriceRequestEmailLine] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_PurPriceRequestEmailLine_PurPriceRequestEmail_PriceRequestEmailId] FOREIGN KEY ([PriceRequestEmailId]) REFERENCES [PurPriceRequestEmail] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_PurPriceRequestEmailLine_PurPurchaseRequestLine_PurchaseRequestLineId] FOREIGN KEY ([PurchaseRequestLineId]) REFERENCES [PurPurchaseRequestLine] ([ID]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ID', N'BodyTemplate', N'CreateDate', N'CreateUserID', N'IsActive', N'IsDefault', N'IsDelete', N'Name', N'SubjectTemplate') AND [object_id] = OBJECT_ID(N'[PurEmailTemplate]'))
    SET IDENTITY_INSERT [PurEmailTemplate] ON;
INSERT INTO [PurEmailTemplate] ([ID], [BodyTemplate], [CreateDate], [CreateUserID], [IsActive], [IsDefault], [IsDelete], [Name], [SubjectTemplate])
VALUES (1, N'<p>Sayın {TEDARIKCI_ADI},</p><p>Aşağıdaki ürünler için fiyat, teslim süresi ve ödeme koşullarınızı içeren teklifinizi rica ederiz.</p>{TALEP_TABLOSU}<p><strong>Teklif son tarihi:</strong> {SON_TARIH}</p><p>{EK_NOT}</p><p>İyi çalışmalar dileriz.</p><p><strong>{GONDEREN_AD_SOYAD}</strong><br />E-posta: {GONDEREN_EPOSTA}<br />Kullanıcı hesabı: {KULLANICI_HESABI}</p>', '2026-09-15T12:00:00.0000000', N'system', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), N'Standart Fiyat Teklifi İsteği', N'Fiyat teklif talebi - {TALEP_NUMARALARI}');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ID', N'BodyTemplate', N'CreateDate', N'CreateUserID', N'IsActive', N'IsDefault', N'IsDelete', N'Name', N'SubjectTemplate') AND [object_id] = OBJECT_ID(N'[PurEmailTemplate]'))
    SET IDENTITY_INSERT [PurEmailTemplate] OFF;

CREATE UNIQUE INDEX [IX_PurEmailTemplate_Name] ON [PurEmailTemplate] ([Name]) WHERE [Name] IS NOT NULL;

CREATE INDEX [IX_PurPriceRequestEmail_EmailTemplateId] ON [PurPriceRequestEmail] ([EmailTemplateId]);

CREATE UNIQUE INDEX [IX_PurPriceRequestEmail_ReferenceNumber] ON [PurPriceRequestEmail] ([ReferenceNumber]) WHERE [ReferenceNumber] IS NOT NULL;

CREATE INDEX [IX_PurPriceRequestEmail_SupplierId_SentDate] ON [PurPriceRequestEmail] ([SupplierId], [SentDate]);

CREATE UNIQUE INDEX [IX_PurPriceRequestEmailLine_PriceRequestEmailId_PurchaseRequestLineId] ON [PurPriceRequestEmailLine] ([PriceRequestEmailId], [PurchaseRequestLineId]) WHERE [PriceRequestEmailId] IS NOT NULL AND [PurchaseRequestLineId] IS NOT NULL;

CREATE INDEX [IX_PurPriceRequestEmailLine_PurchaseRequestLineId] ON [PurPriceRequestEmailLine] ([PurchaseRequestLineId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260915205631_AddSupplierPriceRequestWorkflow', N'10.0.10');

COMMIT;
GO

