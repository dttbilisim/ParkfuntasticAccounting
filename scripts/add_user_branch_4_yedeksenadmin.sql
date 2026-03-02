-- yedeksenadmin kullanıcısına BranchId 4 yetkisi ekler (faturalar BranchId 1 ve 4 ile kayıtlı; mevcut kayıtlar 1 ve 3)
-- Çalıştırmadan önce: Bu kullanıcının zaten BranchId 4 kaydı yoksa eklenir.

INSERT INTO "UserBranches" ("UserId", "BranchId", "IsDefault", "Status", "CreatedDate", "CreatedId")
SELECT u."Id", 4, false, 1, NOW(), u."Id"
FROM "AspNetUsers" u
WHERE (u."UserName" = 'yedeksenadmin' OR u."NormalizedUserName" = 'YEDEKSENADMIN')
  AND NOT EXISTS (
    SELECT 1 FROM "UserBranches" ub
    WHERE ub."UserId" = u."Id" AND ub."BranchId" = 4 AND ub."Status" = 1
  );

-- Kaç satır eklendiğini görmek için (isteğe bağlı):
-- SELECT * FROM "UserBranches" ub
-- JOIN "AspNetUsers" u ON u."Id" = ub."UserId"
-- WHERE u."UserName" = 'yedeksenadmin';
