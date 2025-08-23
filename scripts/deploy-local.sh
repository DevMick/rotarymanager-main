#!/bin/bash

# Script Bash pour tester le déploiement localement
# Simule l'environnement de production Render.com

set -e  # Arrêter en cas d'erreur

# Couleurs pour l'affichage
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Fonctions d'affichage
print_info() {
    echo -e "${BLUE}$1${NC}"
}

print_success() {
    echo -e "${GREEN}$1${NC}"
}

print_warning() {
    echo -e "${YELLOW}$1${NC}"
}

print_error() {
    echo -e "${RED}$1${NC}"
}

# Vérification des prérequis
check_prerequisites() {
    print_info "🔍 Vérification des prérequis..."
    
    # Vérifier .NET 8
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version)
        if [[ $DOTNET_VERSION == 8.* ]]; then
            print_success "✅ .NET 8 SDK trouvé : $DOTNET_VERSION"
        else
            print_error "❌ .NET 8 SDK requis. Version trouvée : $DOTNET_VERSION"
            exit 1
        fi
    else
        print_error "❌ .NET SDK non trouvé. Installez .NET 8 SDK."
        exit 1
    fi
    
    # Vérifier PostgreSQL (optionnel)
    if command -v psql &> /dev/null; then
        PG_VERSION=$(psql --version)
        print_success "✅ PostgreSQL trouvé : $PG_VERSION"
    else
        print_warning "⚠️  PostgreSQL non trouvé. Utilisez une instance distante ou SQLite."
    fi
}

# Construction de l'application
build_application() {
    print_info "🔨 Construction de l'application..."
    
    # Nettoyer les builds précédents
    if [ -d "publish" ]; then
        rm -rf publish
    fi
    
    # Restaurer les dépendances
    print_info "📦 Restauration des packages NuGet..."
    dotnet restore RotaryClubManager.sln
    
    # Build
    print_info "🏗️  Compilation..."
    dotnet build RotaryClubManager.sln --configuration Release --no-restore
    
    # Publish
    print_info "📦 Publication..."
    dotnet publish RotaryClubManager.API/RotaryClubManager.API.csproj \
        --configuration Release \
        --output ./publish \
        --no-build \
        --verbosity minimal
    
    print_success "✅ Application construite avec succès"
}

# Configuration des variables d'environnement
set_environment_variables() {
    print_info "🔧 Configuration des variables d'environnement..."
    
    # Variables d'environnement pour le test local
    export ASPNETCORE_ENVIRONMENT="Development"
    export ASPNETCORE_URLS="http://localhost:5000"
    
    # Base de données (SQLite par défaut pour Linux/Mac)
    export ConnectionStrings__DefaultConnection="Data Source=rotarymanager.db"
    
    # JWT (clé de test - NE PAS UTILISER EN PRODUCTION)
    export JwtSettings__Secret="test-jwt-secret-key-for-local-development-only-32-chars"
    export JwtSettings__Issuer="RotaryClubManager"
    export JwtSettings__Audience="RotaryClubManagerClient"
    export JwtSettings__AccessTokenExpiration="60"
    export JwtSettings__RefreshTokenExpiration="1440"
    
    # Email (configuration de test)
    export Email__SmtpHost="smtp.gmail.com"
    export Email__SmtpPort="587"
    export Email__SmtpUser="test@example.com"
    export Email__SmtpPassword="test-password"
    export Email__FromEmail="test@example.com"
    export Email__FromName="Rotary Club Manager Test"
    export Email__EnableSsl="true"
    
    # Meta WhatsApp (valeurs de test)
    export Meta__AppId="test-app-id"
    export Meta__PhoneNumberId="test-phone-id"
    export Meta__AccessToken="test-access-token"
    export Meta__WebhookVerifyToken="test-webhook-token"
    
    # Logging
    export Logging__LogLevel__Default="Information"
    export Logging__LogLevel__Microsoft_AspNetCore="Warning"
    
    print_success "✅ Variables d'environnement configurées"
}

# Tests de l'application
test_application() {
    print_info "🧪 Exécution des tests..."
    
    # Tests unitaires
    dotnet test --configuration Release --no-build --verbosity minimal
    
    print_success "✅ Tous les tests passent"
}

# Test des health checks
test_health_checks() {
    print_info "🏥 Test des health checks..."
    
    # Attendre que l'application démarre
    sleep 5
    
    # Test health check basique
    if curl -f -s http://localhost:5000/health > /dev/null; then
        print_success "✅ Health check basique : OK"
    else
        print_error "❌ Health check basique : ÉCHEC"
        return 1
    fi
    
    # Test health check détaillé
    if curl -f -s http://localhost:5000/health/detailed > /dev/null; then
        print_success "✅ Health check détaillé : OK"
    else
        print_warning "⚠️  Health check détaillé : ÉCHEC"
    fi
    
    # Test readiness
    if curl -f -s http://localhost:5000/health/ready > /dev/null; then
        print_success "✅ Readiness check : OK"
    else
        print_warning "⚠️  Readiness check : ÉCHEC"
    fi
}

# Démarrage de l'application
run_application() {
    print_info "🚀 Démarrage de l'application..."
    
    set_environment_variables
    
    print_success "🌐 Application disponible sur : http://localhost:5000"
    print_success "📖 Swagger UI : http://localhost:5000/swagger"
    print_success "🏥 Health check : http://localhost:5000/health"
    print_warning "Appuyez sur Ctrl+C pour arrêter"
    
    # Démarrer l'application en arrière-plan pour tester les health checks
    cd publish
    dotnet RotaryClubManager.API.dll &
    APP_PID=$!
    
    # Tester les health checks
    test_health_checks
    
    # Arrêter l'application de test
    kill $APP_PID 2>/dev/null || true
    wait $APP_PID 2>/dev/null || true
    
    # Démarrage normal
    print_info "🎯 Démarrage en mode interactif..."
    dotnet RotaryClubManager.API.dll
}

# Affichage de l'aide
show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --build     Construire l'application uniquement"
    echo "  --test      Exécuter les tests uniquement"
    echo "  --run       Démarrer l'application uniquement"
    echo "  --all       Tout faire (défaut)"
    echo "  --help      Afficher cette aide"
    echo ""
    echo "Exemples:"
    echo "  $0                # Tout faire"
    echo "  $0 --build        # Construire seulement"
    echo "  $0 --test         # Tester seulement"
    echo "  $0 --run          # Démarrer seulement"
}

# Traitement des arguments
BUILD=false
TEST=false
RUN=false
ALL=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --build)
            BUILD=true
            shift
            ;;
        --test)
            TEST=true
            shift
            ;;
        --run)
            RUN=true
            shift
            ;;
        --all)
            ALL=true
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            print_error "Option inconnue: $1"
            show_help
            exit 1
            ;;
    esac
done

# Si aucune option spécifiée, faire tout
if [ "$BUILD" = false ] && [ "$TEST" = false ] && [ "$RUN" = false ] && [ "$ALL" = false ]; then
    ALL=true
fi

if [ "$ALL" = true ]; then
    BUILD=true
    TEST=true
    RUN=true
fi

# Script principal
print_info "🎯 Script de déploiement local - RotaryClubManager.API"
print_info "=================================================="

check_prerequisites

if [ "$BUILD" = true ]; then
    build_application
fi

if [ "$TEST" = true ]; then
    test_application
fi

if [ "$RUN" = true ]; then
    run_application
fi

print_success "🎉 Script terminé avec succès !"
