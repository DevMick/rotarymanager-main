# 📧 API d'Envoi de Calendrier par Email

## Vue d'ensemble

L'API `CalendrierEmailController` permet d'envoyer automatiquement le calendrier mensuel d'un club Rotary à tous ses membres par email. Cette fonctionnalité utilise la méthode `GetCalendrierDirect` existante pour récupérer les événements et les présente dans un format HTML professionnel.

## Endpoint Principal

### POST `/api/CalendrierEmail/envoyer-calendrier`

Envoie le calendrier du mois spécifié à tous les membres du club.

#### Authentification
- **Type** : Bearer Token
- **Rôle requis** : Membre du club

#### Paramètres de requête

```json
{
  "clubId": "uuid",           // Obligatoire - ID du club
  "mois": 8,                  // Obligatoire - Mois (1-12)
  "messagePersonnalise": "string", // Optionnel - Message personnalisé
  "urgent": false,            // Optionnel - Marquer comme urgent
  "envoyerCopie": true        // Optionnel - Envoyer une copie
}
```

#### Exemple de requête

```bash
curl -X POST "https://localhost:7001/api/CalendrierEmail/envoyer-calendrier" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "clubId": "12345678-1234-1234-1234-123456789012",
    "mois": 8,
    "messagePersonnalise": "Chers membres, voici le calendrier du mois d'août.",
    "urgent": false,
    "envoyerCopie": true
  }'
```

#### Réponse de succès (200)

```json
{
  "success": true,
  "message": "Calendrier envoyé avec succès à 15 membre(s).",
  "emailId": "abc123-def456-ghi789",
  "nombreDestinataires": 15,
  "nombreEvenements": 8,
  "mois": 8,
  "nomMois": "Août",
  "clubNom": "Rotary Club Abidjan"
}
```

#### Réponses d'erreur

**400 Bad Request**
```json
{
  "success": false,
  "message": "L'identifiant du club est invalide."
}
```

**403 Forbidden**
```json
{
  "success": false,
  "message": "Accès non autorisé à ce club."
}
```

**500 Internal Server Error**
```json
{
  "success": false,
  "message": "Erreur lors de l'envoi du calendrier.",
  "errors": ["Détails de l'erreur"]
}
```

## Fonctionnalités

### 📅 Contenu du Calendrier

Le calendrier inclut automatiquement :

1. **Réunions du club** - Avec type de réunion et heure
2. **Événements spéciaux** - Galas, formations, etc.
3. **Anniversaires des membres** - Célébrations du mois

### 🎨 Template HTML Professionnel

L'email utilise un template HTML moderne avec :
- Design responsive
- Couleurs Rotary (bleu et or)
- Tableau des événements formaté
- Informations importantes en encadré
- Footer avec branding Rotary

### 🔒 Sécurité et Contrôles

- **Authentification** : Token JWT requis
- **Autorisation** : Vérification de l'appartenance au club
- **Rate Limiting** : Protection contre le spam
- **Validation** : Vérification des données d'entrée

### 📊 Statistiques

L'API retourne des statistiques détaillées :
- Nombre de destinataires
- Nombre d'événements inclus
- ID de l'email pour le suivi

## Utilisation dans l'Interface

### Bouton d'envoi

```javascript
// Exemple d'utilisation dans une vue
async function envoyerCalendrier() {
  try {
    const response = await fetch('/api/CalendrierEmail/envoyer-calendrier', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        clubId: currentClubId,
        mois: selectedMonth,
        messagePersonnalise: messageInput.value,
        urgent: urgentCheckbox.checked,
        envoyerCopie: copyCheckbox.checked
      })
    });

    const result = await response.json();
    
    if (result.success) {
      showSuccess(`Calendrier envoyé à ${result.nombreDestinataires} membre(s)`);
    } else {
      showError(result.message);
    }
  } catch (error) {
    showError('Erreur lors de l\'envoi');
  }
}
```

### Interface utilisateur suggérée

```html
<div class="calendrier-email-form">
  <h3>Envoyer le calendrier par email</h3>
  
  <div class="form-group">
    <label>Mois :</label>
    <select id="mois">
      <option value="1">Janvier</option>
      <option value="2">Février</option>
      <!-- ... autres mois ... -->
      <option value="12">Décembre</option>
    </select>
  </div>
  
  <div class="form-group">
    <label>Message personnalisé :</label>
    <textarea id="message" placeholder="Message optionnel..."></textarea>
  </div>
  
  <div class="form-group">
    <label>
      <input type="checkbox" id="urgent"> Marquer comme urgent
    </label>
  </div>
  
  <div class="form-group">
    <label>
      <input type="checkbox" id="copie" checked> Envoyer une copie
    </label>
  </div>
  
  <button onclick="envoyerCalendrier()" class="btn btn-primary">
    📧 Envoyer le calendrier
  </button>
</div>
```

## Configuration

### Variables d'environnement requises

```json
{
  "Email": {
    "SmtpHost": "smtp.hostinger.com",
    "SmtpPort": 465,
    "SmtpUser": "votre-email@domaine.com",
    "SmtpPassword": "votre-mot-de-passe",
    "FromEmail": "noreply@rotaryclub.com",
    "FromName": "Rotary Club Manager"
  },
  "RateLimit": {
    "Email": {
      "PermitLimit": 10,
      "WindowMinutes": 1,
      "QueueLimit": 5
    }
  }
}
```

## Tests

Utilisez le fichier `CalendrierEmail.http` pour tester l'API :

1. Configurez les variables d'environnement
2. Exécutez la requête de connexion
3. Testez l'envoi avec différents paramètres

## Limitations

- **Rate Limiting** : 10 emails par minute par utilisateur
- **Taille du message** : Maximum 1000 caractères pour le message personnalisé
- **Destinataires** : Seuls les membres actifs avec email valide
- **Événements** : Maximum 50 événements par mois (limite pratique)

## Support

Pour toute question ou problème :
- Consultez les logs de l'application
- Vérifiez la configuration SMTP
- Contactez l'administrateur système
