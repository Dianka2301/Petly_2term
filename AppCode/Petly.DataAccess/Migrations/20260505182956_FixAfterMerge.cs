using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Petly.DataAccess.Migrations
{
    public partial class FixAfterMerge : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `SuccessStories` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `PetId` int NOT NULL,
                    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
                    `StoryText` longtext CHARACTER SET utf8mb4 NOT NULL,
                    `ImageUrl` longtext CHARACTER SET utf8mb4 NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_SuccessStories_pet_PetId`
                        FOREIGN KEY (`PetId`) REFERENCES `pet` (`petId`)
                        ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS `IX_SuccessStories_PetId`
                ON `SuccessStories` (`PetId`);
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `ViewedPets` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `UserId` int NOT NULL,
                    `PetId` int NOT NULL,
                    `ViewedAt` datetime(6) NOT NULL,
                    PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_ViewedPets_AspNetUsers_UserId`
                        FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`)
                        ON DELETE CASCADE,
                    CONSTRAINT `FK_ViewedPets_pet_PetId`
                        FOREIGN KEY (`PetId`) REFERENCES `pet` (`petId`)
                        ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS `IX_ViewedPets_PetId`
                ON `ViewedPets` (`PetId`);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS `IX_ViewedPets_UserId`
                ON `ViewedPets` (`UserId`);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `ViewedPets`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `SuccessStories`;");
        }
    }
}