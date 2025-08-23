# Script PowerShell pour tester le déploiement localement
# Simule l'environnement de production Render.com

param(
    [switch]$Build,
    [switch]$Run,
    [switch]$Test,
    [switch]$All
)

# Couleurs pour l'affichage
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Blue = "Blue"

function Write-ColorOutput($ForegroundColor, $Message) {
    Write-Host $Message -ForegroundColor $ForegroundColor
}

function Test-Prerequisites {
    Write-ColorOutput $Blue "🔍 Vérification des prérequis..."
    
    # Vérifier .NET 8
    try {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -like "8.*") {
            Write-ColorOutput $Green "✅ .NET 8 SDK trouvé : $dotnetVersion"
        } else {
            Write-ColorOutput $Red "❌ .NET 8 SDK requis. Version trouvée : $dotnetVersion"
            exit 1
        }
    } catch {
        Write-ColorOutput $Red "❌ .NET SDK non trouvé. Installez .NET 8 SDK."
        exit 1
    }
    
    # Vérifier PostgreSQL (optionnel)
    try {
        $pgVersion = psql --version 2>$null
        if ($pgVersion) {
            Write-ColorOutput $Green "✅ PostgreSQL trouvé : $pgVersion"
        } else {
            Write-ColorOutput $Yellow "⚠️  PostgreSQL non trouvé. Utilisez LocalDB ou une instance distante."
        }
    } catch {
        Write-ColorOutput $Yellow "⚠️  PostgreSQL non détecté."
    }
}

function Build-Application {
    Write-ColorOutput $Blue "🔨 Construction de l'application..."
    
    # Nettoyer les builds précédents
    if (Test-Path "publish") {
        Remove-Item -Recurse -Force "publish"
    }
    
    # Restaurer les dépendances
    Write-ColorOutput $Blue "📦 Restauration des packages NuGet..."
    dotnet restore RotaryClubManager.sln
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Red "❌ Échec de la restauration des packages"
        exit 1
    }
    
    # Build
    Write-ColorOutput $Blue "🏗️  Compilation..."
    dotnet build RotaryClubManager.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Red "❌ Échec de la compilation"
        exit 1
    }
    
    # Publish
    Write-ColorOutput $Blue "📦 Publication..."
    dotnet publish RotaryClubManager.API/RotaryClubManager.API.csproj `
        --configuration Release `
        --output ./publish `
        --no-build `
        --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Red "❌ Échec de la publication"
        exit 1
    }
    
    Write-ColorOutput $Green "✅ Application construite avec succès"
}

function Set-EnvironmentVariables {
    Write-ColorOutput $Blue "🔧 Configuration des variables d'environnement..."
    
    # Variables d'environnement pour le test local
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://localhost:5000"
    
    # Base de données (LocalDB par défaut)
    $env:ConnectionStrings__DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=RotaryClubManagerDb;Trusted_Connection=true;"
    
    # JWT (clé de test - NE PAS UTILISER EN PRODUCTION)
    $env:JwtSettings__Secret = "test-jwt-secret-key-for-local-development-only-32-chars"
    $env:JwtSettings__Issuer = "RotaryClubManager"
    $env:JwtSettings__Audience = "RotaryClubManagerClient"
    $env:JwtSettings__AccessTokenExpiration = "60"
    $env:JwtSettings__RefreshTokenExpiration = "1440"
    
    # Email (configuration de test)
    $env:Email__SmtpHost = "smtp.gmail.com"
    $env:Email__SmtpPort = "587"
    $env:Email__SmtpUser = "test@example.com"
    $env:Email__SmtpPassword = "test-password"
    $env:Email__FromEmail = "test@example.com"
    $env:Email__FromName = "Rotary Club Manager Test"
    $env:Email__EnableSsl = "true"
    
    # Meta WhatsApp (valeurs de test)
    $env:Meta__AppId = "test-app-id"
    $env:Meta__PhoneNumberId = "test-phone-id"
    $env:Meta__AccessToken = "test-access-token"
    $env:Meta__WebhookVerifyToken = "test-webhook-token"
    
    # Logging
    $env:Logging__LogLevel__Default = "Information"
    $env:Logging__LogLevel__Microsoft_AspNetCore = "Warning"
    
    Write-ColorOutput $Green "✅ Variables d'environnement configurées"
}

function Test-Application {
    Write-ColorOutput $Blue "🧪 Exécution des tests..."
    
    # Tests unitaires
    dotnet test --configuration Release --no-build --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Red "❌ Échec des tests"
        exit 1
    }
    
    Write-ColorOutput $Green "✅ Tous les tests passent"
}

function Test-HealthChecks {
    Write-ColorOutput $Blue "🏥 Test des health checks..."
    
    # Attendre que l'application démarre
    Start-Sleep -Seconds 5
    
    try {
        # Test health check basique
        $response = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get -TimeoutSec 10
        if ($response.status -eq "healthy") {
            Write-ColorOutput $Green "✅ Health check basique : OK"
        } else {
            Write-ColorOutput $Red "❌ Health check basique : ÉCHEC"
        }
        
        # Test health check détaillé
        $detailedResponse = Invoke-RestMethod -Uri "http://localhost:5000/health/detailed" -Method Get -TimeoutSec 10
        Write-ColorOutput $Green "✅ Health check détaillé : OK"
        
        # Test readiness
        $readyResponse = Invoke-RestMethod -Uri "http://localhost:5000/health/ready" -Method Get -TimeoutSec 10
        Write-ColorOutput $Green "✅ Readiness check : OK"
        
    } catch {
        Write-ColorOutput $Red "❌ Health checks échoués : $($_.Exception.Message)"
    }
}

function Run-Application {
    Write-ColorOutput $Blue "🚀 Démarrage de l'application..."
    
    Set-EnvironmentVariables
    
    # Démarrer l'application
    Write-ColorOutput $Green "🌐 Application disponible sur : http://localhost:5000"
    Write-ColorOutput $Green "📖 Swagger UI : http://localhost:5000/swagger"
    Write-ColorOutput $Green "🏥 Health check : http://localhost:5000/health"
    Write-ColorOutput $Yellow "Appuyez sur Ctrl+C pour arrêter"
    
    # Démarrer en arrière-plan pour tester les health checks
    $job = Start-Job -ScriptBlock {
        param($publishPath)
        Set-Location $publishPath
        dotnet RotaryClubManager.API.dll
    } -ArgumentList (Get-Location).Path + "\publish"
    
    # Tester les health checks
    Test-HealthChecks
    
    # Arrêter le job et démarrer normalement
    Stop-Job $job
    Remove-Job $job
    
    # Démarrage normal
    Set-Location "publish"
    dotnet RotaryClubManager.API.dll
}

# Script principal
Write-ColorOutput $Blue "🎯 Script de déploiement local - RotaryClubManager.API"
Write-ColorOutput $Blue "=================================================="

if ($All -or (-not $Build -and -not $Run -and -not $Test)) {
    $Build = $true
    $Test = $true
    $Run = $true
}

Test-Prerequisites

if ($Build) {
    Build-Application
}

if ($Test) {
    Test-Application
}

if ($Run) {
    Run-Application
}

Write-ColorOutput $Green "🎉 Script terminé avec succès !"
