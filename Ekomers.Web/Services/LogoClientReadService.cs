using Dapper;
using Ekomers.Common.Services;
using Ekomers.Models.ViewModels.Api;
using Microsoft.Data.SqlClient;

namespace Ekomers.Web.Services;

/// <summary>
/// Logo firma cari kartlarını yalnızca SELECT sorgularıyla okur.
/// </summary>
public sealed class LogoClientReadService
{
    private readonly string _connectionString;

    public LogoClientReadService(IConfiguration configuration)
    {
        var encrypted = configuration.GetConnectionString("LogoConnection");
        if (string.IsNullOrWhiteSpace(encrypted))
            throw new InvalidOperationException("LogoConnection bağlantı bilgisi bulunamadı.");

        _connectionString = CryptoHelper.Decrypt(encrypted);
    }

    public async Task<ApiPagedResponse<LogoClientApiVM>?> GetClientsAsync(
        int firmNumber,
        string? search,
        bool includePassive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var firm = firmNumber.ToString("000");
        var tableName = $"LG_{firm}_CLCARD";
        var qualifiedTableName = $"dbo.{tableName}";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var tableExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT CASE WHEN OBJECT_ID(@TableName, N'U') IS NULL THEN 0 ELSE 1 END",
            new { TableName = qualifiedTableName },
            commandTimeout: 30,
            cancellationToken: cancellationToken));
        if (tableExists == 0)
            return null;

        var companyName = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            "SELECT TOP (1) CONVERT(nvarchar(250), ISNULL([NAME], N'')) FROM dbo.L_CAPIFIRM WITH (NOLOCK) WHERE NR = @FirmNumber",
            new { FirmNumber = firmNumber },
            commandTimeout: 30,
            cancellationToken: cancellationToken)) ?? string.Empty;

        var searchLike = string.IsNullOrWhiteSpace(search) ? null : $"%{EscapeLike(search.Trim())}%";
        var where = "WHERE (@IncludePassive = 1 OR ISNULL(C.ACTIVE, 0) = 0) " +
                    "AND (@SearchLike IS NULL OR C.CODE LIKE @SearchLike ESCAPE '\\' " +
                    "OR C.DEFINITION_ LIKE @SearchLike ESCAPE '\\' OR C.TAXNR LIKE @SearchLike ESCAPE '\\')";
        var parameters = new
        {
            FirmNumber = firmNumber,
            CompanyName = companyName,
            IncludePassive = includePassive,
            SearchLike = searchLike,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            $"SELECT COUNT(1) FROM dbo.[{tableName}] C WITH (NOLOCK) {where}",
            parameters,
            commandTimeout: 60,
            cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<LogoClientApiVM>(new CommandDefinition(
            $"""
            SELECT
                @FirmNumber AS FirmNumber,
                @CompanyName AS CompanyName,
                CONVERT(int, C.LOGICALREF) AS LogicalRef,
                CONVERT(nvarchar(100), ISNULL(C.CODE, N'')) AS Code,
                CONVERT(nvarchar(250), ISNULL(C.DEFINITION_, N'')) AS Name,
                CONVERT(int, ISNULL(C.CARDTYPE, 0)) AS CardType,
                CONVERT(nvarchar(50), ISNULL(C.TAXNR, N'')) AS TaxNumber,
                CONVERT(nvarchar(100), ISNULL(C.TAXOFFICE, N'')) AS TaxOffice,
                CONVERT(nvarchar(250), ISNULL(C.ADDR1, N'')) AS Address1,
                CONVERT(nvarchar(250), ISNULL(C.ADDR2, N'')) AS Address2,
                CONVERT(nvarchar(100), ISNULL(C.TOWN, N'')) AS District,
                CONVERT(nvarchar(100), ISNULL(C.CITY, N'')) AS City,
                CONVERT(nvarchar(50), ISNULL(C.TELNRS1, N'')) AS Phone1,
                CONVERT(nvarchar(50), ISNULL(C.TELNRS2, N'')) AS Phone2,
                CONVERT(nvarchar(250), ISNULL(C.EMAILADDR, N'')) AS Email,
                CONVERT(bit, CASE WHEN ISNULL(C.ACTIVE, 0) = 0 THEN 1 ELSE 0 END) AS IsActive
            FROM dbo.[{tableName}] C WITH (NOLOCK)
            {where}
            ORDER BY C.CODE, C.LOGICALREF
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """,
            parameters,
            commandTimeout: 60,
            cancellationToken: cancellationToken))).ToList();

        return new ApiPagedResponse<LogoClientApiVM>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            Data = rows
        };
    }

    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);
}
