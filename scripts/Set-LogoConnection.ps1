param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\Ekomers.Web\Ekomers.Web.csproj'),
    [string]$AppSettingsPath = (Join-Path $PSScriptRoot '..\Ekomers.Web\appsettings.json')
)

$ErrorActionPreference = 'Stop'

function ConvertFrom-EncryptedConnectionString {
    param([Parameter(Mandatory)][string]$CipherText)

    $aes = [Security.Cryptography.Aes]::Create()
    try {
        $aes.Key = [Text.Encoding]::UTF8.GetBytes('12345678901234567890123456789012')
        $aes.IV = [Text.Encoding]::UTF8.GetBytes('1234567890123456')
        $cipherBytes = [Convert]::FromBase64String($CipherText)
        $plainBytes = $aes.CreateDecryptor().TransformFinalBlock($cipherBytes, 0, $cipherBytes.Length)
        return [Text.Encoding]::UTF8.GetString($plainBytes)
    }
    finally {
        $aes.Dispose()
    }
}

function ConvertTo-EncryptedConnectionString {
    param([Parameter(Mandatory)][string]$PlainText)

    $aes = [Security.Cryptography.Aes]::Create()
    try {
        $aes.Key = [Text.Encoding]::UTF8.GetBytes('12345678901234567890123456789012')
        $aes.IV = [Text.Encoding]::UTF8.GetBytes('1234567890123456')
        $plainBytes = [Text.Encoding]::UTF8.GetBytes($PlainText)
        $cipherBytes = $aes.CreateEncryptor().TransformFinalBlock($plainBytes, 0, $plainBytes.Length)
        return [Convert]::ToBase64String($cipherBytes)
    }
    finally {
        $aes.Dispose()
    }
}

$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$resolvedAppSettingsPath = (Resolve-Path -LiteralPath $AppSettingsPath).Path
$settings = Get-Content -Raw -LiteralPath $resolvedAppSettingsPath | ConvertFrom-Json
$encryptedConnection = [string]$settings.ConnectionStrings.LogoConnection

if ([string]::IsNullOrWhiteSpace($encryptedConnection)) {
    throw 'ConnectionStrings:LogoConnection appsettings.json içinde bulunamadı.'
}

$plainConnection = ConvertFrom-EncryptedConnectionString -CipherText $encryptedConnection
$connectionBuilder = [System.Data.Common.DbConnectionStringBuilder]::new()
$connectionBuilder.ConnectionString = $plainConnection

$newUserId = Read-Host 'Yeni Logo kullanıcı adı'
if ([string]::IsNullOrWhiteSpace($newUserId)) {
    throw 'Kullanıcı adı boş bırakılamaz.'
}

$securePassword = Read-Host 'Yeni Logo parolası' -AsSecureString
$securePasswordConfirmation = Read-Host 'Yeni Logo parolası (tekrar)' -AsSecureString

$passwordPointer = [IntPtr]::Zero
$confirmationPointer = [IntPtr]::Zero
try {
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $confirmationPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePasswordConfirmation)
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    $passwordConfirmation = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($confirmationPointer)

    if ([string]::IsNullOrEmpty($password)) {
        throw 'Parola boş bırakılamaz.'
    }

    if ($password -cne $passwordConfirmation) {
        throw 'Girilen parolalar aynı değil.'
    }

    $connectionBuilder['User ID'] = $newUserId.Trim()
    $connectionBuilder['Password'] = $password
    $encryptedUpdatedConnection = ConvertTo-EncryptedConnectionString -PlainText $connectionBuilder.ConnectionString

    & dotnet user-secrets set 'ConnectionStrings:LogoConnection' $encryptedUpdatedConnection --project $resolvedProjectPath
    if ($LASTEXITCODE -ne 0) {
        throw 'LogoConnection User Secrets kaydı güncellenemedi.'
    }
}
finally {
    if ($passwordPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
    if ($confirmationPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($confirmationPointer)
    }
    $password = $null
    $passwordConfirmation = $null
}

Write-Host 'LogoConnection giriş bilgileri User Secrets içinde güncellendi. Uygulamayı yeniden başlatın.' -ForegroundColor Green
