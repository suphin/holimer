using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260907123000_AddMillilitreProductionUnit")]
    public partial class AddMillilitreProductionUnit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @MillilitreUnitId int;

                SELECT TOP (1) @MillilitreUnitId = [ID]
                FROM [PrdUnit]
                WHERE UPPER(REPLACE(REPLACE(LTRIM(RTRIM([Code])), N'İ', N'I'), N' ', N'')) IN
                      (N'ML', N'MILILITRE', N'MILILITER', N'MILLILITRE', N'MILLILITER')
                   OR UPPER(REPLACE(REPLACE(LTRIM(RTRIM([Name])), N'İ', N'I'), N' ', N'')) IN
                      (N'ML', N'MILILITRE', N'MILILITER', N'MILLILITRE', N'MILLILITER')
                ORDER BY CASE WHEN [Code] = N'ML' THEN 0 ELSE 1 END, [ID];

                IF @MillilitreUnitId IS NULL
                BEGIN
                    INSERT INTO [PrdUnit]
                        ([Code], [Name], [IsActive], [IsDelete], [CreateDate])
                    VALUES
                        (N'ML', N'Mililitre', 1, 0, GETDATE());
                END;
                ELSE
                BEGIN
                    UPDATE [PrdUnit]
                    SET [Name] = N'Mililitre',
                        [IsActive] = 1,
                        [IsDelete] = 0,
                        [UpdateDate] = GETDATE()
                    WHERE [ID] = @MillilitreUnitId;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Birim stok, satınalma veya üretim kayıtlarında kullanılmış olabilir;
            // geriye dönüşte iş verisi ve yabancı anahtar bütünlüğü korunur.
        }
    }
}
