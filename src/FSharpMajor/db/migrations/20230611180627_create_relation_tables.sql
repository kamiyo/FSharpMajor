-- migrate:up
CREATE TABLE artists_users (
    artist_id       uuid REFERENCES artists ON DELETE CASCADE,
    user_id         uuid REFERENCES users ON DELETE CASCADE,
    starred         timestamp,
    last_played     timestamp,
    rating          integer CHECK (rating >= 1 AND rating <= 5),
    PRIMARY KEY (artist_id, user_id),
    UNIQUE (artist_id, user_id)
);

CREATE TABLE albums_users (
    album_id        uuid REFERENCES albums ON DELETE CASCADE,
    user_id         uuid REFERENCES users ON DELETE CASCADE,
    starred         timestamp,
    last_played     timestamp,
    play_count      bigint,
    rating          integer CHECK (rating >= 1 AND rating <= 5),
    PRIMARY KEY (album_id, user_id),
    UNIQUE (album_id, user_id)
);

CREATE TABLE artists_albums (
    artist_id       uuid REFERENCES artists ON DELETE CASCADE,
    album_id        uuid REFERENCES albums ON DELETE CASCADE,
    PRIMARY KEY (artist_id, album_id),
    UNIQUE (artist_id, album_id)
);

CREATE TABLE albums_genres (
    genre_id        uuid REFERENCES genres ON DELETE CASCADE,
    album_id        uuid REFERENCES albums ON DELETE CASCADE,
    PRIMARY KEY (album_id, genre_id),
    UNIQUE (album_id, genre_id)
);

CREATE TABLE items_artists (
    item_id         uuid REFERENCES directory_items ON DELETE CASCADE,
    artist_id       uuid REFERENCES artists ON DELETE CASCADE,
    PRIMARY KEY (item_id, artist_id),
    UNIQUE (item_id, artist_id)
);

CREATE TABLE items_albums (
    item_id         uuid REFERENCES directory_items ON DELETE CASCADE,
    album_id        uuid REFERENCES albums ON DELETE CASCADE,
    PRIMARY KEY (item_id, album_id),
    UNIQUE (item_id, album_id)
);

CREATE TABLE items_users (
    item_id         uuid REFERENCES directory_items ON DELETE CASCADE,
    user_id         uuid REFERENCES users ON DELETE CASCADE,
    starred         timestamp,
    last_played     timestamp,
    bookmark_pos    bigint,
    play_count      bigint,
    rating          integer CHECK (rating >= 1 AND rating <= 5),
    PRIMARY KEY (item_id, user_id),
    UNIQUE (item_id, user_id)
);

CREATE TABLE albums_cover_art (
    album_id        uuid REFERENCES albums ON DELETE CASCADE,
    cover_art_id    uuid REFERENCES cover_art ON DELETE CASCADE,
    PRIMARY KEY (album_id, cover_art_id),
    UNIQUE (album_id, cover_art_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS items_users_last_played ON items_users(item_id, user_id, last_played);
CREATE UNIQUE INDEX IF NOT EXISTS albums_users_last_played ON albums_users(album_id, user_id, last_played);

-- migrate:down
DROP INDEX IF EXISTS items_users_last_played;
DROP INDEX IF EXISTS albums_users_last_played;

DROP TABLE IF EXISTS albums_cover_art;
DROP TABLE IF EXISTS items_users;
DROP TABLE IF EXISTS items_albums;
DROP TABLE IF EXISTS items_artists;
DROP TABLE IF EXISTS albums_genres;
DROP TABLE IF EXISTS artists_albums;
DROP TABLE IF EXISTS albums_users;
DROP TABLE IF EXISTS artists_users;
