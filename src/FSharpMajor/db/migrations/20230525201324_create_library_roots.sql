-- migrate:up
CREATE TABLE library_roots (
    id              uuid DEFAULT gen_random_uuid() PRIMARY KEY,
    name            varchar NOT NULL,
    path            varchar NOT NULL,
    initial_scan    timestamp,
    is_scanning     bool DEFAULT false NOT NULL
);

-- migrate:down
DROP TABLE IF EXISTS library_roots;
