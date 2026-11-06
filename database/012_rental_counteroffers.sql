-- Structured counter-offers remain private to a rental's participants.
ALTER TABLE rentall."PedidosAluguer"
    ADD COLUMN "PropostaDias" integer,
    ADD COLUMN "PropostaPrecoDia" numeric(12,2),
    ADD COLUMN "PropostaPorId" text,
    ADD COLUMN "PropostaEm" timestamptz;

ALTER TABLE rentall."PedidosAluguer"
    DROP CONSTRAINT IF EXISTS "PedidosAluguer_Estado_check";
ALTER TABLE rentall."PedidosAluguer"
    ADD CONSTRAINT "PedidosAluguer_Estado_check"
    CHECK ("Estado" IN ('Pendente','Contraproposta','Aceite sem pagamento','Recusado','Cancelado'));

ALTER TABLE rentall."PedidosAluguer"
    ADD CONSTRAINT "CK_Pedido_Proposta"
    CHECK ("Estado" <> 'Contraproposta' OR
       ("PropostaDias" BETWEEN 1 AND 365
        AND "PropostaPrecoDia" >= 0.50
        AND "PropostaPrecoDia" <= 10000
        AND "PropostaPorId" IN ("InteressadoId","AnuncianteId")
        AND "PropostaEm" IS NOT NULL));

DROP INDEX IF EXISTS rentall."UX_Pedidos_Interessado_Pendente";
CREATE UNIQUE INDEX "UX_Pedidos_Interessado_Ativo"
ON rentall."PedidosAluguer" ("AnuncioId", "InteressadoId")
WHERE "Estado" IN ('Pendente','Contraproposta');

-- Only the RentAll backend can query or update the private negotiations.
REVOKE ALL ON rentall."PedidosAluguer" FROM PUBLIC, anon, authenticated;
GRANT SELECT,INSERT,UPDATE ON rentall."PedidosAluguer" TO rentall_app;
