-- Guest customers created at availability-request time (no password yet).
ALTER TABLE identity.users
    ADD COLUMN IF NOT EXISTS account_status text NOT NULL DEFAULT 'registered';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'users_account_status_check'
    ) THEN
        ALTER TABLE identity.users
            ADD CONSTRAINT users_account_status_check
            CHECK (account_status IN ('guest', 'registered'));
    END IF;
END $$;
