-- migrate:up
CREATE TABLE cover_art (
    id              uuid DEFAULT gen_random_uuid() PRIMARY KEY,
    mime            varchar NOT NULL,
    image           bytea,
    path            varchar,
    hash            varchar NOT NULL UNIQUE,
    created         timestamp NOT NULL
);

CREATE TABLE genres (
    id uuid         DEFAULT gen_random_uuid() PRIMARY KEY,
    name            varchar NOT NULL
);

CREATE TABLE artists (
    id              uuid DEFAULT gen_random_uuid() PRIMARY KEY,
    name            varchar NOT NULL,
    image_url       varchar
);

CREATE TABLE albums (
    id              uuid DEFAULT gen_random_uuid() PRIMARY KEY,
    name            varchar NOT NULL,
    year            integer
);

CREATE TABLE directory_items (
    id                  uuid DEFAULT gen_random_uuid() PRIMARY KEY,
    parent_id           uuid REFERENCES directory_items ON DELETE CASCADE,
    music_folder_id     uuid REFERENCES library_roots ON DELETE CASCADE, 
    name                varchar,
    is_dir              boolean NOT NULL,
    track               integer,
    year                integer,
    size                bigint,
    content_type        varchar,
    suffix              varchar,
    duration            integer,
    bit_rate            integer,
    path                varchar NOT NULL,
    is_video            boolean,
    disc_number         integer,
    created             timestamp NOT NULL,
    type                varchar,
    album_from_path     boolean NOT NULL,
    artist_from_path    boolean NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS artists_name ON artists(name);
CREATE UNIQUE INDEX IF NOT EXISTS albums_name ON albums(name);
CREATE UNIQUE INDEX IF NOT EXISTS genres_name ON genres(name);

CREATE UNIQUE INDEX IF NOT EXISTS directory_items_path ON directory_items(path);
CREATE INDEX IF NOT EXISTS directory_items_parent ON directory_items(parent_id);
CREATE INDEX IF NOT EXISTS directory_name ON directory_items(name);
CREATE INDEX IF NOT EXISTS items_created ON directory_items(created);


-- migrate:down
DROP INDEX IF EXISTS items_created;
DROP INDEX IF EXISTS directory_name;
DROP INDEX IF EXISTS directory_items_parent;
DROP INDEX IF EXISTS directory_items_path;

DROP INDEX IF EXISTS genres_name;
DROP INDEX IF EXISTS albums_name;
DROP INDEX IF EXISTS artists_name;

DROP TABLE IF EXISTS directory_items;
DROP TABLE IF EXISTS albums;
DROP TABLE IF EXISTS artists;
DROP TABLE IF EXISTS genres;
DROP TABLE IF EXISTS cover_art;