using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shmear.EntityFramework.Migrations
{
    public partial class card : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Game",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Team1Points = table.Column<int>(nullable: false),
                    Team2Points = table.Column<int>(nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    StartedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(unicode: false, maxLength: 50, nullable: false),
                    ConnectionId = table.Column<string>(unicode: false, maxLength: 1000, nullable: false),
                    KeepAlive = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suit",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(unicode: false, maxLength: 10, nullable: false),
                    Char = table.Column<string>(maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Value",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(unicode: false, maxLength: 10, nullable: false),
                    Char = table.Column<string>(maxLength: 1, nullable: false),
                    Points = table.Column<int>(nullable: false),
                    Sequence = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Value", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamePlayer",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    PlayerId = table.Column<int>(nullable: false),
                    GameId = table.Column<int>(nullable: false),
                    SeatNumber = table.Column<int>(nullable: false),
                    Wager = table.Column<int>(nullable: true),
                    Ready = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlayer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamePlayer_Game",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GamePlayer_Player",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerComputer",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Instance = table.Column<string>(unicode: false, maxLength: 50, nullable: false),
                    Version = table.Column<string>(unicode: false, maxLength: 50, nullable: false),
                    PlayerId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerComputer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerComputer_Player",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trick",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    GameId = table.Column<int>(nullable: false),
                    WinningPlayerId = table.Column<int>(nullable: true),
                    Sequence = table.Column<int>(nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trick", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trick_Game",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trick_Player",
                        column: x => x.WinningPlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Board",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DealerPlayerId = table.Column<int>(nullable: true),
                    TrumpSuitId = table.Column<int>(nullable: true),
                    GameId = table.Column<int>(nullable: false),
                    Team1Wager = table.Column<int>(nullable: true),
                    Team2Wager = table.Column<int>(nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Board", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Board_Player",
                        column: x => x.DealerPlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Board_Game",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Board_Suit",
                        column: x => x.TrumpSuitId,
                        principalTable: "Suit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Card",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    SuitId = table.Column<int>(nullable: false),
                    ValueId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Card", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Card_Suit",
                        column: x => x.SuitId,
                        principalTable: "Suit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Card_Value",
                        column: x => x.ValueId,
                        principalTable: "Value",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandCard",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    GameId = table.Column<int>(nullable: false),
                    PlayerId = table.Column<int>(nullable: false),
                    CardId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandCard", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandCard_Card",
                        column: x => x.CardId,
                        principalTable: "Card",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandCard_Game",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandCard_Player",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrickCard",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    TrickId = table.Column<int>(nullable: false),
                    PlayerId = table.Column<int>(nullable: false),
                    CardId = table.Column<int>(nullable: false),
                    Sequence = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrickCard", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrickCard_Card",
                        column: x => x.CardId,
                        principalTable: "Card",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrickCard_Player",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrickCard_Trick",
                        column: x => x.TrickId,
                        principalTable: "Trick",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Board_DealerPlayerId",
                table: "Board",
                column: "DealerPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Board_GameId",
                table: "Board",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Board_TrumpSuitId",
                table: "Board",
                column: "TrumpSuitId");

            migrationBuilder.CreateIndex(
                name: "IX_Card_SuitId",
                table: "Card",
                column: "SuitId");

            migrationBuilder.CreateIndex(
                name: "IX_Card_ValueId",
                table: "Card",
                column: "ValueId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayer_GameId",
                table: "GamePlayer",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayer_PlayerId",
                table: "GamePlayer",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_HandCard_CardId",
                table: "HandCard",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_HandCard_GameId",
                table: "HandCard",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_HandCard_PlayerId",
                table: "HandCard",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerComputer_PlayerId",
                table: "PlayerComputer",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Trick_GameId",
                table: "Trick",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Trick_WinningPlayerId",
                table: "Trick",
                column: "WinningPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TrickCard_CardId",
                table: "TrickCard",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_TrickCard_PlayerId",
                table: "TrickCard",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TrickCard_TrickId",
                table: "TrickCard",
                column: "TrickId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Board");

            migrationBuilder.DropTable(
                name: "GamePlayer");

            migrationBuilder.DropTable(
                name: "HandCard");

            migrationBuilder.DropTable(
                name: "PlayerComputer");

            migrationBuilder.DropTable(
                name: "TrickCard");

            migrationBuilder.DropTable(
                name: "Card");

            migrationBuilder.DropTable(
                name: "Trick");

            migrationBuilder.DropTable(
                name: "Suit");

            migrationBuilder.DropTable(
                name: "Value");

            migrationBuilder.DropTable(
                name: "Game");

            migrationBuilder.DropTable(
                name: "Player");
        }
    }
}
