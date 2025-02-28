# Define file paths
$privateKeyFile = "C:\github\SuperOffice\devnet-database-mirroring\build\privatekey.txt"
$webConfigFile = "C:\github\SuperOffice\devnet-database-mirroring\build\web.config"

# New content for privatekey.txt
$privateKeyContent = @"
<RSAKeyValue>
  <YOURPRIVATEKEY />
</RSAKeyValue>
"@

# Update privatekey.txt
Set-Content -Path $privateKeyFile -Value $privateKeyContent -Encoding UTF8

# Load and modify web.config
[xml]$webConfig = Get-Content -Path $webConfigFile

# Replace SoAppId value
$soAppIdSetting = $webConfig.configuration.appSettings.add | Where-Object { $_.key -eq "SoAppId" }
if ($soAppIdSetting) {
    $soAppIdSetting.value = "FIXED"
}

# Replace password in ConnectionBase
$connectionBaseSetting = $webConfig.configuration.appSettings.add | Where-Object { $_.key -eq "ConnectionBase" }
if ($connectionBaseSetting) {
    # Use regex to replace "Password=MYPASSWORD" with "Password={{YOURPASSWORD}}"
    $connectionBaseSetting.value = $connectionBaseSetting.value -replace "Password=.*?;", "Password={{YOURPASSWORD}};"
}

# Save the modified web.config
$webConfig.Save($webConfigFile)

# Commit and push changes to GitHub
git add .
git commit -m "Updated privatekey.txt and modified web.config (SoAppId and ConnectionBase password)"
git push origin main
