/* Normalize-ProductionUnits.sql uygulamasinin RunId degeri ile geri alma.
   Varsayilan sadece onizleme. Portal/Hangfire yazma islemlerini durdurun.
   Yedek alin; @RunId girin; kontrol sonrasi @Apply=1 yapin.
   Sonradan degisen/silinen referans varsa TAMAMI iptal edilir.
   Yeni kayitlari eski birimlere tasimaz; sadece bu calistirmanin degistirdigi
   referanslari ve birim ad/aktiflik/audit alanlarini geri getirir.
   Yedek tablolar silinmez. Miktar ve maliyet alanlarina dokunulmaz.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @RunId uniqueidentifier = NULL; -- AppliedRunId degerini buraya yazin.
DECLARE @Apply bit = 0;
IF @@TRANCOUNT<>0 THROW 51000,N'Acik bir transaction disinda calistirin.',1;
IF OBJECT_ID(N'dbo.PrdUnitMergeRun',N'U') IS NULL THROW 51000,N'Bu veritabaninda birim birlestirme yedegi yok.',1;
IF @RunId IS NULL
BEGIN
    SELECT * FROM dbo.PrdUnitMergeRun ORDER BY AppliedAt DESC;
    PRINT N'Geri alinacak RunId degerini girin ve betigin tamamini tekrar calistirin.';
    RETURN;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.PrdUnitMergeRun WHERE RunId=@RunId AND RestoredAt IS NULL)
    THROW 51000,N'RunId bulunamadi veya daha once geri alinmis.',1;
DROP TABLE IF EXISTS #RestoreConflicts;
CREATE TABLE #RestoreConflicts(Detail nvarchar(1000));
BEGIN TRY
    IF @Apply=1 BEGIN TRANSACTION;
    IF @Apply=1 AND NOT EXISTS(SELECT 1 FROM dbo.PrdUnitMergeRun WITH(UPDLOCK,HOLDLOCK) WHERE RunId=@RunId AND RestoredAt IS NULL)
        THROW 51000,N'Bu calistirma baska bir islem tarafindan geri alinmis.',1;
    INSERT #RestoreConflicts
      SELECT N'Birim #'+CONVERT(nvarchar(20),b.UnitId)+N' sonradan degismis veya silinmis.'
      FROM dbo.PrdUnitMergeUnitBackup b LEFT JOIN dbo.PrdUnit u WITH(UPDLOCK,HOLDLOCK) ON u.ID=b.UnitId
      JOIN dbo.PrdUnitMergeRun r ON r.RunId=b.RunId
      WHERE b.RunId=@RunId AND (u.ID IS NULL OR ISNULL(CONVERT(int,u.IsDelete),-1)<>ISNULL(CONVERT(int,b.OldIsDelete),-1)
        OR u.Name COLLATE Latin1_General_100_BIN2<>b.NewName COLLATE Latin1_General_100_BIN2
        OR ISNULL(CONVERT(int,u.IsActive),-1)<>ISNULL(CONVERT(int,b.NewIsActive),-1)
        OR u.UpdateDate IS NULL OR u.UpdateDate<>r.AppliedAt OR ISNULL(u.UpdateUserID,N'')<>r.Actor);
    DECLARE @Schema sysname,@Table sysname,@Column sysname,@Sql nvarchar(max);
    DECLARE restore_check_cursor CURSOR LOCAL FAST_FORWARD FOR
      SELECT DISTINCT SchemaName,TableName,ColumnName FROM dbo.PrdUnitMergeReferenceBackup WHERE RunId=@RunId;
    OPEN restore_check_cursor; FETCH NEXT FROM restore_check_cursor INTO @Schema,@Table,@Column;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF OBJECT_ID(QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table),N'U') IS NULL
            INSERT #RestoreConflicts VALUES(N'Tablo bulunamadi: '+QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table));
        ELSE
        BEGIN
            SET @Sql=N'INSERT #RestoreConflicts SELECT @s+N''.''+@t+N''.''+@c+N'' #''+CONVERT(nvarchar(20),b.RowId)+N'' sonradan degismis/silinmis.'' FROM dbo.PrdUnitMergeReferenceBackup b LEFT JOIN '+QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table)+
              N' r WITH(UPDLOCK,HOLDLOCK) ON r.ID=b.RowId WHERE b.RunId=@run AND b.SchemaName=@s AND b.TableName=@t AND b.ColumnName=@c AND (r.ID IS NULL OR r.'+QUOTENAME(@Column)+N' IS NULL OR r.'+QUOTENAME(@Column)+N'<>b.NewUnitId);';
            EXEC sys.sp_executesql @Sql,N'@run uniqueidentifier,@s sysname,@t sysname,@c sysname',@RunId,@Schema,@Table,@Column;
        END
        FETCH NEXT FROM restore_check_cursor INTO @Schema,@Table,@Column;
    END
    CLOSE restore_check_cursor; DEALLOCATE restore_check_cursor;
    SELECT * FROM dbo.PrdUnitMergeRun WHERE RunId=@RunId;
    SELECT SchemaName,TableName,ColumnName,COUNT_BIG(*) ReferencesToRestore FROM dbo.PrdUnitMergeReferenceBackup WHERE RunId=@RunId GROUP BY SchemaName,TableName,ColumnName;
    SELECT * FROM #RestoreConflicts;
    IF @Apply=0 BEGIN PRINT N'ONIZLEME: hicbir kalici veri degistirilmedi.'; RETURN; END
    IF EXISTS(SELECT 1 FROM #RestoreConflicts) THROW 51000,N'Sonradan degisen kayit var; hicbir geri alma kaydedilmedi.',1;
    DECLARE restore_cursor CURSOR LOCAL FAST_FORWARD FOR
      SELECT DISTINCT SchemaName,TableName,ColumnName FROM dbo.PrdUnitMergeReferenceBackup WHERE RunId=@RunId;
    OPEN restore_cursor; FETCH NEXT FROM restore_cursor INTO @Schema,@Table,@Column;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @Sql=N'UPDATE r SET '+QUOTENAME(@Column)+N'=b.OldUnitId FROM '+QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table)+
          N' r JOIN dbo.PrdUnitMergeReferenceBackup b ON b.RowId=r.ID WHERE b.RunId=@run AND b.SchemaName=@s AND b.TableName=@t AND b.ColumnName=@c;';
        EXEC sys.sp_executesql @Sql,N'@run uniqueidentifier,@s sysname,@t sysname,@c sysname',@RunId,@Schema,@Table,@Column;
        FETCH NEXT FROM restore_cursor INTO @Schema,@Table,@Column;
    END
    CLOSE restore_cursor; DEALLOCATE restore_cursor;
    UPDATE u SET Name=b.OldName,IsActive=b.OldIsActive,UpdateDate=b.OldUpdateDate,UpdateUserID=b.OldUpdateUserID
      FROM dbo.PrdUnit u JOIN dbo.PrdUnitMergeUnitBackup b ON b.UnitId=u.ID AND b.RunId=@RunId;
    UPDATE dbo.PrdUnitMergeRun SET RestoredAt=SYSDATETIME() WHERE RunId=@RunId;
    COMMIT;
    PRINT N'Bu calistirmanin birim referanslari geri alindi. Yedek tablolar saklandi.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
