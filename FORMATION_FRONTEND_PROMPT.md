# 🎯 PROMPT COMPLET - Module de Formation IA - Frontend Development

## 📋 **CONTEXTE**
Développer les pages frontend pour le module de formation IA du Rotary Club Manager. Ce module permet aux administrateurs d'uploader des documents PDF de formation et aux membres de suivre des formations interactives avec des QCMs adaptatifs.

## 🔐 **AUTHENTIFICATION**
Tous les endpoints nécessitent un token JWT dans le header :
```
Authorization: Bearer {JWT_TOKEN}
```

## 📚 **ENDPOINTS ET CURLS COMPLETS**

### 🗂️ **1. GESTION DES DOCUMENTS DE FORMATION**

#### **1.1 Upload Document (Admin/President uniquement)**
```bash
curl -X POST "https://api.rotaryclubmanager.com/api/formation/clubs/{clubId}/documents" \
  -H "Authorization: Bearer {JWT_TOKEN}" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@document.pdf" \
  -F "createDto={\"titre\":\"Manuel Rotary 2024\",\"description\":\"Guide complet des procédures Rotary\",\"type\":1}"
```

**Réponse 201 (Créé) :**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "titre": "Manuel Rotary 2024",
  "description": "Guide complet des procédures Rotary",
  "cheminFichier": "/uploads/formation/550e8400-e29b-41d4-a716-446655440000.pdf",
  "dateUpload": "2025-08-25T14:30:00Z",
  "uploadePar": "user123",
  "nomUploadeur": "Jean Dupont",
  "clubId": "525418ac-1bf3-4da2-9f4e-1fd13dd03119",
  "estActif": true,
  "type": 1,
  "nombreChunks": 0,
  "nombreSessions": 0
}
```

**Réponse 400 (Erreur) :**
```json
{
  "message": "Seuls les fichiers PDF sont acceptés"
}
```

#### **1.2 Récupérer un Document**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/clubs/{clubId}/documents/{documentId}" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "titre": "Manuel Rotary 2024",
  "description": "Guide complet des procédures Rotary",
  "cheminFichier": "/uploads/formation/550e8400-e29b-41d4-a716-446655440000.pdf",
  "dateUpload": "2025-08-25T14:30:00Z",
  "uploadePar": "user123",
  "nomUploadeur": "Jean Dupont",
  "clubId": "525418ac-1bf3-4da2-9f4e-1fd13dd03119",
  "estActif": true,
  "type": 1,
  "nombreChunks": 15,
  "nombreSessions": 8
}
```

#### **1.3 Lister tous les Documents d'un Club**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/clubs/{clubId}/documents" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "titre": "Manuel Rotary 2024",
    "description": "Guide complet des procédures Rotary",
    "cheminFichier": "/uploads/formation/550e8400-e29b-41d4-a716-446655440000.pdf",
    "dateUpload": "2025-08-25T14:30:00Z",
    "uploadePar": "user123",
    "nomUploadeur": "Jean Dupont",
    "clubId": "525418ac-1bf3-4da2-9f4e-1fd13dd03119",
    "estActif": true,
    "type": 1,
    "nombreChunks": 15,
    "nombreSessions": 8
  },
  {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "titre": "Procédures du Club",
    "description": "Procédures internes du club",
    "cheminFichier": "/uploads/formation/660e8400-e29b-41d4-a716-446655440001.pdf",
    "dateUpload": "2025-08-25T15:00:00Z",
    "uploadePar": "user456",
    "nomUploadeur": "Marie Martin",
    "clubId": "525418ac-1bf3-4da2-9f4e-1fd13dd03119",
    "estActif": true,
    "type": 2,
    "nombreChunks": 8,
    "nombreSessions": 3
  }
]
```

#### **1.4 Mettre à jour un Document (Admin/President uniquement)**
```bash
curl -X PUT "https://api.rotaryclubmanager.com/api/formation/clubs/{clubId}/documents/{documentId}" \
  -H "Authorization: Bearer {JWT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "titre": "Manuel Rotary 2024 - Mise à jour",
    "description": "Guide complet des procédures Rotary - Version révisée",
    "estActif": true,
    "type": 1
  }'
```

**Réponse 200 :**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "titre": "Manuel Rotary 2024 - Mise à jour",
  "description": "Guide complet des procédures Rotary - Version révisée",
  "cheminFichier": "/uploads/formation/550e8400-e29b-41d4-a716-446655440000.pdf",
  "dateUpload": "2025-08-25T14:30:00Z",
  "uploadePar": "user123",
  "nomUploadeur": "Jean Dupont",
  "clubId": "525418ac-1bf3-4da2-9f4e-1fd13dd03119",
  "estActif": true,
  "type": 1,
  "nombreChunks": 15,
  "nombreSessions": 8
}
```

#### **1.5 Supprimer un Document (Admin/President uniquement)**
```bash
curl -X DELETE "https://api.rotaryclubmanager.com/api/formation/clubs/{clubId}/documents/{documentId}" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 204 (No Content) :** Aucun contenu

### 🎓 **2. GESTION DES SESSIONS DE FORMATION**

#### **2.1 Démarrer une Session**
```bash
curl -X POST "https://api.rotaryclubmanager.com/api/formation/sessions" \
  -H "Authorization: Bearer {JWT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "documentFormationId": "550e8400-e29b-41d4-a716-446655440000",
    "scoreObjectif": 80
  }'
```

**Réponse 201 (Créé) :**
```json
{
  "id": "770e8400-e29b-41d4-a716-446655440002",
  "membreId": "user789",
  "nomMembre": "Pierre Durand",
  "documentFormationId": "550e8400-e29b-41d4-a716-446655440000",
  "titreDocument": "Manuel Rotary 2024",
  "dateDebut": "2025-08-25T16:00:00Z",
  "dateFin": null,
  "scoreActuel": 0,
  "scoreObjectif": 80,
  "statut": 1,
  "nombreQuestions": 0,
  "nombreReponsesCorrectes": 0,
  "pourcentageReussite": 0.0
}
```

#### **2.2 Récupérer une Session**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/sessions/{sessionId}" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
{
  "id": "770e8400-e29b-41d4-a716-446655440002",
  "membreId": "user789",
  "nomMembre": "Pierre Durand",
  "documentFormationId": "550e8400-e29b-41d4-a716-446655440000",
  "titreDocument": "Manuel Rotary 2024",
  "dateDebut": "2025-08-25T16:00:00Z",
  "dateFin": null,
  "scoreActuel": 45,
  "scoreObjectif": 80,
  "statut": 1,
  "nombreQuestions": 10,
  "nombreReponsesCorrectes": 6,
  "pourcentageReussite": 60.0
}
```

#### **2.3 Lister les Sessions de l'Utilisateur**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/sessions" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
[
  {
    "id": "770e8400-e29b-41d4-a716-446655440002",
    "membreId": "user789",
    "nomMembre": "Pierre Durand",
    "documentFormationId": "550e8400-e29b-41d4-a716-446655440000",
    "titreDocument": "Manuel Rotary 2024",
    "dateDebut": "2025-08-25T16:00:00Z",
    "dateFin": null,
    "scoreActuel": 45,
    "scoreObjectif": 80,
    "statut": 1,
    "nombreQuestions": 10,
    "nombreReponsesCorrectes": 6,
    "pourcentageReussite": 60.0
  },
  {
    "id": "880e8400-e29b-41d4-a716-446655440003",
    "membreId": "user789",
    "nomMembre": "Pierre Durand",
    "documentFormationId": "660e8400-e29b-41d4-a716-446655440001",
    "titreDocument": "Procédures du Club",
    "dateDebut": "2025-08-25T17:00:00Z",
    "dateFin": "2025-08-25T18:30:00Z",
    "scoreActuel": 85,
    "scoreObjectif": 80,
    "statut": 4,
    "nombreQuestions": 8,
    "nombreReponsesCorrectes": 7,
    "pourcentageReussite": 87.5
  }
]
```

#### **2.4 Lister les Sessions d'un Club (Admin/President uniquement)**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/clubs/{clubId}/sessions" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
[
  {
    "id": "770e8400-e29b-41d4-a716-446655440002",
    "membreId": "user789",
    "nomMembre": "Pierre Durand",
    "documentFormationId": "550e8400-e29b-41d4-a716-446655440000",
    "titreDocument": "Manuel Rotary 2024",
    "dateDebut": "2025-08-25T16:00:00Z",
    "dateFin": null,
    "scoreActuel": 45,
    "scoreObjectif": 80,
    "statut": 1,
    "nombreQuestions": 10,
    "nombreReponsesCorrectes": 6,
    "pourcentageReussite": 60.0
  },
  {
    "id": "990e8400-e29b-41d4-a716-446655440004",
    "membreId": "user456",
    "nomMembre": "Marie Martin",
    "documentFormationId": "550e8400-e29b-41d4-a716-446655440000",
    "titreDocument": "Manuel Rotary 2024",
    "dateDebut": "2025-08-25T19:00:00Z",
    "dateFin": null,
    "scoreActuel": 70,
    "scoreObjectif": 80,
    "statut": 1,
    "nombreQuestions": 10,
    "nombreReponsesCorrectes": 8,
    "pourcentageReussite": 80.0
  }
]
```

### ❓ **3. GESTION DES QUESTIONS ET RÉPONSES**

#### **3.1 Récupérer les Questions d'une Session**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/sessions/{sessionId}/questions" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
[
  {
    "id": "aa0e8400-e29b-41d4-a716-446655440005",
    "sessionFormationId": "770e8400-e29b-41d4-a716-446655440002",
    "chunkDocumentId": "bb0e8400-e29b-41d4-a716-446655440006",
    "texteQuestion": "Quel est le principe fondamental du Rotary ?",
    "type": 1,
    "options": {
      "A": "Service above self",
      "B": "Business networking",
      "C": "Social events",
      "D": "Political activism"
    },
    "reponseCorrecte": "A",
    "difficulte": 1,
    "estRepondue": true,
    "reponseUtilisateurEstCorrecte": true
  },
  {
    "id": "cc0e8400-e29b-41d4-a716-446655440007",
    "sessionFormationId": "770e8400-e29b-41d4-a716-446655440002",
    "chunkDocumentId": "dd0e8400-e29b-41d4-a716-446655440008",
    "texteQuestion": "Combien de membres peut avoir un club Rotary ?",
    "type": 1,
    "options": {
      "A": "Maximum 50",
      "B": "Maximum 100",
      "C": "Maximum 200",
      "D": "Aucune limite"
    },
    "reponseCorrecte": "D",
    "difficulte": 2,
    "estRepondue": false,
    "reponseUtilisateurEstCorrecte": null
  }
]
```

#### **3.2 Soumettre une Réponse**
```bash
curl -X POST "https://api.rotaryclubmanager.com/api/formation/sessions/{sessionId}/responses" \
  -H "Authorization: Bearer {JWT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "questionFormationId": "cc0e8400-e29b-41d4-a716-446655440007",
    "reponseTexte": "D",
    "tempsReponseMs": 15000
  }'
```

**Réponse 200 :**
```json
{
  "estCorrecte": true,
  "reponseCorrecte": "D",
  "explication": "Un club Rotary peut avoir autant de membres qu'il le souhaite, sans limite fixe.",
  "scoreGagne": 10,
  "scoreTotal": 55,
  "sessionTerminee": false,
  "objectifAtteint": false
}
```

### 🏆 **4. GESTION DES BADGES ET PROGRESSION**

#### **4.1 Récupérer les Badges de l'Utilisateur**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/badges" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
[
  {
    "id": "ee0e8400-e29b-41d4-a716-446655440009",
    "membreId": "user789",
    "nomMembre": "Pierre Durand",
    "type": 1,
    "documentFormationId": "550e8400-e29b-41d4-a716-446655440000",
    "titreDocument": "Manuel Rotary 2024",
    "dateObtention": "2025-08-25T16:30:00Z",
    "pointsGagnes": 50
  },
  {
    "id": "ff0e8400-e29b-41d4-a716-446655440010",
    "membreId": "user789",
    "nomMembre": "Pierre Durand",
    "type": 3,
    "documentFormationId": null,
    "titreDocument": null,
    "dateObtention": "2025-08-25T18:00:00Z",
    "pointsGagnes": 100
  }
]
```

#### **4.2 Récupérer la Progression de l'Utilisateur**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/progression" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
{
  "membreId": "user789",
  "nomMembre": "Pierre Durand",
  "totalPoints": 250,
  "nombreBadges": 3,
  "formationsCompletees": 2,
  "formationsEnCours": 1,
  "scoreMoyen": 75.5,
  "badges": [
    {
      "id": "ee0e8400-e29b-41d4-a716-446655440009",
      "membreId": "user789",
      "nomMembre": "Pierre Durand",
      "type": 1,
      "documentFormationId": "550e8400-e29b-41d4-a716-446655440000",
      "titreDocument": "Manuel Rotary 2024",
      "dateObtention": "2025-08-25T16:30:00Z",
      "pointsGagnes": 50
    },
    {
      "id": "ff0e8400-e29b-41d4-a716-446655440010",
      "membreId": "user789",
      "nomMembre": "Pierre Durand",
      "type": 3,
      "documentFormationId": null,
      "titreDocument": null,
      "dateObtention": "2025-08-25T18:00:00Z",
      "pointsGagnes": 100
    }
  ]
}
```

#### **4.3 Récupérer la Progression d'un Club (Admin/President uniquement)**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/clubs/{clubId}/progression" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
[
  {
    "membreId": "user789",
    "nomMembre": "Pierre Durand",
    "totalPoints": 250,
    "nombreBadges": 3,
    "formationsCompletees": 2,
    "formationsEnCours": 1,
    "scoreMoyen": 75.5,
    "badges": [...]
  },
  {
    "membreId": "user456",
    "nomMembre": "Marie Martin",
    "totalPoints": 180,
    "nombreBadges": 2,
    "formationsCompletees": 1,
    "formationsEnCours": 2,
    "scoreMoyen": 82.0,
    "badges": [...]
  }
]
```

### 🔍 **5. RECHERCHE SÉMANTIQUE**

#### **5.1 Rechercher des Documents**
```bash
curl -X GET "https://api.rotaryclubmanager.com/api/formation/clubs/{clubId}/search?query=procédures%20rotary" \
  -H "Authorization: Bearer {JWT_TOKEN}"
```

**Réponse 200 :**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "titre": "Manuel Rotary 2024",
    "description": "Guide complet des procédures Rotary",
    "cheminFichier": "/uploads/formation/550e8400-e29b-41d4-a716-446655440000.pdf",
    "dateUpload": "2025-08-25T14:30:00Z",
    "uploadePar": "user123",
    "nomUploadeur": "Jean Dupont",
    "clubId": "525418ac-1bf3-4da2-9f4e-1fd13dd03119",
    "estActif": true,
    "type": 1,
    "nombreChunks": 15,
    "nombreSessions": 8
  }
]
```

## 📊 **ENUMS ET VALEURS**

### **TypeDocumentFormation :**
- `1` = ManuelRotary
- `2` = ProcedureClub
- `3` = FormationLeadership
- `4` = ReglementInterieur
- `5` = GuideProjet
- `6` = Autre

### **StatutSession :**
- `1` = EnCours
- `2` = Terminee
- `3` = Abandonnee
- `4` = Reussie

### **TypeQuestion :**
- `1` = QCM
- `2` = VraiFaux
- `3` = QuestionOuverte

### **TypeBadge :**
- `1` = PremierQuiz
- `2` = FormationCompletee
- `3` = ScoreParfait
- `4` = StreakConsecutif
- `5` = ExpertFormation

## 🎨 **INTERFACES FRONTEND À CRÉER**

### **Pour les Administrateurs/Presidents :**
1. **Page de Gestion des Documents** - Upload, liste, édition, suppression
2. **Dashboard de Progression Club** - Vue d'ensemble des formations
3. **Page de Recherche** - Recherche sémantique dans les documents

### **Pour tous les Membres :**
1. **Page de Formation** - Liste des documents disponibles
2. **Interface de Quiz** - Questions interactives avec feedback
3. **Page de Progression Personnelle** - Badges, scores, historique
4. **Page de Session Active** - Quiz en cours avec progression

## 🚀 **FONCTIONNALITÉS CLÉS À IMPLÉMENTER**

1. **Upload de fichiers PDF** avec drag & drop
2. **Interface de quiz adaptative** avec feedback immédiat
3. **Système de badges** avec animations
4. **Barre de progression** en temps réel
5. **Recherche sémantique** avec suggestions
6. **Dashboard analytics** pour les admins
7. **Notifications** pour les nouvelles formations
8. **Export des résultats** en PDF/Excel



## 🎯 **OBJECTIFS UX**

1. **Interface intuitive** et moderne
2. **Feedback immédiat** pour toutes les actions
3. **Gamification** avec badges et points
4. **Progression visuelle** claire
5. **Accessibilité** (WCAG 2.1 AA)
6. **Performance** optimisée
7. **Offline capability** pour les quiz en cours
