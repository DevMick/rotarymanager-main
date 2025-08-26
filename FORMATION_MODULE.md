# Module de Formation IA - Rotary Club Manager

## Vue d'ensemble

Le module de formation IA est une extension du Rotary Club Manager qui permet aux administrateurs d'uploader des documents de formation (PDFs), de les traiter via embeddings/chunking, et aux membres du club d'apprendre le contenu via des QCM adaptatifs gamifiés avec suivi des progrès.

## Architecture

### Entités créées

1. **DocumentFormation** - Documents PDF uploadés par les administrateurs
2. **ChunkDocument** - Chunks de texte extraits des PDFs avec embeddings
3. **SessionFormation** - Sessions de formation des membres
4. **QuestionFormation** - Questions générées automatiquement
5. **ReponseUtilisateur** - Réponses des utilisateurs aux questions
6. **BadgeFormation** - Système de gamification avec badges

### Endpoints API

#### Gestion des Documents de Formation

- `POST /api/formation/clubs/{clubId}/documents` - Upload d'un document PDF
- `GET /api/formation/clubs/{clubId}/documents/{documentId}` - Récupérer un document
- `GET /api/formation/clubs/{clubId}/documents` - Liste des documents d'un club
- `PUT /api/formation/clubs/{clubId}/documents/{documentId}` - Modifier un document
- `DELETE /api/formation/clubs/{clubId}/documents/{documentId}` - Supprimer un document

#### Gestion des Sessions de Formation

- `POST /api/formation/sessions` - Démarrer une nouvelle session
- `GET /api/formation/sessions/{sessionId}` - Récupérer une session
- `GET /api/formation/sessions` - Sessions de l'utilisateur connecté
- `GET /api/formation/clubs/{clubId}/sessions` - Sessions d'un club (Admin/President)

#### Gestion des Questions et Réponses

- `GET /api/formation/sessions/{sessionId}/questions` - Questions d'une session
- `POST /api/formation/sessions/{sessionId}/responses` - Soumettre une réponse

#### Gestion des Badges et Progression

- `GET /api/formation/badges` - Badges de l'utilisateur
- `GET /api/formation/progression` - Progression de l'utilisateur
- `GET /api/formation/clubs/{clubId}/progression` - Progression d'un club (Admin/President)

#### Recherche Sémantique

- `GET /api/formation/clubs/{clubId}/search?query={query}` - Recherche dans les documents

## Utilisation

### 1. Upload d'un Document de Formation

```bash
curl -X POST "https://api.rotaryclub.test/api/formation/clubs/{clubId}/documents" \
  -H "Authorization: Bearer {JWT_TOKEN}" \
  -F "file=@manuel-rotary.pdf" \
  -F "titre=Manuel Rotary 2024" \
  -F "description=Guide complet des procédures Rotary" \
  -F "type=1"
```

### 2. Démarrer une Session de Formation

```bash
curl -X POST "https://api.rotaryclub.test/api/formation/sessions" \
  -H "Authorization: Bearer {JWT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "documentFormationId": "uuid",
    "scoreObjectif": 80
  }'
```

### 3. Récupérer les Questions d'une Session

```bash
curl -X GET "https://api.rotaryclub.test/api/formation/sessions/{sessionId}/questions" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

### 4. Soumettre une Réponse

```bash
curl -X POST "https://api.rotaryclub.test/api/formation/sessions/{sessionId}/responses" \
  -H "Authorization: Bearer {JWT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "questionFormationId": "uuid",
    "reponseUtilisateur": "A",
    "tempsReponseMs": 5000
  }'
```

### 5. Consulter sa Progression

```bash
curl -X GET "https://api.rotaryclub.test/api/formation/progression" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

## Configuration

### Variables d'environnement requises

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

### Extensions PostgreSQL requises

```sql
-- Installation de pgvector pour les embeddings
CREATE EXTENSION IF NOT EXISTS vector;

-- Index vectoriel pour la recherche sémantique
CREATE INDEX idx_chunk_embedding ON chunk_document USING ivfflat (embedding vector_cosine_ops);
```

## Permissions

- **Admin/President** : Upload, modification, suppression de documents
- **Tous les membres** : Participation aux formations, consultation de leur progression
- **Admin/President** : Consultation des statistiques du club

## Fonctionnalités à venir

1. **Intégration OpenAI** - Génération automatique de questions
2. **Système de badges avancé** - Badges selon les fonctions Rotary
3. **Notifications** - Rappels de formation via WhatsApp/Email
4. **Analytics avancés** - Rapports détaillés sur l'efficacité des formations
5. **Formations obligatoires** - Selon les fonctions des membres

## Migration de base de données

```bash
# Créer la migration
dotnet ef migrations add AddFormationModule

# Appliquer la migration
dotnet ef database update
```

## Tests

```bash
# Tests unitaires
dotnet test --filter "Category=Formation"

# Tests d'intégration
dotnet test --filter "Category=FormationIntegration"
```

## Support

Pour toute question ou problème avec le module de formation, consultez :
- La documentation API Swagger : `/swagger`
- Les logs d'application pour les erreurs détaillées
- Le support technique de l'équipe de développement
