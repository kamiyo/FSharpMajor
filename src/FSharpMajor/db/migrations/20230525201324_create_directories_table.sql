-- migrate:up
CREATE TABLE directories (
    id      uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    path    varchar(255) NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS directory_idx ON directories (id);
CREATE UNIQUE INDEX IF NOT EXISTS dir_path_idx ON directories (path);

-- migrate:down
DROP TABLE directories;
DROP INDEX IF EXISTS directory_idx;
DROP INDEX IF EXISTS dir_path_idx;
