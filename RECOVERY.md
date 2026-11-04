# RentAll recovery — deployment notes

This work is isolated on branch `recovery/supabase-render`. The `Master` branch remains untouched.

## Implemented
- ASP.NET Core upgraded to .NET 10 with PostgreSQL Npgsql provider.
- Legacy SQL Server migrations excluded from compilation.
- PostgreSQL application schema `rentall` created on Supabase with 15 tables and six category seeds. Schema DDL is kept in `database/001_rentall_bootstrap.sql`.
- Data Protection keys persisted to `rentall."DataProtectionKeys"`.
- Item photo upload moved to Supabase Storage bucket `rentall-images` (max 5MB, JPEG/PNG/WebP). Old Razor `/imagens/...` links are forwarded through the ImagesController.
- Item operations require login and ownership checks. Announcement owner/foreign keys are not taken from form input.
- Dockerfile and Render blueprint `render.yaml` added; check `/healthz` for database readiness.
- CI workflow builds .NET application and Docker image.
- Removed exposed credentials from current `appsettings.json`.

## Before deploying publicly
1. Configure **only in Render private environment settings**:
   - `ConnectionStrings__ApplicationDbContextConnection`: Npgsql string obtained from Supabase > Connect > Session pooler (IPv4 compatible). Host, port 5432, username, password, DB=postgres, SSL Mode=Require.
   - `Supabase__Url=https://uyfaxvsedxljcztwveqy.supabase.co`
   - `Supabase__ServiceKey`: Supabase service_role secret used only by the ASP.NET backend (never put it in source code, logs or frontend).
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ASPNETCORE_URLS=http://0.0.0.0:10000`
2. Select Render **Free**, **Docker**, Frankfurt, branch `recovery/supabase-render`. Deploy only once the .NET and Docker CI jobs pass.
3. ASP.NET Core Identity is the login system (not Supabase Auth). The Supabase `rentall` schema is *not* exposed via the Data API. `anon` and `authenticated` roles have no schema usage or table access.
4. Email verification and password-reset mail need a working email provider. New registration without SendGrid is permitted for testing, but password-reset-by-email will fail until configured.
5. Rotate any legacy Azure SQL credentials, Google OAuth client secrets and other secrets stored in historical Git commits. Changing the latest `appsettings.json` does not remove old history.
6. Supabase warns that RLS is off for 15 application tables. We checked `anon` and `authenticated` cannot access `rentall` or the identity table. Nonetheless review RLS design before considering exposure through Supabase API. Never indiscriminately grant public table rights.
7. Supabase security advisor additionally warns about `public.rls_auto_enable()` being executable by `anon` and `authenticated`. Review that function separately rather than changing it blindly.
8. Confirm ownership and validation on all create/edit/delete operations, empty-state behavior, image upload and deletion, password recovery, and login after service restarts.

## Limitations
- Old users, proposals, ads and images cannot be recovered without backups.
- Render Free services sleep during inactivity; Supabase Free can pause an inactive project.
- The current storage bucket is public for advertisement images; do not upload private documents.
- Security review, runtime integration tests and deployment are still required.
