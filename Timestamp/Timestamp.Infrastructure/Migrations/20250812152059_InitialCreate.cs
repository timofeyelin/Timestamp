using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Timestamp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeltaTime = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AverageExecutionTime = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    AverageValue = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    MedianValue = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    MaxValue = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    MinValue = table.Column<decimal>(type: "numeric(18,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Values",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutionTime = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    ResultsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Values_Results_ResultsId",
                        column: x => x.ResultsId,
                        principalTable: "Results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Results_FileName",
                table: "Results",
                column: "FileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Values_ResultsId",
                table: "Values",
                column: "ResultsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Values");

            migrationBuilder.DropTable(
                name: "Results");
        }
    }
}
