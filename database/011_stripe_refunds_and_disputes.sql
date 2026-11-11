-- Sandbox payment state reconciliation. Never exposes payment records via Supabase Data API.
ALTER TABLE rentall."PagamentosAluguer"
  ADD COLUMN IF NOT EXISTS "ReembolsadoCentimos" bigint NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "DisputaAberta" boolean NOT NULL DEFAULT false;
ALTER TABLE rentall."PagamentosAluguer"
  ADD CONSTRAINT "CK_Pagamentos_Reembolsado" CHECK
  ("ReembolsadoCentimos" >= 0 AND "ReembolsadoCentimos" <= "TotalCentimos");
ALTER TABLE rentall."PagamentosAluguer"
  DROP CONSTRAINT IF EXISTS "PagamentosAluguer_Estado_check";
ALTER TABLE rentall."PagamentosAluguer"
  ADD CONSTRAINT "PagamentosAluguer_Estado_check"
  CHECK("Estado" IN ('Checkout criado','Pago em teste','Expirado','Falhado',
                    'Reembolso parcial','Reembolsado em teste',
                    'Disputa aberta','Disputa encerrada'));
REVOKE ALL ON rentall."PagamentosAluguer" FROM PUBLIC,anon,authenticated;
GRANT SELECT,INSERT,UPDATE ON rentall."PagamentosAluguer" TO rentall_app;
