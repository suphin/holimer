using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ekomers.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261021120000_AddPivotReports")]
public sealed class AddPivotReports : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("RptPivotReport", columns: table => new {
            ID = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Name = table.Column<string>(maxLength: 150, nullable: false),
            SchemaName = table.Column<string>(maxLength: 128, nullable: false),
            ViewName = table.Column<string>(maxLength: 128, nullable: false),
            SettingsJson = table.Column<string>(nullable: false),
            IsActive = table.Column<bool>(nullable: false),
            UpdatedAt = table.Column<DateTime>(nullable: false),
            UpdatedBy = table.Column<string>(maxLength: 256, nullable: false),
            Version = table.Column<byte[]>(rowVersion: true, nullable: false)
        }, constraints: table => table.PrimaryKey("PK_RptPivotReport", x => x.ID));
        migrationBuilder.Sql("""
            INSERT dbo.RptPivotReport(Name,SchemaName,ViewName,SettingsJson,IsActive,UpdatedAt,UpdatedBy)
            VALUES(N'Fatura Dökümü',N'dbo',N'fatura_dokumu_100',N'{"allowedFields":["Yıl","Ay","Fatura Türü","Fatura Tipi","Fatura Numarası","Fatura İptal Durumu","Tarihi","Cari Hesap Kodu","Cari Hesap Unvanı","Hizmet Kodu","Hizmet Açıklaması","Birim","İşlem Döviz Türü","Miktar","SatIr Matrahı","Doviz SatIr Matrahı","Kdv","Toplamı"],"layout":{"rows":["Fatura Türü"],"column":"Ay","measures":[{"field":"SatIr Matrahı","function":"sum"}],"filters":[]}}',0,SYSUTCDATETIME(),N'Migration');
            """);
        migrationBuilder.Sql("""
            DECLARE @CategoryId int;
            SELECT TOP(1) @CategoryId=ID FROM AuthorizationCategory WHERE Ad=N'Raporlar'
            ORDER BY CASE WHEN IsDelete=1 THEN 1 ELSE 0 END,ID;
            IF @CategoryId IS NULL
            BEGIN
                INSERT AuthorizationCategory(Ad,Aciklama,IsActive,IsDelete,CreateDate)
                VALUES(N'Raporlar',N'Rapor erişim yetkileri',1,0,GETDATE());
                SET @CategoryId=CONVERT(int,SCOPE_IDENTITY());
            END
            ELSE UPDATE AuthorizationCategory SET IsActive=1,IsDelete=0 WHERE ID=@CategoryId;
            MERGE Yetkilendirme AS target USING(VALUES
                (N'Pivot Raporları Görüntüleme',N'Aktif Logo pivot raporlarını görüntüleme ve filtreleme',N'PivotRaporlar'),
                (N'Pivot Raporları Yönetme',N'View, kullanılabilir alanlar ve varsayılan pivot düzenini yönetme',N'PivotRaporlarYonet')
            ) AS source(Ad,Aciklama,ClaimName)
            ON target.ClaimType=N'Rapor' AND target.ClaimName=source.ClaimName
            WHEN MATCHED THEN UPDATE SET Ad=source.Ad,Aciklama=source.Aciklama,KategoriID=@CategoryId,
                PolicyName=N'Rapor.'+source.ClaimName,IsActive=1,IsDelete=0,UpdateDate=GETDATE()
            WHEN NOT MATCHED THEN INSERT(Ad,Aciklama,KategoriID,PolicyName,ClaimType,ClaimName,IsActive,IsDelete,CreateDate)
                VALUES(source.Ad,source.Aciklama,@CategoryId,N'Rapor.'+source.ClaimName,N'Rapor',source.ClaimName,1,0,GETDATE());
            -- No access is granted to existing users/roles automatically.
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("RptPivotReport");
        migrationBuilder.Sql("UPDATE Yetkilendirme SET IsActive=0,IsDelete=1,UpdateDate=GETDATE() WHERE ClaimType=N'Rapor' AND ClaimName IN(N'PivotRaporlar',N'PivotRaporlarYonet');");
    }
}
