SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: albums; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.albums (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying NOT NULL,
    year integer,
    from_path boolean NOT NULL
);


--
-- Name: albums_cover_art; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.albums_cover_art (
    album_id uuid NOT NULL,
    cover_art_id uuid NOT NULL
);


--
-- Name: albums_genres; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.albums_genres (
    genre_id uuid NOT NULL,
    album_id uuid NOT NULL
);


--
-- Name: albums_users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.albums_users (
    album_id uuid NOT NULL,
    user_id uuid NOT NULL,
    starred timestamp without time zone,
    last_played timestamp without time zone,
    play_count bigint,
    rating integer,
    CONSTRAINT albums_users_rating_check CHECK (((rating >= 1) AND (rating <= 5)))
);


--
-- Name: artists; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.artists (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying NOT NULL,
    image_url character varying,
    from_path boolean NOT NULL
);


--
-- Name: artists_albums; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.artists_albums (
    artist_id uuid NOT NULL,
    album_id uuid NOT NULL
);


--
-- Name: artists_users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.artists_users (
    artist_id uuid NOT NULL,
    user_id uuid NOT NULL,
    starred timestamp without time zone,
    last_played timestamp without time zone,
    rating integer,
    CONSTRAINT artists_users_rating_check CHECK (((rating >= 1) AND (rating <= 5)))
);


--
-- Name: cover_art; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.cover_art (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    mime character varying NOT NULL,
    image bytea,
    path character varying,
    hash character varying NOT NULL,
    created timestamp without time zone NOT NULL
);


--
-- Name: directory_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.directory_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    parent_id uuid,
    music_folder_id uuid,
    name character varying,
    is_dir boolean NOT NULL,
    track integer,
    year integer,
    size bigint,
    content_type character varying,
    suffix character varying,
    duration integer,
    bit_rate integer,
    path character varying NOT NULL,
    is_video boolean,
    disc_number integer,
    created timestamp without time zone NOT NULL,
    type character varying
);


--
-- Name: genres; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.genres (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying NOT NULL
);


--
-- Name: items_albums; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.items_albums (
    item_id uuid NOT NULL,
    album_id uuid NOT NULL
);


--
-- Name: items_artists; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.items_artists (
    item_id uuid NOT NULL,
    artist_id uuid NOT NULL
);


--
-- Name: items_users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.items_users (
    item_id uuid NOT NULL,
    user_id uuid NOT NULL,
    starred timestamp without time zone,
    last_played timestamp without time zone,
    bookmark_pos bigint,
    play_count bigint,
    rating integer,
    CONSTRAINT items_users_rating_check CHECK (((rating >= 1) AND (rating <= 5)))
);


--
-- Name: library_roots; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.library_roots (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying NOT NULL,
    path character varying NOT NULL,
    initial_scan timestamp without time zone,
    is_scanning boolean DEFAULT false NOT NULL
);


--
-- Name: schema_migrations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.schema_migrations (
    version character varying(128) NOT NULL
);


--
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    username character varying NOT NULL,
    password character varying NOT NULL,
    scrobbling boolean DEFAULT true NOT NULL,
    admin_role boolean DEFAULT false NOT NULL,
    settings_role boolean DEFAULT false NOT NULL,
    download_role boolean DEFAULT true NOT NULL,
    upload_role boolean DEFAULT false NOT NULL,
    playlist_role boolean DEFAULT true NOT NULL,
    cover_art_role boolean DEFAULT true NOT NULL,
    podcast_role boolean DEFAULT true NOT NULL,
    comment_role boolean DEFAULT true NOT NULL,
    stream_role boolean DEFAULT true NOT NULL,
    jukebox_role boolean DEFAULT false NOT NULL,
    share_role boolean DEFAULT true NOT NULL,
    video_conversion_role boolean DEFAULT true NOT NULL,
    max_bit_rate integer,
    avatar_last_changed timestamp without time zone
);


--
-- Name: albums_cover_art albums_cover_art_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums_cover_art
    ADD CONSTRAINT albums_cover_art_pkey PRIMARY KEY (album_id, cover_art_id);


--
-- Name: albums_genres albums_genres_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums_genres
    ADD CONSTRAINT albums_genres_pkey PRIMARY KEY (album_id, genre_id);


--
-- Name: albums albums_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums
    ADD CONSTRAINT albums_pkey PRIMARY KEY (id);


--
-- Name: albums_users albums_users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums_users
    ADD CONSTRAINT albums_users_pkey PRIMARY KEY (album_id, user_id);


--
-- Name: artists_albums artists_albums_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.artists_albums
    ADD CONSTRAINT artists_albums_pkey PRIMARY KEY (artist_id, album_id);


--
-- Name: artists artists_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.artists
    ADD CONSTRAINT artists_pkey PRIMARY KEY (id);


--
-- Name: artists_users artists_users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.artists_users
    ADD CONSTRAINT artists_users_pkey PRIMARY KEY (artist_id, user_id);


--
-- Name: cover_art cover_art_hash_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cover_art
    ADD CONSTRAINT cover_art_hash_key UNIQUE (hash);


--
-- Name: cover_art cover_art_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cover_art
    ADD CONSTRAINT cover_art_pkey PRIMARY KEY (id);


--
-- Name: directory_items directory_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.directory_items
    ADD CONSTRAINT directory_items_pkey PRIMARY KEY (id);


--
-- Name: genres genres_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.genres
    ADD CONSTRAINT genres_pkey PRIMARY KEY (id);


--
-- Name: items_albums items_albums_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.items_albums
    ADD CONSTRAINT items_albums_pkey PRIMARY KEY (item_id, album_id);


--
-- Name: items_artists items_artists_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.items_artists
    ADD CONSTRAINT items_artists_pkey PRIMARY KEY (item_id, artist_id);


--
-- Name: items_users items_users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.items_users
    ADD CONSTRAINT items_users_pkey PRIMARY KEY (item_id, user_id);


--
-- Name: library_roots library_roots_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.library_roots
    ADD CONSTRAINT library_roots_pkey PRIMARY KEY (id);


--
-- Name: schema_migrations schema_migrations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.schema_migrations
    ADD CONSTRAINT schema_migrations_pkey PRIMARY KEY (version);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- Name: albums_name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX albums_name ON public.albums USING btree (name);


--
-- Name: albums_users_last_played; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX albums_users_last_played ON public.albums_users USING btree (album_id, user_id, last_played);


--
-- Name: artists_name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX artists_name ON public.artists USING btree (name);


--
-- Name: directory_items_parent; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX directory_items_parent ON public.directory_items USING btree (parent_id);


--
-- Name: directory_items_path; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX directory_items_path ON public.directory_items USING btree (path);


--
-- Name: directory_name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX directory_name ON public.directory_items USING btree (name);


--
-- Name: genres_name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX genres_name ON public.genres USING btree (name);


--
-- Name: items_created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX items_created ON public.directory_items USING btree (created);


--
-- Name: items_users_last_played; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX items_users_last_played ON public.items_users USING btree (item_id, user_id, last_played);


--
-- Name: users_username; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX users_username ON public.users USING btree (username);


--
-- Name: albums_cover_art albums_cover_art_album_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums_cover_art
    ADD CONSTRAINT albums_cover_art_album_id_fkey FOREIGN KEY (album_id) REFERENCES public.albums(id) ON DELETE CASCADE;


--
-- Name: albums_cover_art albums_cover_art_cover_art_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums_cover_art
    ADD CONSTRAINT albums_cover_art_cover_art_id_fkey FOREIGN KEY (cover_art_id) REFERENCES public.cover_art(id) ON DELETE CASCADE;


--
-- Name: albums_genres albums_genres_album_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums_genres
    ADD CONSTRAINT albums_genres_album_id_fkey FOREIGN KEY (album_id) REFERENCES public.albums(id) ON DELETE CASCADE;


--
-- Name: albums_genres albums_genres_genre_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums_genres
    ADD CONSTRAINT albums_genres_genre_id_fkey FOREIGN KEY (genre_id) REFERENCES public.genres(id) ON DELETE CASCADE;


--
-- Name: albums_users albums_users_album_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums_users
    ADD CONSTRAINT albums_users_album_id_fkey FOREIGN KEY (album_id) REFERENCES public.albums(id) ON DELETE CASCADE;


--
-- Name: albums_users albums_users_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.albums_users
    ADD CONSTRAINT albums_users_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: artists_albums artists_albums_album_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.artists_albums
    ADD CONSTRAINT artists_albums_album_id_fkey FOREIGN KEY (album_id) REFERENCES public.albums(id) ON DELETE CASCADE;


--
-- Name: artists_albums artists_albums_artist_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.artists_albums
    ADD CONSTRAINT artists_albums_artist_id_fkey FOREIGN KEY (artist_id) REFERENCES public.artists(id) ON DELETE CASCADE;


--
-- Name: artists_users artists_users_artist_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.artists_users
    ADD CONSTRAINT artists_users_artist_id_fkey FOREIGN KEY (artist_id) REFERENCES public.artists(id) ON DELETE CASCADE;


--
-- Name: artists_users artists_users_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.artists_users
    ADD CONSTRAINT artists_users_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: directory_items directory_items_music_folder_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.directory_items
    ADD CONSTRAINT directory_items_music_folder_id_fkey FOREIGN KEY (music_folder_id) REFERENCES public.library_roots(id) ON DELETE CASCADE;


--
-- Name: directory_items directory_items_parent_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.directory_items
    ADD CONSTRAINT directory_items_parent_id_fkey FOREIGN KEY (parent_id) REFERENCES public.directory_items(id) ON DELETE CASCADE;


--
-- Name: items_albums items_albums_album_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.items_albums
    ADD CONSTRAINT items_albums_album_id_fkey FOREIGN KEY (album_id) REFERENCES public.albums(id) ON DELETE CASCADE;


--
-- Name: items_albums items_albums_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.items_albums
    ADD CONSTRAINT items_albums_item_id_fkey FOREIGN KEY (item_id) REFERENCES public.directory_items(id) ON DELETE CASCADE;


--
-- Name: items_artists items_artists_artist_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.items_artists
    ADD CONSTRAINT items_artists_artist_id_fkey FOREIGN KEY (artist_id) REFERENCES public.artists(id) ON DELETE CASCADE;


--
-- Name: items_artists items_artists_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.items_artists
    ADD CONSTRAINT items_artists_item_id_fkey FOREIGN KEY (item_id) REFERENCES public.directory_items(id) ON DELETE CASCADE;


--
-- Name: items_users items_users_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.items_users
    ADD CONSTRAINT items_users_item_id_fkey FOREIGN KEY (item_id) REFERENCES public.directory_items(id) ON DELETE CASCADE;


--
-- Name: items_users items_users_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.items_users
    ADD CONSTRAINT items_users_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--


--
-- Dbmate schema migrations
--

INSERT INTO public.schema_migrations (version) VALUES
    ('20230524062112'),
    ('20230525201324'),
    ('20230611034124'),
    ('20230611180627');
