-- Tek kullanıcıya Admin rolü atar - menü erişimi için
-- pgAdmin veya psql ile çalıştırın: psql -U postgres -d YOUR_DB -f GrantAdminRoleToFirstUser.sql

-- 1. Admin rolü yoksa oluştur
INSERT INTO "AspNetRoles" ("Name", "NormalizedName")
SELECT 'Admin', 'ADMIN'
WHERE NOT EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "NormalizedName" = 'ADMIN');

-- 2. İlk sistem kullanıcısına (MembershipId ve CompanyId NULL olan) Admin rolü ata
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" u, "AspNetRoles" r
WHERE r."NormalizedName" = 'ADMIN'
  AND u."MembershipId" IS NULL
  AND u."CompanyId" IS NULL
  AND NOT EXISTS (
    SELECT 1 FROM "AspNetUserRoles" ur
    WHERE ur."UserId" = u."Id" AND ur."RoleId" = r."Id"
  )
  AND u."Id" = (SELECT MIN("Id") FROM "AspNetUsers" WHERE "MembershipId" IS NULL AND "CompanyId" IS NULL);

-- Sonuç: Kaç kullanıcıya rol atandığını görmek için
SELECT u."UserName", r."Name" AS "Role"
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON ur."UserId" = u."Id"
JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
WHERE u."MembershipId" IS NULL AND u."CompanyId" IS NULL;
