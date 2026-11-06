-- RentAll: add country without losing any existing advertisement.
-- Historical listings are Brazilian; new ones may also belong to Portugal.
ALTER TABLE rentall."Anuncios"
    ADD COLUMN IF NOT EXISTS "Pais" varchar(2) NOT NULL DEFAULT 'BR';
UPDATE rentall."Anuncios"
    SET "Pais" = 'BR'
    WHERE "Pais" IS NULL OR "Pais" NOT IN ('BR','PT');
ALTER TABLE rentall."Anuncios"
    DROP CONSTRAINT IF EXISTS "CK_Anuncios_Pais";
ALTER TABLE rentall."Anuncios"
    ADD CONSTRAINT "CK_Anuncios_Pais" CHECK ("Pais" IN ('BR','PT'));
CREATE INDEX IF NOT EXISTS "IX_Anuncios_Pais_Estado_Cidade"
    ON rentall."Anuncios" ("Pais", "Estado", "Cidade");
