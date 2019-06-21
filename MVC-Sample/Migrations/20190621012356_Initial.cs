using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MVCSample.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardioMachine",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    MachineType = table.Column<string>(nullable: true),
                    MachineModel = table.Column<string>(nullable: true),
                    MachineNumber = table.Column<int>(nullable: false),
                    capabilities1 = table.Column<int>(nullable: false),
                    capabilities2 = table.Column<int>(nullable: false),
                    capabilities3 = table.Column<int>(nullable: false),
                    capabilities4 = table.Column<int>(nullable: false),
                    capabilities5 = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardioMachine", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardioMachine");
        }
    }
}
