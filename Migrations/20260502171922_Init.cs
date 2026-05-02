using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tilemgr.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    canvas_Name = table.Column<string>(type: "TEXT", nullable: false),
                    canvas_Compressed = table.Column<byte[]>(type: "BLOB", nullable: false),
                    palette_ImgPath = table.Column<string>(type: "TEXT", nullable: false),
                    palette_TileWid = table.Column<int>(type: "INTEGER", nullable: false),
                    palette_TileHei = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
