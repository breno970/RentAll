-- RentAll showcase workflow: deliberately separate from Stripe/financial ledger.
-- Additive migration: never changes old requests, real payments or Identity accounts.
CREATE TABLE IF NOT EXISTS rentall."PercursosAluguerDemo" (
  "PedidoId" integer PRIMARY KEY REFERENCES rentall."PedidosAluguer"("Id") ON DELETE CASCADE,
  "Estado" varchar(32) NOT NULL DEFAULT 'Checkout'
    CHECK ("Estado" IN ('Checkout','PagamentoRecusado','PagamentoSimulado',
      'EntregaProposta','EntregaAcordada','EmCurso','DevolucaoPendente',
      'Concluido','Cancelado')),
  "MetodoPagamento" varchar(16) NULL CHECK
    ("MetodoPagamento" IS NULL OR "MetodoPagamento" IN ('cartao','mbway')),
  "PagamentoEm" timestamptz NULL,
  "EntregaTipo" varchar(32) NULL CHECK
    ("EntregaTipo" IS NULL OR "EntregaTipo" IN ('em_maos','ponto_recolha','transportadora')),
  "EntregaLocal" varchar(120) NULL,
  "EntregaEm" timestamptz NULL,
  "AcordadaEm" timestamptz NULL,
  "EntreguePeloAnuncianteEm" timestamptz NULL,
  "RecebidaPeloLocatarioEm" timestamptz NULL,
  "DevolvidaPeloLocatarioEm" timestamptz NULL,
  "RecebidaPeloAnuncianteEm" timestamptz NULL,
  "IncidenteAberto" boolean NOT NULL DEFAULT false,
  "Avaliacao" integer NULL CHECK ("Avaliacao" BETWEEN 1 AND 5),
  "CriadoEm" timestamptz NOT NULL DEFAULT now(),
  "AtualizadoEm" timestamptz NOT NULL DEFAULT now(),
  "Versao" bigint NOT NULL DEFAULT 0
);
-- This schema is not exposed to Supabase Data API. Retain defence in depth.
REVOKE ALL ON rentall."PercursosAluguerDemo" FROM PUBLIC, anon, authenticated;
ALTER TABLE rentall."PercursosAluguerDemo" ENABLE ROW LEVEL SECURITY;
GRANT SELECT, INSERT, UPDATE ON rentall."PercursosAluguerDemo" TO rentall_app;
CREATE POLICY "PercursosAluguerDemo_backend_only"
    ON rentall."PercursosAluguerDemo"
    FOR ALL TO rentall_app
    USING (true) WITH CHECK (true);
-- Identity is ASP.NET Core Identity, not Supabase Auth. The ASP.NET controller
-- verifies both participant IDs against the authenticated claim on each request.
