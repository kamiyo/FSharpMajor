-- migrate:up
CREATE TABLE users (
    id              uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    username        varchar(32) NOT NULL,
    password        bytea,
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
    share_role      boolean NOT NULL DEFAULT false,
    video_conversion_role   boolean NOT NULL DEFAULT true,
    max_bit_rate    integer,
    avatar_last_changed timestamp
);

CREATE UNIQUE INDEX IF NOT EXISTS user_idx ON users (username);

-- migrate:down
DROP TABLE users;
DROP INDEX IF EXISTS user_idx;
