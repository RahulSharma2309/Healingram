-- Guest profile fields collected at signup and editable on /dashboard?tab=profile.
ALTER TABLE identity.users ADD COLUMN IF NOT EXISTS first_name text;
ALTER TABLE identity.users ADD COLUMN IF NOT EXISTS last_name text;
ALTER TABLE identity.users ADD COLUMN IF NOT EXISTS address text;
