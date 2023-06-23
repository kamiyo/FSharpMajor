-- migrate:up
CREATE TABLE users (
    id              uuid DEFAULT gen_random_uuid() PRIMARY KEY,
    username        varchar NOT NULL,
    password        varchar NOT NULL,
    scrobbling      boolean NOT NULL DEFAULT true,
    admin_role      boolean NOT NULL DEFAULT false,
    settings_role   boolean NOT NULL DEFAULT false,
    download_role   boolean NOT NULL DEFAULT true,
    upload_role     boolean NOT NULL DEFAULT false,
    playlist_role   boolean NOT NULL DEFAULT true,
    cover_art_role  boolean NOT NULL DEFAULT true,
    podcast_role    boolean NOT NULL DEFAULT true,
    comment_role    boolean NOT NULL DEFAULT true,
    stream_role     boolean NOT NULL DEFAULT true,
    jukebox_role    boolean NOT NULL DEFAULT false,
    share_role      boolean NOT NULL DEFAULT true,
    video_conversion_role   boolean NOT NULL DEFAULT true,
    max_bit_rate    integer,
    avatar_last_changed timestamp
);

CREATE UNIQUE INDEX IF NOT EXISTS users_username ON users(username);

-- migrate:down
DROP TABLE IF EXISTS users;
DROP INDEX IF EXISTS users_username;
