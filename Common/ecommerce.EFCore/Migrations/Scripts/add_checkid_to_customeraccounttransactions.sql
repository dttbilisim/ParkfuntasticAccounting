-- CustomerAccountTransactions tablosuna CheckId kolonunu ekler.
-- Sadece kolon yoksa çalıştırın (migration boş uygulandıysa veya farklı DB'de kaldıysa).
-- PostgreSQL

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'CustomerAccountTransactions' AND column_name = 'CheckId'
  ) THEN
    ALTER TABLE "CustomerAccountTransactions" ADD COLUMN "CheckId" integer NULL;
    CREATE INDEX "IX_CustomerAccountTransactions_CheckId" ON "CustomerAccountTransactions" ("CheckId");
    ALTER TABLE "CustomerAccountTransactions"
      ADD CONSTRAINT "FK_CustomerAccountTransactions_Checks_CheckId"
      FOREIGN KEY ("CheckId") REFERENCES "Checks" ("Id");
  END IF;
END $$;
