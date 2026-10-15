/*
  PORTAL veritabaninda calistirin; LOGO veritabaninda CALISTIRMAYIN.
  Varsayilan @Apply=0: sadece onizleme; kalici tablo/veri degisikligi yapmaz.
  Yedek alin, portal/Hangfire yazma islemlerini durdurun, onizlemeyi inceleyin;
  ardindan @Apply=1 yaparak AYNI BETIGIN TAMAMINI tekrar calistirin.
  Sadece 1:1 esanlamli birimler birlesir. KG/GRAM, LITRE/ML, koli/adet DONUSMEZ.
  Miktar, fiyat, maliyet, lot ve belge kayitlari silinmez/degistirilmez.
  Eski UnitId referanslari tasinir; eski birimler silinmeden pasife alinir.
  Bilinmeyen ve kod/adi celisen birimler inceleme icin raporlanir, dokunulmaz.
  Her uygulama RunId ve eski degerleri kalici yedek tablolara kaydeder.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit = 0;
DECLARE @Actor nvarchar(100) = N'Admin-UnitNormalization';
DECLARE @RunId uniqueidentifier = NEWID();
DECLARE @Now datetime2 = SYSDATETIME();
IF @@TRANCOUNT <> 0 THROW 51000, N'Betigi acik bir transaction disinda calistirin.', 1;
IF OBJECT_ID(N'dbo.PrdUnit',N'U') IS NULL
    THROW 51000, N'Portal veritabani secili degil: dbo.PrdUnit bulunamadi.', 1;

DROP TABLE IF EXISTS #Aliases;
CREATE TABLE #Aliases(Token nvarchar(100) COLLATE Latin1_General_100_BIN2 PRIMARY KEY, Canonical nvarchar(50), DisplayName nvarchar(100));
INSERT #Aliases VALUES
 (N'AD',N'ADET',N'Adet'),(N'ADET',N'ADET',N'Adet'),(N'PCS',N'ADET',N'Adet'),(N'PIECE',N'ADET',N'Adet'),
 (N'G',N'GRAM',N'Gram'),(N'GR',N'GRAM',N'Gram'),(N'GRM',N'GRAM',N'Gram'),(N'GRAM',N'GRAM',N'Gram'),
 (N'KG',N'KG',N'Kilogram'),(N'KILO',N'KG',N'Kilogram'),(N'KILOGRAM',N'KG',N'Kilogram'),
 (N'L',N'LITRE',N'Litre'),(N'LT',N'LITRE',N'Litre'),(N'LTR',N'LITRE',N'Litre'),(N'LITRE',N'LITRE',N'Litre'),(N'LITER',N'LITRE',N'Litre'),
 (N'ML',N'ML',N'Mililitre'),(N'MILILITRE',N'ML',N'Mililitre'),(N'MILILITER',N'ML',N'Mililitre'),(N'MILLILITRE',N'ML',N'Mililitre'),(N'MILLILITER',N'ML',N'Mililitre'),
 (N'KOLI',N'KOLI',N'Koli'),(N'PK',N'PAKET',N'Paket'),(N'PKT',N'PAKET',N'Paket'),(N'PAKET',N'PAKET',N'Paket');

DROP TABLE IF EXISTS #Units;
DROP TABLE IF EXISTS #Targets;
DROP TABLE IF EXISTS #Map;
DROP TABLE IF EXISTS #References;
DROP TABLE IF EXISTS #Counts;
DROP TABLE IF EXISTS #Blockers;
CREATE TABLE #Blockers(Detail nvarchar(1000));
CREATE TABLE #References(SchemaName sysname,TableName sysname,ColumnName sysname,PRIMARY KEY(SchemaName,TableName,ColumnName));
CREATE TABLE #Counts(SchemaName sysname,TableName sysname,ColumnName sysname,AffectedRows bigint);

BEGIN TRY
    IF @Apply=1 BEGIN TRANSACTION;
    -- HOLDLOCK/UPDLOCK keep the selected map stable during an APPLY transaction.
    SELECT u.ID,u.Code,u.Name,u.IsActive,u.IsDelete,
           c.Canonical CodeCanonical,n.Canonical NameCanonical,
           CASE WHEN c.Canonical IS NOT NULL AND n.Canonical IS NOT NULL AND c.Canonical<>n.Canonical THEN NULL
                ELSE COALESCE(c.Canonical,n.Canonical) END Canonical,
           CASE WHEN c.Canonical IS NOT NULL AND n.Canonical IS NOT NULL AND c.Canonical<>n.Canonical THEN NULL
                ELSE COALESCE(c.DisplayName,n.DisplayName) END DisplayName
    INTO #Units
    FROM dbo.PrdUnit u WITH(UPDLOCK,HOLDLOCK)
    CROSS APPLY(SELECT
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(LTRIM(RTRIM(u.Code)) COLLATE Latin1_General_100_CI_AS),N'İ',N'I'),N' ',N''),N'.',N''),N'-',N''),N'_',N'') CodeToken,
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(LTRIM(RTRIM(u.Name)) COLLATE Latin1_General_100_CI_AS),N'İ',N'I'),N' ',N''),N'.',N''),N'-',N''),N'_',N'') NameToken) t
    LEFT JOIN #Aliases c ON c.Token=t.CodeToken COLLATE Latin1_General_100_BIN2
    LEFT JOIN #Aliases n ON n.Token=t.NameToken COLLATE Latin1_General_100_BIN2;

    SELECT ID,Canonical,DisplayName INTO #Targets FROM
    (SELECT *,ROW_NUMBER() OVER(PARTITION BY Canonical ORDER BY
        CASE WHEN UPPER(LTRIM(RTRIM(Code))) COLLATE Latin1_General_100_BIN2=Canonical COLLATE Latin1_General_100_BIN2 THEN 0 ELSE 1 END,ID) rn
     FROM #Units WHERE Canonical IS NOT NULL AND ISNULL(IsDelete,0)=0 AND ISNULL(IsActive,1)=1) ranked WHERE rn=1;
    SELECT u.ID OldId,t.ID NewId,t.Canonical,t.DisplayName INTO #Map
    FROM #Units u JOIN #Targets t ON t.Canonical=u.Canonical WHERE u.ID<>t.ID;
    CREATE UNIQUE CLUSTERED INDEX IX_Map ON #Map(OldId);

    -- Discover FK references plus convention-based UnitId columns (including historical rows).
    INSERT #References
    SELECT DISTINCT s.name,t.name,c.name
    FROM sys.foreign_key_columns f
    JOIN sys.tables t ON t.object_id=f.parent_object_id
    JOIN sys.schemas s ON s.schema_id=t.schema_id
    JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=f.parent_column_id
    WHERE f.referenced_object_id=OBJECT_ID(N'dbo.PrdUnit');
    INSERT #References
    SELECT s.name,t.name,c.name FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id
    WHERE s.name=N'dbo' AND LEFT(t.name,3) IN(N'Prd',N'Pur')
      AND c.name IN(N'UnitId',N'FromUnitId',N'ToUnitId')
      AND t.name NOT IN(N'PrdUnitMergeReferenceBackup',N'PrdUnitMergeUnitBackup')
      AND NOT EXISTS(SELECT 1 FROM #References r WHERE r.SchemaName=s.name AND r.TableName=t.name AND r.ColumnName=c.name);

    INSERT #Blockers SELECT N'Desteklenmeyen referans: '+QUOTENAME(r.SchemaName)+N'.'+QUOTENAME(r.TableName)+N'.'+QUOTENAME(r.ColumnName)
    FROM #References r
    WHERE r.SchemaName<>N'dbo' OR LEFT(r.TableName,3) NOT IN(N'Prd',N'Pur')
       OR NOT EXISTS(SELECT 1 FROM sys.columns c WHERE c.object_id=OBJECT_ID(QUOTENAME(r.SchemaName)+N'.'+QUOTENAME(r.TableName)) AND c.name=N'ID' AND c.system_type_id=56)
       OR EXISTS(SELECT 1 FROM sys.columns c WHERE c.object_id=OBJECT_ID(QUOTENAME(r.SchemaName)+N'.'+QUOTENAME(r.TableName)) AND c.name=r.ColumnName AND c.system_type_id<>56);

    DECLARE @Schema sysname,@Table sysname,@Column sysname,@Sql nvarchar(max);
    DECLARE reference_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT SchemaName,TableName,ColumnName FROM #References;
    OPEN reference_cursor; FETCH NEXT FROM reference_cursor INTO @Schema,@Table,@Column;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @Sql=N'INSERT #Counts SELECT @s,@t,@c,COUNT_BIG(*) FROM '+QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table)+N' r WITH(UPDLOCK,HOLDLOCK) JOIN #Map m ON r.'+QUOTENAME(@Column)+N'=m.OldId;';
        EXEC sys.sp_executesql @Sql,N'@s sysname,@t sysname,@c sysname',@Schema,@Table,@Column;
        FETCH NEXT FROM reference_cursor INTO @Schema,@Table,@Column;
    END
    CLOSE reference_cursor; DEALLOCATE reference_cursor;

    -- A non-identity conversion between purported synonyms indicates unsafe/ambiguous data.
    IF OBJECT_ID(N'dbo.PrdUnitConversion',N'U') IS NOT NULL
    BEGIN
        IF EXISTS(SELECT 1 FROM dbo.PrdUnitConversion c
          LEFT JOIN #Map f ON f.OldId=c.FromUnitId LEFT JOIN #Map t ON t.OldId=c.ToUnitId
          WHERE ISNULL(c.IsDelete,0)=0 AND ISNULL(c.IsActive,1)=1
            AND (f.OldId IS NOT NULL OR t.OldId IS NOT NULL)
            AND COALESCE(f.NewId,c.FromUnitId)=COALESCE(t.NewId,c.ToUnitId) AND c.Factor<>1)
            INSERT #Blockers VALUES(N'Esanlamli birimler arasinda 1 disinda donusum katsayisi var. Once PrdUnitConversion kayitlarini inceleyin.');
        IF EXISTS(SELECT 1 FROM dbo.PrdUnitConversion c
          LEFT JOIN #Map f ON f.OldId=c.FromUnitId LEFT JOIN #Map t ON t.OldId=c.ToUnitId
          WHERE ISNULL(c.IsDelete,0)=0 AND ISNULL(c.IsActive,1)=1
          GROUP BY c.MaterialId,COALESCE(f.NewId,c.FromUnitId),COALESCE(t.NewId,c.ToUnitId)
          HAVING MIN(c.Factor)<>MAX(c.Factor) AND MAX(CASE WHEN f.OldId IS NOT NULL OR t.OldId IS NOT NULL THEN 1 ELSE 0 END)=1)
            INSERT #Blockers VALUES(N'Birlesme sonrasi ayni donusum icin farkli katsayilar olusuyor; otomatik duzeltme durduruldu.');
    END

    SELECT DB_NAME() PortalDatabase,@Apply ApplyMode,@RunId RunId;
    SELECT m.OldId,u.Code OldCode,u.Name OldName,m.NewId,t.Code TargetCode,m.DisplayName TargetName,m.Canonical
      FROM #Map m JOIN #Units u ON u.ID=m.OldId JOIN #Units t ON t.ID=m.NewId ORDER BY m.Canonical,m.OldId;
    SELECT ID,Code,Name,CodeCanonical,NameCanonical,
      CASE WHEN CodeCanonical IS NOT NULL AND NameCanonical IS NOT NULL AND CodeCanonical<>NameCanonical THEN N'Kod/ad celiskisi'
           WHEN Canonical IS NULL THEN N'Bilinmeyen birim'
           ELSE N'Aktif hedef birim yok' END ReviewReason
      FROM #Units u WHERE Canonical IS NULL OR NOT EXISTS(SELECT 1 FROM #Targets t WHERE t.Canonical=u.Canonical);
    SELECT * FROM #Counts WHERE AffectedRows>0 ORDER BY SchemaName,TableName,ColumnName;
    SELECT * FROM #Blockers;
    -- Expected combined balance; no stock movement is created by this script.
    IF OBJECT_ID(N'dbo.PrdStockMovement',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PrdMaterial',N'U') IS NOT NULL
      SELECT s.MaterialId,p.Code MaterialCode,s.WarehouseId,COALESCE(m.NewId,s.UnitId) TargetUnitId,
        SUM(CASE WHEN s.Direction=1 THEN s.Quantity ELSE -s.Quantity END) ExpectedBalance,
        SUM(CASE WHEN s.Direction=1 THEN s.TotalCost ELSE -s.TotalCost END) UnchangedStockValue
      FROM dbo.PrdStockMovement s JOIN dbo.PrdMaterial p ON p.ID=s.MaterialId LEFT JOIN #Map m ON m.OldId=s.UnitId
      WHERE ISNULL(s.IsDelete,0)=0
      GROUP BY s.MaterialId,p.Code,s.WarehouseId,COALESCE(m.NewId,s.UnitId)
      HAVING MAX(CASE WHEN m.OldId IS NOT NULL THEN 1 ELSE 0 END)=1;

    IF @Apply=0 BEGIN PRINT N'ONIZLEME: kalici veri degistirilmedi. Yedek sonrasi @Apply=1 ile tekrar calistirin.'; RETURN; END
    IF EXISTS(SELECT 1 FROM #Blockers) THROW 51000,N'Engelleyici kontroller bulundu. Hicbir duzeltme kaydedilmedi.',1;
    IF NOT EXISTS(SELECT 1 FROM #Map) BEGIN ROLLBACK; PRINT N'Birlesecek esanlamli birim bulunmadi.'; RETURN; END

    IF OBJECT_ID(N'dbo.PrdUnitMergeRun',N'U') IS NULL
      CREATE TABLE dbo.PrdUnitMergeRun(RunId uniqueidentifier NOT NULL PRIMARY KEY,AppliedAt datetime2 NOT NULL,Actor nvarchar(100) NOT NULL,RestoredAt datetime2 NULL);
    IF OBJECT_ID(N'dbo.PrdUnitMergeReferenceBackup',N'U') IS NULL
      CREATE TABLE dbo.PrdUnitMergeReferenceBackup(RunId uniqueidentifier NOT NULL,SchemaName sysname NOT NULL,TableName sysname NOT NULL,ColumnName sysname NOT NULL,RowId int NOT NULL,OldUnitId int NOT NULL,NewUnitId int NOT NULL,
        PRIMARY KEY(RunId,SchemaName,TableName,ColumnName,RowId));
    IF OBJECT_ID(N'dbo.PrdUnitMergeUnitBackup',N'U') IS NULL
      CREATE TABLE dbo.PrdUnitMergeUnitBackup(RunId uniqueidentifier NOT NULL,UnitId int NOT NULL,OldName nvarchar(100) NOT NULL,OldIsActive bit NULL,OldIsDelete bit NULL,OldUpdateDate datetime2 NULL,OldUpdateUserID nvarchar(max) NULL,NewName nvarchar(100) NOT NULL,NewIsActive bit NULL,
        PRIMARY KEY(RunId,UnitId));
    INSERT dbo.PrdUnitMergeRun VALUES(@RunId,@Now,@Actor,NULL);
    INSERT dbo.PrdUnitMergeUnitBackup
      SELECT @RunId,u.ID,u.Name,u.IsActive,u.IsDelete,u.UpdateDate,u.UpdateUserID,COALESCE(t.DisplayName,u.Name),CASE WHEN m.OldId IS NOT NULL THEN CONVERT(bit,0) ELSE u.IsActive END
      FROM dbo.PrdUnit u LEFT JOIN #Targets t ON t.ID=u.ID LEFT JOIN #Map m ON m.OldId=u.ID
      WHERE t.ID IS NOT NULL OR m.OldId IS NOT NULL;

    DECLARE update_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT SchemaName,TableName,ColumnName FROM #References;
    OPEN update_cursor; FETCH NEXT FROM update_cursor INTO @Schema,@Table,@Column;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @Sql=N'UPDATE r SET '+QUOTENAME(@Column)+N'=m.NewId OUTPUT @run,@s,@t,@c,inserted.ID,deleted.'+QUOTENAME(@Column)+N',inserted.'+QUOTENAME(@Column)+
          N' INTO dbo.PrdUnitMergeReferenceBackup(RunId,SchemaName,TableName,ColumnName,RowId,OldUnitId,NewUnitId) FROM '+QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table)+N' r JOIN #Map m ON r.'+QUOTENAME(@Column)+N'=m.OldId;';
        EXEC sys.sp_executesql @Sql,N'@run uniqueidentifier,@s sysname,@t sysname,@c sysname',@RunId,@Schema,@Table,@Column;
        FETCH NEXT FROM update_cursor INTO @Schema,@Table,@Column;
    END
    CLOSE update_cursor; DEALLOCATE update_cursor;
    UPDATE u SET Name=b.NewName,IsActive=b.NewIsActive,UpdateDate=@Now,UpdateUserID=@Actor
      FROM dbo.PrdUnit u JOIN dbo.PrdUnitMergeUnitBackup b ON b.UnitId=u.ID AND b.RunId=@RunId;
    COMMIT;
    SELECT @RunId AppliedRunId,COUNT_BIG(*) RewrittenReferences FROM dbo.PrdUnitMergeReferenceBackup WHERE RunId=@RunId;
    PRINT N'Tamamlandi. RunId degerini ve yedek tablolarini saklayin. Miktar/maliyet degismedi; eski birimler pasif.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
