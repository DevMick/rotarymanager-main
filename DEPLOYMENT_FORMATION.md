# Guide de Déploiement - Module de Formation IA

## Prérequis

### 1. Extensions PostgreSQL
Le module de formation nécessite l'extension `pgvector` pour les embeddings :

```sql
-- Se connecter à la base de données PostgreSQL
psql -h localhost -U postgres -d rotaryclubmanager

-- Installer l'extension pgvector
CREATE EXTENSION IF NOT EXISTS vector;
```

### 2. Variables d'environnement
Ajouter les configurations suivantes dans `appsettings.json` ou les variables d'environnement :

```json
{
  "Formation": {
    "OpenAI": {
      "ApiKey": "sk-...",
      "EmbeddingModel": "text-embedding-3-small",
      "ChatModel": "gpt-4o-mini"
    },
    "Upload": {
      "MaxFileSizeMB": 50,
      "AllowedExtensions": [".pdf"],
      "StoragePath": "uploads/formation/"
    },
    "Quiz": {
      "DefaultTargetScore": 80,
      "MaxQuestionsPerSession": 20,
      "MinDifficultyLevel": 1,
      "MaxDifficultyLevel": 5
    }
  }
}
```

## Déploiement

### Étape 1: Migration de la base de données

```bash
# Créer la migration Entity Framework
dotnet ef migrations add AddFormationModule --project RotaryClubManager.Infrastructure --startup-project RotaryClubManager.API

# Appliquer la migration
dotnet ef database update --project RotaryClubManager.Infrastructure --startup-project RotaryClubManager.API
```

### Étape 2: Installation des dépendances

```bash
# Installer les packages NuGet nécessaires
dotnet add RotaryClubManager.API package OpenAI
dotnet add RotaryClubManager.API package iTextSharp
dotnet add RotaryClubManager.API package Hangfire.Core
dotnet add RotaryClubManager.API package Hangfire.SqlServer
```

### Étape 3: Configuration des services

Dans `Program.cs`, ajouter les services de formation :

```csharp
// Ajouter les services de formation
builder.Services.AddScoped<IFormationService, FormationService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IQuestionGeneratorService, QuestionGeneratorService>();
builder.Services.AddScoped<IFormationRepository, FormationRepository>();

// Configuration Hangfire pour les tâches async
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();
```

### Étape 4: Création du dossier d'upload

```bash
# Créer le dossier pour les fichiers de formation
mkdir -p uploads/formation
chmod 755 uploads/formation
```

### Étape 5: Configuration Nginx (si applicable)

```nginx
# Ajouter dans la configuration Nginx pour servir les fichiers statiques
location /uploads/formation/ {
    alias /path/to/your/app/uploads/formation/;
    expires 1y;
    add_header Cache-Control "public, immutable";
}
```

## Tests de déploiement

### 1. Test de l'extension pgvector

```sql
-- Vérifier que l'extension est installée
SELECT * FROM pg_extension WHERE extname = 'vector';

-- Tester la création d'un vecteur
SELECT '[1,2,3,4,5]'::vector;
```

### 2. Test des endpoints API

```bash
# Test de l'endpoint de santé
curl -X GET "https://api.rotaryclub.test/api/formation/health"

# Test d'authentification
curl -X POST "https://api.rotaryclub.test/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@rotaryclub.test","password":"Password123!"}'
```

### 3. Test d'upload de document

```bash
# Créer un fichier PDF de test
echo "Test content" > test.pdf

# Upload du document
curl -X POST "https://api.rotaryclub.test/api/formation/clubs/{clubId}/documents" \
  -H "Authorization: Bearer {JWT_TOKEN}" \
  -F "file=@test.pdf" \
  -F "titre=Test Document" \
  -F "type=1"
```

## Monitoring et logs

### 1. Logs d'application

```bash
# Surveiller les logs en temps réel
tail -f logs/formation.log

# Rechercher les erreurs
grep -i error logs/formation.log
```

### 2. Monitoring de la base de données

```sql
-- Vérifier l'utilisation de l'espace disque
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables 
WHERE schemaname = 'public' 
AND tablename LIKE '%formation%';

-- Vérifier les performances des requêtes
SELECT query, calls, total_time, mean_time
FROM pg_stat_statements 
WHERE query LIKE '%formation%'
ORDER BY total_time DESC;
```

### 3. Monitoring OpenAI

```bash
# Vérifier l'utilisation de l'API OpenAI
curl -H "Authorization: Bearer $OPENAI_API_KEY" \
  https://api.openai.com/v1/usage
```

## Sécurité

### 1. Validation des fichiers uploadés

- Seuls les fichiers PDF sont acceptés
- Taille maximale : 50MB
- Scan antivirus recommandé

### 2. Isolation des données

- Chaque club ne peut accéder qu'à ses propres documents
- Validation des permissions par rôle (Admin/President)

### 3. Protection des embeddings

- Les embeddings sont stockés de manière sécurisée
- Accès restreint aux données sensibles

## Sauvegarde et restauration

### 1. Sauvegarde des données de formation

```bash
# Sauvegarde complète
pg_dump -h localhost -U postgres -d rotaryclubmanager \
  --table=document_formation \
  --table=chunk_document \
  --table=session_formation \
  --table=question_formation \
  --table=reponse_utilisateur \
  --table=badge_formation \
  > formation_backup.sql

# Sauvegarde des fichiers uploadés
tar -czf formation_files_backup.tar.gz uploads/formation/
```

### 2. Restauration

```bash
# Restaurer les données
psql -h localhost -U postgres -d rotaryclubmanager < formation_backup.sql

# Restaurer les fichiers
tar -xzf formation_files_backup.tar.gz
```

## Dépannage

### Problèmes courants

1. **Erreur pgvector non installé**
   ```bash
   # Solution : Installer l'extension
   CREATE EXTENSION IF NOT EXISTS vector;
   ```

2. **Erreur de permissions sur le dossier upload**
   ```bash
   # Solution : Corriger les permissions
   chmod 755 uploads/formation
   chown www-data:www-data uploads/formation
   ```

3. **Erreur OpenAI API**
   ```bash
   # Vérifier la clé API
   echo $OPENAI_API_KEY
   # Tester la connexion
   curl -H "Authorization: Bearer $OPENAI_API_KEY" \
     https://api.openai.com/v1/models
   ```

### Support

Pour toute question ou problème :
- Consulter les logs d'application
- Vérifier la documentation API Swagger
- Contacter l'équipe de développement

## Mise à jour

### Procédure de mise à jour

1. Sauvegarder les données actuelles
2. Arrêter l'application
3. Appliquer les nouvelles migrations
4. Redémarrer l'application
5. Tester les fonctionnalités

```bash
# Script de mise à jour automatique
./update_formation_module.sh
```
