-- PaymentType 7 (Cari) PcPos'ta görünmesi için IsPcPos=true yap
-- Transfer sorgusu IsPcPos=true olan PaymentType'lara bağlı BankAccount'ları Pos olarak gönderir.
-- Bu script çalıştırıldıktan sonra PcPos'ta transfer/sync yapın.

UPDATE "PaymentTypes"
SET "IsPcPos" = true
WHERE "Id" = 7;

-- Kontrol: Cari Hesap Kasası (BankAccount 5) PaymentTypeId=7 ve Active=true olmalı
-- SELECT ba."Id", ba."AccountName", ba."PaymentTypeId", ba."Active", pt."Name", pt."IsPcPos"
-- FROM "BankAccounts" ba
-- LEFT JOIN "PaymentTypes" pt ON pt."Id" = ba."PaymentTypeId"
-- WHERE ba."Id" = 5;
