# Get the directory of the script
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptPath
$projectRoot = Join-Path $repoRoot (Join-Path "src" "DatabaseMirroringWcfService") 

# Define relative file paths
$privateKeyFile = Join-Path $projectRoot "privatekey.txt"
$webConfigFile = Join-Path $projectRoot "web.config"


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
    $soAppIdSetting.value = "SANITIZED"
}

# Replace password in ConnectionBase
$connectionBaseSetting = $webConfig.configuration.appSettings.add | Where-Object { $_.key -eq "ConnectionBase" }
if ($connectionBaseSetting) {
    # Use regex to replace "Password=MYPASSWORD" with "Password={{YOURPASSWORD}}"
    $connectionBaseSetting.value = "Server=YOUR_SERVER_NAME;User ID=YOUR_DB_USER_NAME;Password=YOUR_DB_USER_PASSWORD"
}

# Save the modified web.config
$webConfig.Save($webConfigFile)

# Commit and push changes to GitHub
git add .
git commit -m "Updated privatekey.txt and modified web.config (SoAppId and ConnectionBase password)"
git push origin main
