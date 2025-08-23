# Rotary Club Manager API

Une API REST complète pour la gestion des clubs Rotary, développée avec .NET 8 et Entity Framework Core.

## 🚀 Fonctionnalités

- **Gestion des membres** : Inscription, profils, rôles et statuts
- **Gestion des réunions** : Planification, ordres du jour, présences
- **Système d'authentification** : JWT avec rôles et permissions
- **Gestion des clubs** : Informations, membres, événements
- **API documentée** : Swagger/OpenAPI intégré
- **Validation des données** : FluentValidation
- **Rate limiting** : Protection contre les abus
- **Architecture en couches** : Clean Architecture

## 🏗️ Architecture

Le projet suit une architecture en couches (Clean Architecture) :

```
├── RotaryClubManager.API/          # Couche de présentation (Controllers, DTOs)
├── RotaryClubManager.Application/  # Couche application (Services, Validators)
├── RotaryClubManager.Domain/       # Couche domaine (Entities, Identity)
└── RotaryClubManager.Infrastructure/ # Couche infrastructure (Data, Services)
```

## 🛠️ Technologies utilisées

- **.NET 8** - Framework principal
- **Entity Framework Core** - ORM
- **SQL Server** - Base de données
- **JWT Bearer** - Authentification
- **FluentValidation** - Validation des données
- **Swagger/OpenAPI** - Documentation API
- **ASP.NET Core Identity** - Gestion des utilisateurs

## 📋 Prérequis

- .NET 8 SDK
- SQL Server (LocalDB ou instance complète)
- Visual Studio 2022 ou VS Code

## 🚀 Installation et démarrage

1. **Cloner le repository**
   ```bash
   git clone https://github.com/DevMick/rotarymanager-main.git
   cd rotarymanager-main
   ```

2. **Restaurer les packages NuGet**
   ```bash
   dotnet restore
   ```

3. **Configurer l'application**
   - Copier `appsettings.example.json` vers `appsettings.json`
   - Modifier les valeurs de configuration dans `appsettings.json` :
     - Chaîne de connexion à la base de données
     - Clé secrète JWT (minimum 32 caractères)
     - Configuration email SMTP
     - Tokens API (Meta WhatsApp, Twilio, etc.)
   - Appliquer les migrations :
   ```bash
   dotnet ef database update --project RotaryClubManager.Infrastructure --startup-project RotaryClubManager.API
   ```

4. **Lancer l'application**
   ```bash
   dotnet run --project RotaryClubManager.API
   ```

5. **Accéder à l'API**
   - API : `https://localhost:7000` ou `http://localhost:5000`
   - Documentation Swagger : `https://localhost:7000/swagger`

## 📖 Documentation API

La documentation complète de l'API est disponible via Swagger UI une fois l'application lancée.

### Endpoints principaux

- **Authentication** : `/api/auth/*`
- **Membres** : `/api/membres/*`
- **Clubs** : `/api/clubs/*`
- **Réunions** : `/api/reunions/*`

## 🔧 Configuration

### Configuration

Copiez le fichier `appsettings.example.json` vers `appsettings.json` et configurez les valeurs suivantes :

#### Base de données
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RotaryClubManagerDb;Trusted_Connection=true;"
  }
}
```

#### JWT
```json
{
  "JwtSettings": {
    "Secret": "votre-clé-secrète-très-longue-et-sécurisée-minimum-32-caractères",
    "Issuer": "RotaryClubManager",
    "Audience": "RotaryClubManagerClient",
    "AccessTokenExpiration": 60,
    "RefreshTokenExpiration": 1440
  }
}
```

#### Email SMTP
```json
{
  "Email": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@example.com",
    "SmtpPassword": "your-password",
    "FromEmail": "your-email@example.com",
    "EnableSsl": true
  }
}
```

#### Services externes (optionnel)
- **Meta WhatsApp** : Configurez `Meta:AppId`, `Meta:AccessToken`, etc.
- **Twilio** : Configurez `Twilio:AccountSid`, `Twilio:AuthToken`

⚠️ **Important** : Ne jamais commiter le fichier `appsettings.json` avec de vraies valeurs de production.

## 🧪 Tests

```bash
# Exécuter tous les tests
dotnet test

# Exécuter les tests avec couverture
dotnet test --collect:"XPlat Code Coverage"
```

## 📝 Migrations

```bash
# Créer une nouvelle migration
dotnet ef migrations add NomDeLaMigration --project RotaryClubManager.Infrastructure --startup-project RotaryClubManager.API

# Appliquer les migrations
dotnet ef database update --project RotaryClubManager.Infrastructure --startup-project RotaryClubManager.API
```

## 🤝 Contribution

1. Fork le projet
2. Créer une branche feature (`git checkout -b feature/AmazingFeature`)
3. Commit les changements (`git commit -m 'Add some AmazingFeature'`)
4. Push vers la branche (`git push origin feature/AmazingFeature`)
5. Ouvrir une Pull Request

## 📄 Licence

Ce projet est sous licence MIT. Voir le fichier `LICENSE` pour plus de détails.

## 👥 Auteurs

- **DevMick** - *Développeur principal* - [DevMick](https://github.com/DevMick)

## 🆘 Support

Pour toute question ou problème, veuillez ouvrir une issue sur GitHub.
