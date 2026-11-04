# RentAll recovery status

Branch: `recovery/supabase-render` (original Master unchanged).

## Changes already started
- Target framework set to .NET 10.
- SQL Server EF provider replaced with Npgsql EF PostgreSQL.
- Database configuration switched to environment variables (the old public credentials must be revoked).
- Google sign-in is enabled only if both environment settings are present.
- Dockerfile added for Render.

## BLOCKERS — do not deploy as production yet
1. Compile and test application in .NET 10; dependency compatibility not verified.
2. Replace old SQL Server-specific migrations with tested PostgreSQL migrations, then create tables. Do not run legacy migrations on Supabase.
3. Move image uploads away from `wwwroot/imagens` into persistent storage (Supabase Storage) and test image URLs.
4. Fix authorization and ownership checks for items and advertisements, including create/edit/delete.
5. Set up email provider and account confirmation; ASP.NET Identity currently requires confirmed accounts.
6. Configure persistent Data Protection keys and durable secrets for container restarts.
7. Revoke compromised historical Google and database credentials; deleting them from current `appsettings.json` does not clean Git history.
8. Test database connection from Render using Supabase's IPv4 session pooler connection information.

## Render environment settings
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://0.0.0.0:10000`
- `ConnectionStrings__ApplicationDbContextConnection` = PostgreSQL connection string from Supabase's Connect panel; store privately.
- `Authentication__Google__ClientId` and `Authentication__Google__ClientSecret` (optional, both required).
- `SendGridKey` (if using the existing email sender).

Old accounts and user data cannot be reconstructed without the old database backup.
