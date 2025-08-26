# Rotary Club Manager - Project Overview & Database Schema

## 📋 Project Overview

### 🎯 Purpose
The **Rotary Club Manager** is a comprehensive .NET 8 API designed to manage Rotary clubs, their members, meetings, events, budgets, and communications. It provides a complete digital solution for Rotary club administration with features for member management, meeting organization, event planning, budget tracking, and communication tools.

### 🏗️ Architecture
The project follows **Clean Architecture** principles with a layered structure:

```
RotaryClubManager/
├── RotaryClubManager.API/              # Presentation Layer (Controllers, DTOs)
├── RotaryClubManager.Application/      # Application Layer (Services, Validators)
├── RotaryClubManager.Domain/           # Domain Layer (Entities, Identity)
└── RotaryClubManager.Infrastructure/   # Infrastructure Layer (Data, External Services)
```

### 🛠️ Technology Stack
- **Framework**: .NET 8
- **ORM**: Entity Framework Core
- **Database**: PostgreSQL (production) / SQL Server (development)
- **Authentication**: JWT Bearer with ASP.NET Core Identity
- **Validation**: FluentValidation
- **Documentation**: Swagger/OpenAPI
- **Communication**: Meta WhatsApp Business API, Email SMTP
- **Rate Limiting**: Built-in ASP.NET Core Rate Limiting
- **Deployment**: Docker, Render.com

### 🚀 Key Features
- **Member Management**: Registration, profiles, roles, and status tracking
- **Meeting Management**: Planning, agendas, attendance tracking
- **Event Management**: Internal/external events with budget tracking
- **Budget Management**: Multi-level budget categories with realization tracking
- **Document Management**: File uploads with categorization
- **Communication**: WhatsApp and email integration
- **Gala Management**: Complete gala event management system
- **Committee Management**: Roles, responsibilities, and deadlines
- **Authentication & Authorization**: JWT-based security with role-based access

---

## 🗄️ Database Schema

### 📊 Entity Relationship Overview

The database consists of several interconnected modules:

1. **Identity & User Management**
2. **Club & Membership Management**
3. **Meeting & Agenda Management**
4. **Event Management**
5. **Budget & Financial Management**
6. **Document Management**
7. **Gala Event Management**
8. **Committee & Function Management**

### 🔐 Identity & User Management

#### ApplicationUser
```sql
-- Extends ASP.NET Core Identity
ApplicationUser {
    Id (string, PK)                    -- Identity User ID
    UserName (string)                  -- Username/Email
    Email (string)                     -- Email address
    FirstName (string, 100)            -- First name
    LastName (string, 100)             -- Last name
    ProfilePictureUrl (string, 200)    -- Profile picture URL
    JoinedDate (DateTime)              -- Membership start date
    IsActive (bool)                    -- Account status
    NumeroMembre (string, 50)          -- Member number
    DateAnniversaire (DateTime)        -- Birthday
}
```

#### UserClub (Many-to-Many Relationship)
```sql
UserClub {
    Id (Guid, PK)
    UserId (string, FK)                -- References ApplicationUser
    ClubId (Guid, FK)                  -- References Club
}
```

### 🏢 Club & Membership Management

#### Club
```sql
Club {
    Id (Guid, PK)
    Name (string, 100)                 -- Club name
    DateCreation (string, 50)          -- Creation date
    NumeroClub (int, unique)           -- Club number
    NumeroTelephone (string, 20)       -- Phone number
    Email (string, 100, unique)        -- Club email
    LieuReunion (string, 200)          -- Meeting location
    ParrainePar (string, 100)          -- Sponsor
    JourReunion (string, 20)           -- Meeting day
    HeureReunion (TimeSpan)            -- Meeting time
    Frequence (string, 50)             -- Meeting frequency
    Adresse (string, 300)              -- Address
}
```

#### Mandat (Term/Period)
```sql
Mandat {
    Id (Guid, PK)
    Annee (int)                        -- Year
    DateDebut (DateTime)               -- Start date
    DateFin (DateTime)                 -- End date
    Description (string, 200)          -- Description
    MontantCotisation (decimal)        -- Membership fee
    EstActuel (bool)                   -- Current term flag
    ClubId (Guid, FK)                  -- References Club
}
```

#### Cotisation & PaiementCotisation
```sql
Cotisation {
    Id (Guid, PK)
    Montant (int)                      -- Amount
    MembreId (string, FK)              -- References ApplicationUser
    MandatId (Guid, FK)                -- References Mandat
}

PaiementCotisation {
    Id (Guid, PK)
    Montant (int)                      -- Payment amount
    Date (DateTime)                    -- Payment date
    Commentaires (string, 500)         -- Comments
    MembreId (string, FK)              -- References ApplicationUser
    ClubId (Guid, FK)                  -- References Club
}
```

### 📅 Meeting & Agenda Management

#### TypeReunion
```sql
TypeReunion {
    Id (Guid, PK)
    Libelle (string, 100, unique)      -- Meeting type label
}
```

#### Reunion
```sql
Reunion {
    Id (Guid, PK)
    Date (Date)                        -- Meeting date
    Heure (Time)                       -- Meeting time
    TypeReunionId (Guid, FK)           -- References TypeReunion
    ClubId (Guid, FK)                  -- References Club
}
```

#### OrdreDuJour (Agenda)
```sql
OrdreDuJour {
    Id (Guid, PK)
    Description (string, 1000)         -- Agenda description
    Rapport (string)                   -- Meeting report
    ReunionId (Guid, FK)               -- References Reunion
}
```

#### OrdreJourRapport
```sql
OrdreJourRapport {
    Id (Guid, PK)
    OrdreDuJourId (Guid, FK)           -- References OrdreDuJour
    Texte (string)                     -- Report text
    Divers (string)                    -- Miscellaneous notes
}
```

#### ListePresence (Attendance)
```sql
ListePresence {
    Id (Guid, PK)
    MembreId (string, FK)              -- References ApplicationUser
    ReunionId (Guid, FK)               -- References Reunion
}
```

#### InviteReunion (Meeting Guests)
```sql
InviteReunion {
    Id (Guid, PK)
    Nom (string, 100)                  -- Last name
    Prenom (string, 100)               -- First name
    Email (string, 255)                -- Email
    Telephone (string, 20)             -- Phone
    Organisation (string, 200)         -- Organization
    ReunionId (Guid, FK)               -- References Reunion
}
```

#### ReunionDocument
```sql
ReunionDocument {
    Id (Guid, PK)
    Libelle (string, 200)              -- Document label
    ReunionId (Guid, FK)               -- References Reunion
    Document (bytea)                   -- Document binary data
}
```

### 🎉 Event Management

#### Evenement
```sql
Evenement {
    Id (Guid, PK)
    Libelle (string, 200)              -- Event label
    Date (DateTime)                    -- Event date
    Lieu (string, 300)                 -- Location
    Description (string, 1000)         -- Description
    EstInterne (bool)                  -- Internal event flag
    ClubId (Guid, FK)                  -- References Club
}
```

#### EvenementDocument
```sql
EvenementDocument {
    Id (Guid, PK)
    Libelle (string, 200)              -- Document label
    Document (bytea)                   -- Document binary data
    DateAjout (DateTime)               -- Upload date
    EvenementId (Guid, FK)             -- References Evenement
}
```

#### EvenementImage
```sql
EvenementImage {
    Id (Guid, PK)
    Image (bytea)                      -- Image binary data
    Description (string, 500)          -- Image description
    DateAjout (DateTime)               -- Upload date
    EvenementId (Guid, FK)             -- References Evenement
}
```

#### EvenementBudget
```sql
EvenementBudget {
    Id (Guid, PK)
    Libelle (string, 200)              -- Budget item label
    MontantBudget (decimal(18,2))      -- Budgeted amount
    MontantRealise (decimal(18,2))     -- Realized amount
    EvenementId (Guid, FK)             -- References Evenement
}
```

#### EvenementRecette
```sql
EvenementRecette {
    Id (Guid, PK)
    Libelle (string, 200)              -- Revenue item label
    Montant (decimal(18,2))            -- Revenue amount
    EvenementId (Guid, FK)             -- References Evenement
}
```

### 💰 Budget & Financial Management

#### TypeBudget
```sql
TypeBudget {
    Id (Guid, PK)
    Libelle (string, 50, unique)       -- Budget type (Expenses, Revenue)
}
```

#### CategoryBudget
```sql
CategoryBudget {
    Id (Guid, PK)
    Libelle (string, 100)              -- Category label
    TypeBudgetId (Guid, FK)            -- References TypeBudget
}
```

#### SousCategoryBudget
```sql
SousCategoryBudget {
    Id (Guid, PK)
    Libelle (string, 150)              -- Subcategory label
    CategoryBudgetId (Guid, FK)        -- References CategoryBudget
    ClubId (Guid, FK)                  -- References Club
}
```

#### RubriqueBudget
```sql
RubriqueBudget {
    Id (Guid, PK)
    Libelle (string, 200)              -- Budget item label
    PrixUnitaire (decimal(18,2))       -- Unit price
    Quantite (int)                     -- Quantity
    MontantRealise (decimal(18,2))     -- Realized amount
    SousCategoryBudgetId (Guid, FK)    -- References SousCategoryBudget
    MandatId (Guid, FK)                -- References Mandat
    ClubId (Guid, FK)                  -- References Club
}
```

#### RubriqueBudgetRealise
```sql
RubriqueBudgetRealise {
    Id (Guid, PK)
    Date (DateTime)                    -- Realization date
    Montant (decimal(18,2))            -- Realized amount
    Commentaires (string, 500)         -- Comments
    RubriqueBudgetId (Guid, FK)        -- References RubriqueBudget
}
```

### 📄 Document Management

#### Categorie
```sql
Categorie {
    Id (Guid, PK)
    Libelle (string, 100, unique)      -- Category label
}
```

#### TypeDocument
```sql
TypeDocument {
    Id (Guid, PK)
    Libelle (string, 100, unique)      -- Document type label
}
```

#### Document
```sql
Document {
    Id (Guid, PK)
    Nom (string, 200)                  -- Document name
    Description (string, 1000)         -- Description
    Fichier (bytea)                    -- File binary data
    CategorieId (Guid, FK)             -- References Categorie
    TypeDocumentId (Guid, FK)          -- References TypeDocument
    ClubId (Guid, FK)                  -- References Club
}
```

### 🎭 Gala Event Management

#### Gala
```sql
Gala {
    Id (Guid, PK)
    Libelle (string)                   -- Gala label
    Date (DateTime)                    -- Gala date
    Lieu (string)                      -- Location
    NombreTables (int)                 -- Number of tables
    NombreSouchesTickets (int)         -- Number of ticket books
    QuantiteParSoucheTickets (int)     -- Tickets per book
    NombreSouchesTombola (int)         -- Number of raffle books
    QuantiteParSoucheTombola (int)     -- Raffle tickets per book
}
```

#### GalaInvites
```sql
GalaInvites {
    Id (Guid, PK)
    Nom_Prenom (string)                -- Full name
    Present (bool)                     -- Attendance status
    GalaId (Guid, FK)                  -- References Gala
}
```

#### GalaTable
```sql
GalaTable {
    Id (Guid, PK)
    TableLibelle (string)              -- Table label
    GalaId (Guid, FK)                  -- References Gala
}
```

#### GalaTableAffectation
```sql
GalaTableAffectation {
    Id (Guid, PK)
    GalaTableId (Guid, FK)             -- References GalaTable
    GalaInvitesId (Guid, FK)           -- References GalaInvites
    DateAjout (DateTime)               -- Assignment date
}
```

#### GalaTicket
```sql
GalaTicket {
    Id (Guid, PK)
    Quantite (int)                     -- Quantity
    Externe (string, 250)              -- External participant name
    GalaId (Guid, FK)                  -- References Gala
    MembreId (string, FK)              -- References ApplicationUser (optional)
}
```

#### GalaTombola
```sql
GalaTombola {
    Id (Guid, PK)
    Quantite (int)                     -- Quantity
    Externe (string, 250)              -- External participant name
    GalaId (Guid, FK)                  -- References Gala
    MembreId (string, FK)              -- References ApplicationUser (optional)
}
```

### 👥 Committee & Function Management

#### Fonction
```sql
Fonction {
    Id (Guid, PK)
    NomFonction (string, 100, unique)  -- Function name
}
```

#### FonctionEcheances (Deadlines)
```sql
FonctionEcheances {
    Id (Guid, PK)
    Libelle (string, 200)              -- Deadline label
    DateButoir (DateTime)              -- Due date
    Frequence (int)                    -- Frequency enum
    FonctionId (Guid, FK)              -- References Fonction
}
```

#### FonctionRolesResponsabilites
```sql
FonctionRolesResponsabilites {
    Id (Guid, PK)
    Libelle (string, 200)              -- Role label
    Description (string, 1000)         -- Description
    FonctionId (Guid, FK)              -- References Fonction
}
```

#### Comite
```sql
Comite {
    Id (Guid, PK)
    Nom (string, 100)                  -- Committee name
    MandatId (Guid, FK)                -- References Mandat
    ClubId (Guid, FK)                  -- References Club
}
```

#### ComiteMembre
```sql
ComiteMembre {
    Id (Guid, PK)
    MandatId (Guid, FK)                -- References Mandat
    MembreId (string, FK)              -- References ApplicationUser
    FonctionId (Guid, FK)              -- References Fonction
}
```

#### Commission
```sql
Commission {
    Id (Guid, PK)
    Nom (string, 100)                  -- Commission name
    Description (string, 200)          -- Description
    ClubId (Guid, FK)                  -- References Club
}
```

#### MembreCommission
```sql
MembreCommission {
    Id (Guid, PK)
    EstResponsable (bool)              -- Is responsible flag
    DateNomination (DateTime)          -- Nomination date
    EstActif (bool)                    -- Active status
    Commentaires (string, 500)         -- Comments
    MembreId (string, FK)              -- References ApplicationUser
    CommissionId (Guid, FK)            -- References Commission
    MandatId (Guid, FK)                -- References Mandat
}
```

### 📧 Communication

#### Email
```sql
Email {
    Id (Guid, PK)
    Sujet (string)                     -- Subject
    Corps (string)                     -- Body
    Destinataires (string)             -- Recipients
    DateEnvoi (DateTime)               -- Send date
    Statut (string)                    -- Status
    ClubId (Guid, FK)                  -- References Club
}
```

---

## 🔗 Key Relationships

### One-to-Many Relationships
- **Club** → **Mandat** (One club can have multiple terms)
- **Club** → **Reunion** (One club can have multiple meetings)
- **Club** → **Evenement** (One club can have multiple events)
- **Mandat** → **RubriqueBudget** (One term can have multiple budget items)
- **Reunion** → **OrdreDuJour** (One meeting can have multiple agenda items)
- **Evenement** → **EvenementBudget** (One event can have multiple budget items)

### Many-to-Many Relationships
- **ApplicationUser** ↔ **Club** (via UserClub)
- **GalaTable** ↔ **GalaInvites** (via GalaTableAffectation)
- **ApplicationUser** ↔ **Commission** (via MembreCommission)

### Complex Relationships
- **Budget Hierarchy**: TypeBudget → CategoryBudget → SousCategoryBudget → RubriqueBudget
- **Meeting Management**: Reunion → OrdreDuJour → OrdreJourRapport
- **Event Management**: Evenement → EvenementBudget/EvenementRecette/EvenementDocument

---

## 🎯 Business Logic Highlights

### Multi-Tenant Architecture
- Each club operates independently
- Data isolation through ClubId foreign keys
- User-club associations through UserClub junction table

### Temporal Data Management
- Mandat (terms) for time-based data organization
- Historical tracking of budget realizations
- Meeting attendance and event participation records

### Flexible Communication
- Support for both internal members and external guests
- WhatsApp integration for real-time communication
- Email system for formal communications

### Comprehensive Event Management
- Internal and external event distinction
- Budget tracking with planned vs. realized amounts
- Document and image management for events

### Advanced Budget System
- Hierarchical budget structure
- Real-time budget vs. actual tracking
- Multi-level categorization for detailed financial management

---

## 🚀 Deployment & Configuration

### Environment Configuration
- **Development**: SQL Server LocalDB
- **Production**: PostgreSQL on Render.com
- **Docker**: Containerized deployment support

### Security Features
- JWT-based authentication
- Role-based authorization
- Rate limiting for API protection
- Input validation with FluentValidation

### External Integrations
- **Meta WhatsApp Business API**: Real-time messaging
- **SMTP Email**: Formal communications
- **File Storage**: Binary document storage in database

This comprehensive system provides a complete digital solution for Rotary club management, covering all aspects from member administration to financial tracking and event organization.
