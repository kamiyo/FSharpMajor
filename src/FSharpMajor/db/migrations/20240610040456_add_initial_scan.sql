-- migrate:up
ALTER TABLE library_roots
    ADD COLUMN IF NOT EXISTS initial_scan timestamp DEFAULT null,
    ADD COLUMN IF NOT EXISTS is_scanning bool DEFAULT false NOT NULL;

-- migrate:down
ALTER TABLE library_roots
    DROP COLUMN IF EXISTS initial_scan,
    DROP COLUMN IF EXISTS is_scanning;
