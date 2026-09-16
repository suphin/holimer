using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260910120000_AddReferenceCostEditPermission")]
    public partial class AddReferenceCostEditPermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @CategoryId int;

                SELECT TOP (1) @CategoryId = [ID]
                FROM [AuthorizationCategory]
                WHERE [Ad] = N'Üretim'
                ORDER BY CASE WHEN [IsDelete] = 1 THEN 1 ELSE 0 END, [ID];

                IF @CategoryId IS NULL
                BEGIN
                    INSERT INTO [AuthorizationCategory]
                        ([Ad], [Aciklama], [IsActive], [IsDelete], [CreateDate])
                    VALUES
                        (N'Üretim', N'Üretim modülü işlem yetkileri', 1, 0, GETDATE());

                    SET @CategoryId = CAST(SCOPE_IDENTITY() AS int);
                END
                ELSE
                BEGIN
                    UPDATE [AuthorizationCategory]
                    SET [IsActive] = 1, [IsDelete] = 0
                    WHERE [ID] = @CategoryId;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM [Yetkilendirme]
                    WHERE [ClaimType] = N'Authorize'
                      AND [ClaimName] = N'ReferansMaliyetDuzenle'
                )
                BEGIN
                    UPDATE [Yetkilendirme]
                    SET [Ad] = N'Referans Maliyet Düzenleme',
                        [Aciklama] = N'Malzeme referans maliyetlerinin mevcut versiyonlarını düzenleme yetkisi',
                        [KategoriID] = @CategoryId,
                        [PolicyName] = N'ReferansMaliyetDuzenle',
                        [IsActive] = 1,
                        [IsDelete] = 0,
                        [UpdateDate] = GETDATE()
                    WHERE [ClaimType] = N'Authorize'
                      AND [ClaimName] = N'ReferansMaliyetDuzenle';
                END
                ELSE
                BEGIN
                    INSERT INTO [Yetkilendirme]
                        ([Ad], [Aciklama], [KategoriID], [PolicyName], [ClaimType], [ClaimName], [IsActive], [IsDelete], [CreateDate])
                    VALUES
                        (N'Referans Maliyet Düzenleme',
                         N'Malzeme referans maliyetlerinin mevcut versiyonlarını düzenleme yetkisi',
                         @CategoryId,
                         N'ReferansMaliyetDuzenle',
                         N'Authorize',
                         N'ReferansMaliyetDuzenle',
                         1,
                         0,
                         GETDATE());
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [Yetkilendirme]
                WHERE [ClaimType] = N'Authorize'
                  AND [ClaimName] = N'ReferansMaliyetDuzenle';
                """);
        }
    }
}
