using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFormationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BadgesFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembreId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DocumentFormationId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DateObtention = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PointsGagnes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgesFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadgesFormation_AspNetUsers_MembreId",
                        column: x => x.MembreId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentsFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CheminFichier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DateUpload = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadePar = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentsFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentsFormation_AspNetUsers_UploadePar",
                        column: x => x.UploadePar,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentsFormation_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChunksDocument",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentFormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    IndexChunk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChunksDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChunksDocument_DocumentsFormation_DocumentFormationId",
                        column: x => x.DocumentFormationId,
                        principalTable: "DocumentsFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionsFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembreId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DocumentFormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScoreActuel = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ScoreObjectif = table.Column<int>(type: "integer", nullable: false, defaultValue: 80),
                    Statut = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionsFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionsFormation_AspNetUsers_MembreId",
                        column: x => x.MembreId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionsFormation_DocumentsFormation_DocumentFormationId",
                        column: x => x.DocumentFormationId,
                        principalTable: "DocumentsFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionsFormation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionFormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TexteQuestion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Options = table.Column<string>(type: "jsonb", nullable: true),
                    ReponseCorrecte = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Difficulte = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionsFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionsFormation_ChunksDocument_ChunkDocumentId",
                        column: x => x.ChunkDocumentId,
                        principalTable: "ChunksDocument",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionsFormation_SessionsFormation_SessionFormationId",
                        column: x => x.SessionFormationId,
                        principalTable: "SessionsFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReponsesUtilisateur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionFormationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReponseTexte = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EstCorrecte = table.Column<bool>(type: "boolean", nullable: false),
                    TempsReponseMs = table.Column<int>(type: "integer", nullable: false),
                    DateReponse = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SessionFormationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReponsesUtilisateur", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReponsesUtilisateur_QuestionsFormation_QuestionFormationId",
                        column: x => x.QuestionFormationId,
                        principalTable: "QuestionsFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReponsesUtilisateur_SessionsFormation_SessionFormationId",
                        column: x => x.SessionFormationId,
                        principalTable: "SessionsFormation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgesFormation_DateObtention",
                table: "BadgesFormation",
                column: "DateObtention");

            migrationBuilder.CreateIndex(
                name: "IX_BadgesFormation_MembreId",
                table: "BadgesFormation",
                column: "MembreId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgesFormation_MembreId_Type_DocumentFormationId",
                table: "BadgesFormation",
                columns: new[] { "MembreId", "Type", "DocumentFormationId" });

            migrationBuilder.CreateIndex(
                name: "IX_BadgesFormation_Type",
                table: "BadgesFormation",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ChunksDocument_DocumentFormationId",
                table: "ChunksDocument",
                column: "DocumentFormationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChunksDocument_DocumentFormationId_IndexChunk",
                table: "ChunksDocument",
                columns: new[] { "DocumentFormationId", "IndexChunk" });

            migrationBuilder.CreateIndex(
                name: "IX_ChunksDocument_IndexChunk",
                table: "ChunksDocument",
                column: "IndexChunk");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsFormation_ClubId",
                table: "DocumentsFormation",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsFormation_ClubId_EstActif",
                table: "DocumentsFormation",
                columns: new[] { "ClubId", "EstActif" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsFormation_DateUpload",
                table: "DocumentsFormation",
                column: "DateUpload");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsFormation_UploadePar",
                table: "DocumentsFormation",
                column: "UploadePar");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionsFormation_ChunkDocumentId",
                table: "QuestionsFormation",
                column: "ChunkDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionsFormation_Difficulte",
                table: "QuestionsFormation",
                column: "Difficulte");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionsFormation_SessionFormationId",
                table: "QuestionsFormation",
                column: "SessionFormationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReponsesUtilisateur_DateReponse",
                table: "ReponsesUtilisateur",
                column: "DateReponse");

            migrationBuilder.CreateIndex(
                name: "IX_ReponsesUtilisateur_QuestionFormationId",
                table: "ReponsesUtilisateur",
                column: "QuestionFormationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReponsesUtilisateur_SessionFormationId",
                table: "ReponsesUtilisateur",
                column: "SessionFormationId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionsFormation_DateDebut",
                table: "SessionsFormation",
                column: "DateDebut");

            migrationBuilder.CreateIndex(
                name: "IX_SessionsFormation_DocumentFormationId",
                table: "SessionsFormation",
                column: "DocumentFormationId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionsFormation_MembreId",
                table: "SessionsFormation",
                column: "MembreId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionsFormation_MembreId_DocumentFormationId",
                table: "SessionsFormation",
                columns: new[] { "MembreId", "DocumentFormationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadgesFormation");

            migrationBuilder.DropTable(
                name: "ReponsesUtilisateur");

            migrationBuilder.DropTable(
                name: "QuestionsFormation");

            migrationBuilder.DropTable(
                name: "ChunksDocument");

            migrationBuilder.DropTable(
                name: "SessionsFormation");

            migrationBuilder.DropTable(
                name: "DocumentsFormation");
        }
    }
}
