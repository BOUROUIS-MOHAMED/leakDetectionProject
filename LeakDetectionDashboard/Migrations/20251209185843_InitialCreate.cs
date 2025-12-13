using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeakDetectionDashboard.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Context = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PollIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousPipeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Diameter = table.Column<double>(type: "REAL", nullable: false),
                    Length = table.Column<double>(type: "REAL", nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: false),
                    InstallationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpectedPressureDrop = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pipes_Pipes_PreviousPipeId",
                        column: x => x.PreviousPipeId,
                        principalTable: "Pipes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Pipes_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sensors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousSensorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    DistanceFromPreviousSensor = table.Column<double>(type: "REAL", nullable: false),
                    Elevation = table.Column<double>(type: "REAL", nullable: false),
                    IsWaterTap = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpectedDailyUsage = table.Column<double>(type: "REAL", nullable: false),
                    SensorStatus = table.Column<string>(type: "TEXT", nullable: false),
                    LastCalibrationDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sensors_Pipes_PipeId",
                        column: x => x.PipeId,
                        principalTable: "Pipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sensors_Sensors_PreviousSensorId",
                        column: x => x.PreviousSensorId,
                        principalTable: "Sensors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SensorSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SensorId = table.Column<int>(type: "INTEGER", nullable: false),
                    PipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    PressureCurrent = table.Column<double>(type: "REAL", nullable: false),
                    PressurePreviousSensor = table.Column<double>(type: "REAL", nullable: false),
                    FlowRate = table.Column<double>(type: "REAL", nullable: false),
                    WaterUsageDiff = table.Column<double>(type: "REAL", nullable: false),
                    PressureDropRate = table.Column<double>(type: "REAL", nullable: false),
                    Hour = table.Column<int>(type: "INTEGER", nullable: false),
                    Minute = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    IsWorkingHours = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBreakTime = table.Column<bool>(type: "INTEGER", nullable: false),
                    BreakType = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedUsageMultiplier = table.Column<double>(type: "REAL", nullable: false),
                    MinutesSinceBreakStart = table.Column<int>(type: "INTEGER", nullable: false),
                    OccupancyLevel = table.Column<double>(type: "REAL", nullable: false),
                    PressureCurrentVsBaseline = table.Column<double>(type: "REAL", nullable: false),
                    FlowRateVsBaseline = table.Column<double>(type: "REAL", nullable: false),
                    LeakProbability = table.Column<double>(type: "REAL", nullable: false),
                    IsLeakPredicted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LeakSeverityPredicted = table.Column<int>(type: "INTEGER", nullable: false),
                    LeakTypePredicted = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorSnapshots_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "PollIntervalMinutes" },
                values: new object[] { 1, 5 });

            migrationBuilder.CreateIndex(
                name: "IX_Pipes_PreviousPipeId",
                table: "Pipes",
                column: "PreviousPipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pipes_ZoneId",
                table: "Pipes",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_PipeId",
                table: "Sensors",
                column: "PipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_PreviousSensorId",
                table: "Sensors",
                column: "PreviousSensorId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorSnapshots_SensorId",
                table: "SensorSnapshots",
                column: "SensorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "SensorSnapshots");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Sensors");

            migrationBuilder.DropTable(
                name: "Pipes");

            migrationBuilder.DropTable(
                name: "Zones");
        }
    }
}
