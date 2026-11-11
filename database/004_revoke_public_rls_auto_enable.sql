-- Security hardening: this privileged event-trigger helper must not be callable via the public Data API.
REVOKE EXECUTE ON FUNCTION public.rls_auto_enable() FROM PUBLIC, anon, authenticated;
