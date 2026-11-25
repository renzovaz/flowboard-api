using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FlowBoard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "program_increments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    iteration_count = table.Column<int>(type: "integer", nullable: false),
                    iteration_length_weeks = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_increments", x => x.id);
                    table.CheckConstraint("ck_program_increments_iteration_count", "iteration_count BETWEEN 2 AND 6");
                    table.CheckConstraint("ck_program_increments_iteration_length_weeks", "iteration_length_weeks BETWEEN 1 AND 4");
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    capacity_points = table.Column<int>(type: "integer", nullable: false),
                    wip_limit_in_progress = table.Column<int>(type: "integer", nullable: false),
                    wip_limit_review = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                    table.CheckConstraint("ck_teams_capacity_points", "capacity_points BETWEEN 1 AND 200");
                    table.CheckConstraint("ck_teams_wip_limit_in_progress", "wip_limit_in_progress BETWEEN 1 AND 20");
                    table.CheckConstraint("ck_teams_wip_limit_review", "wip_limit_review BETWEEN 1 AND 20");
                });

            migrationBuilder.CreateTable(
                name: "features",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    program_increment_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    business_value = table.Column<int>(type: "integer", nullable: false),
                    time_criticality = table.Column<int>(type: "integer", nullable: false),
                    risk_reduction = table.Column<int>(type: "integer", nullable: false),
                    job_size = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_features", x => x.id);
                    table.CheckConstraint("ck_features_business_value", "business_value IN (1, 2, 3, 5, 8, 13, 20)");
                    table.CheckConstraint("ck_features_job_size", "job_size IN (1, 2, 3, 5, 8, 13, 20)");
                    table.CheckConstraint("ck_features_risk_reduction", "risk_reduction IN (1, 2, 3, 5, 8, 13, 20)");
                    table.CheckConstraint("ck_features_time_criticality", "time_criticality IN (1, 2, 3, 5, 8, 13, 20)");
                    table.ForeignKey(
                        name: "fk_features_program_increments_program_increment_id",
                        column: x => x.program_increment_id,
                        principalTable: "program_increments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    feature_id = table.Column<int>(type: "integer", nullable: false),
                    team_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    acceptance_criteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    story_points = table.Column<int>(type: "integer", nullable: true),
                    iteration = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stories", x => x.id);
                    table.CheckConstraint("ck_stories_iteration", "iteration IS NULL OR iteration >= 1");
                    table.CheckConstraint("ck_stories_status", "status IN ('Backlog', 'Ready', 'InProgress', 'Review', 'Done')");
                    table.CheckConstraint("ck_stories_story_points", "story_points IS NULL OR story_points IN (1, 2, 3, 5, 8, 13)");
                    table.ForeignKey(
                        name: "fk_stories_features_feature_id",
                        column: x => x.feature_id,
                        principalTable: "features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stories_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_features_program_increment_id",
                table: "features",
                column: "program_increment_id");

            migrationBuilder.CreateIndex(
                name: "ix_program_increments_name",
                table: "program_increments",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stories_feature_id",
                table: "stories",
                column: "feature_id");

            migrationBuilder.CreateIndex(
                name: "ix_stories_team_id_iteration",
                table: "stories",
                columns: new[] { "team_id", "iteration" });

            migrationBuilder.CreateIndex(
                name: "ix_stories_team_id_status",
                table: "stories",
                columns: new[] { "team_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_teams_name",
                table: "teams",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stories");

            migrationBuilder.DropTable(
                name: "features");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "program_increments");
        }
    }
}
