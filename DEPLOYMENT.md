# Déploiement sur Render.com

Ce guide vous explique comment déployer l'API RotaryClubManager sur Render.com.

## 🚀 Options de déploiement

### Option 1 : Blueprint (Recommandée)
Utilise le fichier `render.yaml` pour un déploiement automatisé complet.

### Option 2 : Manuel
Utilise le fichier `.render-buildpacks.rc` comme guide pour une configuration manuelle.

## 📋 Prérequis

1. **Compte Render.com** (gratuit)
2. **Repository GitHub** connecté
3. **Tokens et clés API** pour les services externes

## 🔧 Option 1 : Déploiement Blueprint

### 1. Préparer le repository
```bash
# Assurez-vous que render.yaml est dans la racine du projet
git add render.yaml
git commit -m "Add Render deployment configuration"
git push origin main
```

### 2. Déployer via Render Dashboard
1. Allez sur [Render Dashboard](https://dashboard.render.com)
2. Cliquez **"New"** → **"Blueprint"**
3. Connectez votre repository GitHub
4. Sélectionnez le fichier `render.yaml`
5. Cliquez **"Apply"**

### 3. Configurer les secrets
Après le déploiement, ajoutez ces variables d'environnement secrètes :

```bash
# Email
Email__SmtpPassword=your-email-app-password

# Meta WhatsApp API
Meta__AppId=your-meta-app-id
Meta__PhoneNumberId=your-phone-number-id
Meta__WhatsAppBusinessAccountId=your-business-account-id
Meta__AccessToken=your-meta-access-token

# Twilio (optionnel)
Twilio__AccountSid=your-twilio-account-sid
Twilio__AuthToken=your-twilio-auth-token
```

## 🛠️ Option 2 : Déploiement Manuel

### 1. Créer le service Web
1. **Dashboard** → **"New"** → **"Web Service"**
2. **Repository** : `https://github.com/DevMick/rotarymanager-main`
3. **Name** : `rotarymanager-api`
4. **Environment** : `Docker`
5. **Plan** : `Free`

### 2. Configuration Build
```bash
# Build Command
dotnet restore && dotnet build --configuration Release && dotnet publish RotaryClubManager.API/RotaryClubManager.API.csproj --configuration Release --output ./publish

# Start Command
dotnet ./publish/RotaryClubManager.API.dll
```

### 3. Paramètres avancés
- **Health Check Path** : `/health`
- **Auto-Deploy** : `Yes`

### 4. Créer la base de données PostgreSQL
1. **Dashboard** → **"New"** → **"PostgreSQL"**
2. **Name** : `rotarymanager-db`
3. **Plan** : `Free`
4. Copiez la chaîne de connexion

### 5. Variables d'environnement
Ajoutez toutes les variables listées dans `.render-buildpacks.rc`

## 🔐 Configuration des secrets

### Email (Gmail)
1. Activez l'authentification à 2 facteurs
2. Générez un mot de passe d'application
3. Utilisez ce mot de passe pour `Email__SmtpPassword`

### Meta WhatsApp API
1. Créez un compte [Meta for Developers](https://developers.facebook.com)
2. Créez une application WhatsApp Business
3. Récupérez les tokens depuis le dashboard Meta

### JWT Secret
```bash
# Générez une clé sécurisée
openssl rand -base64 32
```

## 🌐 Configuration des domaines

### Domaine personnalisé (optionnel)
1. **Service Settings** → **"Custom Domains"**
2. Ajoutez votre domaine
3. Configurez les enregistrements DNS

### CORS
Mettez à jour les origines autorisées :
```bash
CORS__AllowedOrigins__0=https://your-frontend-domain.com
CORS__AllowedOrigins__1=https://your-app.com
```

## 📊 Monitoring et logs

### Health Checks
- **Basic** : `https://your-app.onrender.com/health`
- **Detailed** : `https://your-app.onrender.com/health/detailed`
- **Readiness** : `https://your-app.onrender.com/health/ready`
- **Liveness** : `https://your-app.onrender.com/health/live`

### Logs
1. **Dashboard** → Votre service → **"Logs"**
2. Surveillez les erreurs de démarrage
3. Vérifiez les connexions à la base de données

## 🔄 Migrations de base de données

### Option 1 : Automatique (recommandée)
Décommentez cette ligne dans `render.yaml` :
```yaml
# dotnet ./publish/RotaryClubManager.API.dll --migrate
```

### Option 2 : Manuel
```bash
# Via Render Shell
dotnet ef database update --project RotaryClubManager.Infrastructure --startup-project RotaryClubManager.API
```

## 🚨 Dépannage

### Build échoue
```bash
# Vérifiez les logs de build
# Assurez-vous que .NET 8 est disponible
# Vérifiez les chemins des projets
```

### Application ne démarre pas
```bash
# Vérifiez les variables d'environnement
# Testez la connexion à la base de données
# Vérifiez les logs d'application
```

### Health check échoue
```bash
# Vérifiez que /health retourne 200 OK
# Testez localement d'abord
# Vérifiez les dépendances (DB, etc.)
```

### Base de données inaccessible
```bash
# Vérifiez la chaîne de connexion
# Assurez-vous que PostgreSQL est en cours d'exécution
# Testez la connectivité réseau
```

## 📈 Optimisations

### Performance
- Utilisez la mise en cache Redis (plan payant)
- Optimisez les requêtes de base de données
- Configurez la compression

### Sécurité
- Utilisez HTTPS uniquement
- Configurez les en-têtes de sécurité
- Limitez les origines CORS

### Monitoring
- Configurez les alertes Render
- Utilisez des outils de monitoring externes
- Surveillez les métriques de performance

## 🆘 Support

- **Documentation Render** : https://render.com/docs
- **Communauté Render** : https://community.render.com
- **Issues GitHub** : https://github.com/DevMick/rotarymanager-main/issues

## 📝 Checklist de déploiement

- [ ] Repository GitHub configuré
- [ ] Fichiers de configuration ajoutés
- [ ] Service web créé sur Render
- [ ] Base de données PostgreSQL créée
- [ ] Variables d'environnement configurées
- [ ] Secrets ajoutés de manière sécurisée
- [ ] Health checks fonctionnels
- [ ] Migrations de base de données appliquées
- [ ] CORS configuré pour votre frontend
- [ ] Domaine personnalisé configuré (optionnel)
- [ ] Monitoring et alertes configurés
