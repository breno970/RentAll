-- Fix production permission error 42501 for the server-side messaging feature.
-- The ASP.NET Core backend connects as rentall_app. Do not grant table access to
-- Supabase anon/authenticated roles; messaging authorization is checked in ASP.NET.
GRANT SELECT, INSERT ON TABLE rentall."Mensagens" TO rentall_app;
GRANT USAGE, SELECT ON SEQUENCE rentall."Mensagens_Id_seq" TO rentall_app;
REVOKE ALL ON TABLE rentall."Mensagens" FROM PUBLIC, anon, authenticated;
REVOKE ALL ON SEQUENCE rentall."Mensagens_Id_seq" FROM PUBLIC, anon, authenticated;
