using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ekomers.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261012100000_AddRawMaterialConsumptionPermissions")]
public sealed class AddRawMaterialConsumptionPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DECLARE @CategoryId int;
        SELECT TOP(1) @CategoryId=ID FROM AuthorizationCategory WHERE Ad=N'Üretim' ORDER BY CASE WHEN IsDelete=1 THEN 1 ELSE 0 END,ID;
        IF @CategoryId IS NULL
        BEGIN
            INSERT AuthorizationCategory(Ad,Aciklama,IsActive,IsDelete,CreateDate) VALUES(N'Üretim',N'Üretim modülü işlem yetkileri',1,0,GETDATE());
            SET @CategoryId=CONVERT(int,SCOPE_IDENTITY());
        END
        ELSE UPDATE AuthorizationCategory SET IsActive=1,IsDelete=0 WHERE ID=@CategoryId;
        MERGE Yetkilendirme AS target USING(VALUES
            (N'Hammadde Sarf Yönetimi',N'Üretim emrine bağlanmadan hammadde sarf taslağı oluşturma, iptal etme ve depo stoklarından düşme',N'HammaddeSarfYonet'),
            (N'Hammadde Sarf Geri Alma',N'İşlenmiş hammadde sarfını gerekçeli ters kayıtla geri alma',N'HammaddeSarfGeriAl')
        ) AS source(Ad,Aciklama,ClaimName)
        ON target.ClaimType=N'Authorize' AND target.ClaimName=source.ClaimName
        WHEN MATCHED THEN UPDATE SET Ad=source.Ad,Aciklama=source.Aciklama,KategoriID=@CategoryId,PolicyName=source.ClaimName,IsActive=1,IsDelete=0,UpdateDate=GETDATE()
        WHEN NOT MATCHED THEN INSERT(Ad,Aciklama,KategoriID,PolicyName,ClaimType,ClaimName,IsActive,IsDelete,CreateDate)
            VALUES(source.Ad,source.Aciklama,@CategoryId,source.ClaimName,N'Authorize',source.ClaimName,1,0,GETDATE());
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        UPDATE Yetkilendirme SET IsActive=0,IsDelete=1,UpdateDate=GETDATE()
        WHERE ClaimType=N'Authorize' AND ClaimName IN(N'HammaddeSarfYonet',N'HammaddeSarfGeriAl');
        """);
}
