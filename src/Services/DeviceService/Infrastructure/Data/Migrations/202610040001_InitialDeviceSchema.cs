using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SmartFactory.Services.DeviceService.Infrastructure.Data;

#nullable disable

namespace SmartFactory.Services.DeviceService.Infrastructure.Data.Migrations;

[DbContext(typeof(DeviceDbContext))]
[Migration("202610040001_InitialDeviceSchema")]
public sealed class InitialDeviceSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Devices",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DeviceId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                FirmwareVersion = table.Column<string>(type: "text", nullable: false),
                SoftwareVersion = table.Column<string>(type: "text", nullable: false),
                LastUpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                PendingUpdateVersion = table.Column<string>(type: "text", nullable: true),
                UpdateStatus = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Devices", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Devices");
    }
}
