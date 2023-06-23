-- migrate:up
CREATE TABLE library_roots (
    id              uuid DEFAULT gen_random_uuid() PRIMARY KEY,
    name            varchar NOT NULL,
    path            varchar NOT NULL,
    scan_completed  timestamp
);

-- migrate:down
DROP TABLE IF EXISTS library_roots;
