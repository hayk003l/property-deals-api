using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PropertyDeals.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                attempts = table.Column<int>(type: "integer", nullable: false),
                error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_messages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "properties",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                bedrooms = table.Column<int>(type: "integer", nullable: false),
                listed_by_id = table.Column<int>(type: "integer", nullable: false),
                address_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                address_country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                address_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                address_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                address_zip_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                price_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_properties", x => x.id);
                table.ForeignKey(
                    name: "fk_properties_users_listed_by_id",
                    column: x => x.listed_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "deals",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                property_id = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                closing_date = table.Column<DateOnly>(type: "date", nullable: true),
                created_by = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_deals", x => x.id);
                table.ForeignKey(
                    name: "fk_deals_properties_property_id",
                    column: x => x.property_id,
                    principalTable: "properties",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_deals_users_created_by",
                    column: x => x.created_by,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "activity_logs",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                deal_id = table.Column<int>(type: "integer", nullable: false),
                action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                user_id = table.Column<int>(type: "integer", nullable: false),
                details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_activity_logs", x => x.id);
                table.ForeignKey(
                    name: "fk_activity_logs_deals_deal_id",
                    column: x => x.deal_id,
                    principalTable: "deals",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_activity_logs_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "documents",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                deal_id = table.Column<int>(type: "integer", nullable: false),
                file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                s3_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                uploaded_by = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_documents", x => x.id);
                table.ForeignKey(
                    name: "fk_documents_deals_deal_id",
                    column: x => x.deal_id,
                    principalTable: "deals",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_documents_users_uploaded_by",
                    column: x => x.uploaded_by,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "participants",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                deal_id = table.Column<int>(type: "integer", nullable: false),
                user_id = table.Column<int>(type: "integer", nullable: false),
                role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_participants", x => x.id);
                table.ForeignKey(
                    name: "fk_participants_deals_deal_id",
                    column: x => x.deal_id,
                    principalTable: "deals",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_participants_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_activity_logs_deal_id_occurred_on_utc",
            table: "activity_logs",
            columns: new[] { "deal_id", "occurred_on_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_activity_logs_user_id",
            table: "activity_logs",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_deals_created_by",
            table: "deals",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "ix_deals_property_id",
            table: "deals",
            column: "property_id");

        migrationBuilder.CreateIndex(
            name: "ix_deals_status_closing_date",
            table: "deals",
            columns: new[] { "status", "closing_date" });

        migrationBuilder.CreateIndex(
            name: "ix_documents_deal_id",
            table: "documents",
            column: "deal_id");

        migrationBuilder.CreateIndex(
            name: "ix_documents_s3_key",
            table: "documents",
            column: "s3_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_documents_uploaded_by",
            table: "documents",
            column: "uploaded_by");

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_occurred_on_utc",
            table: "outbox_messages",
            column: "occurred_on_utc",
            filter: "processed_on_utc IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_participants_deal_id",
            table: "participants",
            column: "deal_id");

        migrationBuilder.CreateIndex(
            name: "ix_participants_user_id_deal_id",
            table: "participants",
            columns: new[] { "user_id", "deal_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_properties_listed_by_id",
            table: "properties",
            column: "listed_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_email",
            table: "users",
            column: "email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "activity_logs");

        migrationBuilder.DropTable(
            name: "documents");

        migrationBuilder.DropTable(
            name: "outbox_messages");

        migrationBuilder.DropTable(
            name: "participants");

        migrationBuilder.DropTable(
            name: "deals");

        migrationBuilder.DropTable(
            name: "properties");

        migrationBuilder.DropTable(
            name: "users");
    }
}
