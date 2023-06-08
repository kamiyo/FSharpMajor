-- migrate:up
CREATE TABLE users (
    id              uuid DEFAULT gen_random_uuid() PRIMARY KEY,
    username        varchar(32) NOT NULL,
    password        varchar(255) NOT NULL,
    scrobbling      boolean DEFAULT true,
    admin_role      boolean DEFAULT false,
    settings_role   boolean DEFAULT false,
    download_role   boolean DEFAULT true,
    upload_role     boolean DEFAULT false,
    playlist_role   boolean DEFAULT true,
    cover_art_role  boolean DEFAULT true,
    podcast_role    boolean DEFAULT true,
    comment_role    boolean DEFAULT true,
    stream_role     boolean DEFAULT true,
    jukebox_role    boolean DEFAULT false,
    share_role      boolean DEFAULT false,
    video_conversion_role   boolean DEFAULT true,
    max_bit_rate    integer,
    avatar_last_changed timestamp
);

CREATE UNIQUE INDEX IF NOT EXISTS user_idx ON users (username);

-- migrate:down
DROP TABLE users;
DROP INDEX IF EXISTS user_idx;
