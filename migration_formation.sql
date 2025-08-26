-- Migration pour le module de formation IA
-- Rotary Club Manager

-- 1. Création de l'extension pgvector pour les embeddings
CREATE EXTENSION IF NOT EXISTS vector;

-- 2. Création de la table DocumentFormation
CREATE TABLE document_formation (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    titre VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    chemin_fichier VARCHAR(500) NOT NULL,
    date_upload TIMESTAMP DEFAULT NOW(),
    uploade_par VARCHAR(450) NOT NULL,
    club_id UUID NOT NULL,
    est_actif BOOLEAN DEFAULT true,
    type INTEGER NOT NULL,
    CONSTRAINT fk_document_formation_uploadeur FOREIGN KEY (uploade_par) REFERENCES "AspNetUsers"(Id) ON DELETE RESTRICT,
    CONSTRAINT fk_document_formation_club FOREIGN KEY (club_id) REFERENCES club(id) ON DELETE CASCADE
);

-- 3. Création de la table ChunkDocument
CREATE TABLE chunk_document (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_formation_id UUID NOT NULL,
    contenu TEXT NOT NULL,
    embedding VECTOR(1536),
    metadata JSONB,
    index_chunk INTEGER NOT NULL,
    CONSTRAINT fk_chunk_document_document_formation FOREIGN KEY (document_formation_id) REFERENCES document_formation(id) ON DELETE CASCADE
);

-- 4. Création de la table SessionFormation
CREATE TABLE session_formation (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    membre_id VARCHAR(450) NOT NULL,
    document_formation_id UUID NOT NULL,
    date_debut TIMESTAMP DEFAULT NOW(),
    date_fin TIMESTAMP,
    score_actuel INTEGER DEFAULT 0,
    score_objectif INTEGER DEFAULT 80,
    statut INTEGER DEFAULT 1,
    CONSTRAINT fk_session_formation_membre FOREIGN KEY (membre_id) REFERENCES "AspNetUsers"(Id) ON DELETE CASCADE,
    CONSTRAINT fk_session_formation_document_formation FOREIGN KEY (document_formation_id) REFERENCES document_formation(id) ON DELETE CASCADE
);

-- 5. Création de la table QuestionFormation
CREATE TABLE question_formation (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_formation_id UUID NOT NULL,
    chunk_document_id UUID NOT NULL,
    texte_question VARCHAR(1000) NOT NULL,
    type INTEGER NOT NULL,
    options JSONB,
    reponse_correcte VARCHAR(500) NOT NULL,
    difficulte INTEGER DEFAULT 1,
    CONSTRAINT fk_question_formation_session_formation FOREIGN KEY (session_formation_id) REFERENCES session_formation(id) ON DELETE CASCADE,
    CONSTRAINT fk_question_formation_chunk_document FOREIGN KEY (chunk_document_id) REFERENCES chunk_document(id) ON DELETE CASCADE
);

-- 6. Création de la table ReponseUtilisateur
CREATE TABLE reponse_utilisateur (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    question_formation_id UUID NOT NULL,
    reponse_utilisateur VARCHAR(1000) NOT NULL,
    est_correcte BOOLEAN NOT NULL,
    temps_reponse_ms INTEGER NOT NULL,
    date_reponse TIMESTAMP DEFAULT NOW(),
    CONSTRAINT fk_reponse_utilisateur_question_formation FOREIGN KEY (question_formation_id) REFERENCES question_formation(id) ON DELETE CASCADE
);

-- 7. Création de la table BadgeFormation
CREATE TABLE badge_formation (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    membre_id VARCHAR(450) NOT NULL,
    type INTEGER NOT NULL,
    document_formation_id VARCHAR(450),
    date_obtention TIMESTAMP DEFAULT NOW(),
    points_gagnes INTEGER DEFAULT 0,
    CONSTRAINT fk_badge_formation_membre FOREIGN KEY (membre_id) REFERENCES "AspNetUsers"(Id) ON DELETE CASCADE
);

-- 8. Création des index pour optimiser les performances

-- Index pour DocumentFormation
CREATE INDEX idx_document_formation_club_id ON document_formation(club_id);
CREATE INDEX idx_document_formation_uploade_par ON document_formation(uploade_par);
CREATE INDEX idx_document_formation_date_upload ON document_formation(date_upload);
CREATE INDEX idx_document_formation_club_actif ON document_formation(club_id, est_actif);

-- Index pour ChunkDocument
CREATE INDEX idx_chunk_document_document_formation_id ON chunk_document(document_formation_id);
CREATE INDEX idx_chunk_document_index_chunk ON chunk_document(index_chunk);
CREATE INDEX idx_chunk_document_doc_index ON chunk_document(document_formation_id, index_chunk);

-- Index vectoriel pour la recherche sémantique
CREATE INDEX idx_chunk_embedding ON chunk_document USING ivfflat (embedding vector_cosine_ops);

-- Index pour SessionFormation
CREATE INDEX idx_session_formation_membre_id ON session_formation(membre_id);
CREATE INDEX idx_session_formation_document_formation_id ON session_formation(document_formation_id);
CREATE INDEX idx_session_formation_date_debut ON session_formation(date_debut);
CREATE INDEX idx_session_formation_membre_doc ON session_formation(membre_id, document_formation_id);

-- Index pour QuestionFormation
CREATE INDEX idx_question_formation_session_formation_id ON question_formation(session_formation_id);
CREATE INDEX idx_question_formation_chunk_document_id ON question_formation(chunk_document_id);
CREATE INDEX idx_question_formation_difficulte ON question_formation(difficulte);

-- Index pour ReponseUtilisateur
CREATE INDEX idx_reponse_utilisateur_question_formation_id ON reponse_utilisateur(question_formation_id);
CREATE INDEX idx_reponse_utilisateur_date_reponse ON reponse_utilisateur(date_reponse);

-- Index pour BadgeFormation
CREATE INDEX idx_badge_formation_membre_id ON badge_formation(membre_id);
CREATE INDEX idx_badge_formation_type ON badge_formation(type);
CREATE INDEX idx_badge_formation_date_obtention ON badge_formation(date_obtention);
CREATE INDEX idx_badge_formation_membre_type_doc ON badge_formation(membre_id, type, document_formation_id);

-- 9. Insertion de données de test (optionnel)

-- Insertion d'un document de formation de test
INSERT INTO document_formation (id, titre, description, chemin_fichier, uploade_par, club_id, type)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'Manuel Rotary 2024',
    'Guide complet des procédures Rotary International',
    '/uploads/formation/manuel-rotary-2024.pdf',
    (SELECT id FROM "AspNetUsers" LIMIT 1),
    (SELECT id FROM club LIMIT 1),
    1
);

-- Insertion de chunks de test
INSERT INTO chunk_document (id, document_formation_id, contenu, index_chunk)
VALUES 
    ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'Le Rotary International est une organisation de service international.', 1),
    ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'Les réunions de club doivent avoir lieu régulièrement selon les statuts.', 2),
    ('44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', 'Chaque membre doit participer activement aux projets du club.', 3);

-- 10. Commentaires sur les tables

COMMENT ON TABLE document_formation IS 'Documents PDF de formation uploadés par les administrateurs';
COMMENT ON TABLE chunk_document IS 'Chunks de texte extraits des PDFs avec embeddings pour la recherche sémantique';
COMMENT ON TABLE session_formation IS 'Sessions de formation des membres';
COMMENT ON TABLE question_formation IS 'Questions générées automatiquement pour les quiz';
COMMENT ON TABLE reponse_utilisateur IS 'Réponses des utilisateurs aux questions de formation';
COMMENT ON TABLE badge_formation IS 'Système de gamification avec badges pour les membres';

-- 11. Vérification de la migration

-- Vérifier que les tables ont été créées
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('document_formation', 'chunk_document', 'session_formation', 'question_formation', 'reponse_utilisateur', 'badge_formation');

-- Vérifier que l'extension pgvector est installée
SELECT * FROM pg_extension WHERE extname = 'vector';

-- Vérifier que les index vectoriels sont créés
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'chunk_document' 
AND indexname = 'idx_chunk_embedding';
