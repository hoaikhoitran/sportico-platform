using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SporticoApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingTrainingFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coach_payout_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payout_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    bank_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bank_account_holder = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying", comment: "pending | verified | rejected"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_payout_accounts_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_payout_accounts_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Coach payout account");

            migrationBuilder.CreateTable(
                name: "coach_wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    available_balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    pending_balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    total_earned = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    total_withdrawn = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_wallets_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_wallets_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Internal wallet for coach");

            migrationBuilder.CreateTable(
                name: "training_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    session_count = table.Column<int>(type: "integer", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    goal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying", comment: "pending | published | rejected | archived"),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_packages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_packages_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_training_packages_sport",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Training packages created by coaches");

            migrationBuilder.CreateTable(
                name: "coach_wallet_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coach_wallet_transactions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_wallet_transactions_wallet",
                        column: x => x.coach_wallet_id,
                        principalTable: "coach_wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Coach wallet transactions");

            migrationBuilder.CreateTable(
                name: "withdrawal_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_payout_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying", comment: "pending | approved | rejected | paid | cancelled"),
                    admin_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("withdrawal_requests_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_withdrawal_requests_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_withdrawal_requests_payout_account",
                        column: x => x.coach_payout_account_id,
                        principalTable: "coach_payout_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_withdrawal_requests_wallet",
                        column: x => x.coach_wallet_id,
                        principalTable: "coach_wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Withdrawal requests from coach wallet");

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    training_package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    platform_fee_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    platform_fee_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    coach_receive_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    per_session_coach_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_sessions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    completed_sessions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending_payment'::character varying", comment: "pending_payment | active | completed | cancelled | refunded"),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("bookings_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bookings_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_training_package",
                        column: x => x.training_package_id,
                        principalTable: "training_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Bookings for training package purchases");

            migrationBuilder.CreateTable(
                name: "learner_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    goal_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    height_cm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    body_fat_percent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    current_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    health_notes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    injury_notes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    training_history = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    available_days_per_week = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    preferred_session_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    equipment_available = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("learner_assessments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_learner_assessments_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_learner_assessments_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_learner_assessments_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Learner assessment for personalization");

            migrationBuilder.CreateTable(
                name: "progress_check_ins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    body_fat_percent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    waist_cm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    energy_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sleep_quality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    learner_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    coach_feedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("progress_check_ins_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_progress_check_ins_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_progress_check_ins_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_progress_check_ins_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Progress check-ins for bookings");

            migrationBuilder.CreateTable(
                name: "training_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    goal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    overview = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_weeks = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'draft'::character varying", comment: "draft | active | completed | cancelled"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_plans_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_plans_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_training_plans_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_training_plans_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Training plans for bookings");

            migrationBuilder.CreateTable(
                name: "training_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'requested'::character varying", comment: "requested | scheduled | completed | cancelled | missed"),
                    meeting_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    learner_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    coach_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_sessions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_sessions_booking",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_training_sessions_coach",
                        column: x => x.coach_id,
                        principalTable: "coach_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_training_sessions_learner",
                        column: x => x.learner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Training session schedule for bookings");

            migrationBuilder.CreateTable(
                name: "training_plan_weeks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    training_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_number = table.Column<int>(type: "integer", nullable: false),
                    focus = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_plan_weeks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_plan_weeks_plan",
                        column: x => x.training_plan_id,
                        principalTable: "training_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Weekly breakdown for training plans");

            migrationBuilder.CreateTable(
                name: "training_plan_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    training_plan_week_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_plan_days_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_plan_days_week",
                        column: x => x.training_plan_week_id,
                        principalTable: "training_plan_weeks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Daily breakdown for training plans");

            migrationBuilder.CreateTable(
                name: "training_plan_exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    training_plan_day_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    sets = table.Column<int>(type: "integer", nullable: true),
                    reps = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    intensity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    rest_seconds = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("training_plan_exercises_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_plan_exercises_day",
                        column: x => x.training_plan_day_id,
                        principalTable: "training_plan_days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Exercises for training plan days");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_coach",
                table: "bookings",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_created_at",
                table: "bookings",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_bookings_learner",
                table: "bookings",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_status",
                table: "bookings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_training_package",
                table: "bookings",
                column: "training_package_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_payout_accounts_status",
                table: "coach_payout_accounts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_coach_payout_accounts_coach",
                table: "coach_payout_accounts",
                column: "coach_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_coach_wallet_transactions_coach",
                table: "coach_wallet_transactions",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_coach_wallet_transactions_created_at",
                table: "coach_wallet_transactions",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_coach_wallet_transactions_reference",
                table: "coach_wallet_transactions",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "idx_coach_wallet_transactions_wallet",
                table: "coach_wallet_transactions",
                column: "coach_wallet_id");

            migrationBuilder.CreateIndex(
                name: "uq_coach_wallets_coach",
                table: "coach_wallets",
                column: "coach_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learner_assessments_coach_id",
                table: "learner_assessments",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_learner_assessments_learner_id",
                table: "learner_assessments",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "uq_learner_assessments_booking",
                table: "learner_assessments",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_progress_check_ins_booking_created_at",
                table: "progress_check_ins",
                columns: new[] { "booking_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_progress_check_ins_coach_id",
                table: "progress_check_ins",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_progress_check_ins_learner_id",
                table: "progress_check_ins",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_coach",
                table: "training_packages",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_created_at",
                table: "training_packages",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_published",
                table: "training_packages",
                column: "status",
                filter: "((status)::text = 'published'::text)");

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_sport",
                table: "training_packages",
                column: "sport_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_packages_status",
                table: "training_packages",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_training_plan_days_week_day",
                table: "training_plan_days",
                columns: new[] { "training_plan_week_id", "day_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_training_plan_exercises_day_order",
                table: "training_plan_exercises",
                columns: new[] { "training_plan_day_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "idx_training_plan_weeks_plan_week",
                table: "training_plan_weeks",
                columns: new[] { "training_plan_id", "week_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_training_plans_coach",
                table: "training_plans",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_plans_learner",
                table: "training_plans",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_plans_status",
                table: "training_plans",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_training_plans_booking",
                table: "training_plans",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_booking",
                table: "training_sessions",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_coach",
                table: "training_sessions",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_coach_time",
                table: "training_sessions",
                columns: new[] { "coach_id", "start_time", "end_time" });

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_learner",
                table: "training_sessions",
                column: "learner_id");

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_learner_time",
                table: "training_sessions",
                columns: new[] { "learner_id", "start_time", "end_time" });

            migrationBuilder.CreateIndex(
                name: "idx_training_sessions_status",
                table: "training_sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_withdrawal_requests_coach",
                table: "withdrawal_requests",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "idx_withdrawal_requests_created_at",
                table: "withdrawal_requests",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_withdrawal_requests_status",
                table: "withdrawal_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_coach_payout_account_id",
                table: "withdrawal_requests",
                column: "coach_payout_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_coach_wallet_id",
                table: "withdrawal_requests",
                column: "coach_wallet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coach_wallet_transactions");

            migrationBuilder.DropTable(
                name: "learner_assessments");

            migrationBuilder.DropTable(
                name: "progress_check_ins");

            migrationBuilder.DropTable(
                name: "training_plan_exercises");

            migrationBuilder.DropTable(
                name: "training_sessions");

            migrationBuilder.DropTable(
                name: "withdrawal_requests");

            migrationBuilder.DropTable(
                name: "training_plan_days");

            migrationBuilder.DropTable(
                name: "coach_payout_accounts");

            migrationBuilder.DropTable(
                name: "coach_wallets");

            migrationBuilder.DropTable(
                name: "training_plan_weeks");

            migrationBuilder.DropTable(
                name: "training_plans");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "training_packages");
        }
    }
}
