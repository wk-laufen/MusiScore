DROP TABLE IF EXISTS voice_print;
DROP TABLE IF EXISTS voice;
DROP TABLE IF EXISTS voice_print_setting;
DROP TABLE IF EXISTS composition;

CREATE TABLE composition(
    id SERIAL PRIMARY KEY NOT NULL,
    title VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL
);

CREATE TABLE voice_print_setting(
    "key" VARCHAR(50) PRIMARY KEY NOT NULL,
    name VARCHAR(50) NOT NULL
);
INSERT INTO voice_print_setting ("key", name) VALUES ('duplex', 'Doppelseitig');
INSERT INTO voice_print_setting ("key", name) VALUES ('a4_to_a3_duplex', 'A4 -> A3 doppelseitig');
INSERT INTO voice_print_setting ("key", name) VALUES ('a4_to_booklet', 'A4 -> Broschüre');

CREATE TABLE voice(
    id SERIAL PRIMARY KEY NOT NULL,
    name VARCHAR(50) NOT NULL,
    file BYTEA NOT NULL,
    composition_id INT NOT NULL,
    print_setting_id VARCHAR(50) NOT NULL,
    FOREIGN KEY(composition_id) REFERENCES composition(id) ON DELETE CASCADE,
    FOREIGN KEY(print_setting_id) REFERENCES voice_print_setting("key")
);

CREATE TABLE voice_print(
    id SERIAL PRIMARY KEY NOT NULL,
    timestamp TIMESTAMP NOT NULL,
    count INT NOT NULL,
    voice_id INT NOT NULL,
    FOREIGN KEY(voice_id) REFERENCES voice(id) ON DELETE CASCADE
);

-- create custom print configs
ALTER TABLE voice_print_setting RENAME TO voice_print_config;
ALTER TABLE voice_print_config
    ADD COLUMN reorder_pages_as_booklet BOOLEAN,
    ADD COLUMN cups_command_line_args VARCHAR(256),
    ADD COLUMN sort_order INT;
ALTER TABLE voice RENAME COLUMN print_setting_id TO print_config_id;
UPDATE voice_print_config SET reorder_pages_as_booklet=FALSE, cups_command_line_args='-o media=A4 -o sides=two-sided-long-edge', sort_order=1 WHERE "key"='duplex';
UPDATE voice_print_config SET reorder_pages_as_booklet=FALSE, cups_command_line_args='-o number-up=2 -o media=A3 -o sides=two-sided-short-edge', sort_order=2 WHERE "key"='a4_to_a3_duplex';
UPDATE voice_print_config SET reorder_pages_as_booklet=TRUE, cups_command_line_args='-o number-up=2 -o media=A3 -o sides=two-sided-short-edge', sort_order=3 WHERE "key"='a4_to_booklet';
ALTER TABLE voice_print_config
    ALTER COLUMN reorder_pages_as_booklet SET NOT NULL,
    ALTER COLUMN cups_command_line_args SET NOT NULL,
    ALTER COLUMN sort_order SET NOT NULL;

-- add composer and arranger
ALTER TABLE composition
    ADD COLUMN composer VARCHAR,
    ADD COLUMN arranger VARCHAR;

-- use tags for composition attributes
CREATE TABLE composition_tag_type (
    "key" VARCHAR(50) PRIMARY KEY NOT NULL,
    name VARCHAR NOT NULL,
    settings JSONB NOT NULL
);
CREATE TABLE composition_tag (
    composition_id INT NOT NULL,
    tag_type VARCHAR(50) NOT NULL,
    value VARCHAR,
    PRIMARY KEY (composition_id, tag_type),
    FOREIGN KEY (composition_id) REFERENCES composition(id) ON DELETE CASCADE,
    FOREIGN KEY (tag_type) REFERENCES composition_tag_type("key") ON DELETE CASCADE
);
INSERT INTO composition_tag_type ("key", name, settings) VALUES
    ('composer', 'Komponist', '{ "overview_display_format": { "order": 1, "format": "%s" } }'::jsonb),
    ('arranger', 'Arrangeur', '{ "overview_display_format": { "order": 2, "format": "arr. %s" } }'::jsonb),
    ('category', 'Kategorie', '{}'::jsonb),
    ('difficultyLevel', 'Schwierigkeitsgrad', '{}'::jsonb),
    ('notes', 'Notizen', '{}'::jsonb);
INSERT INTO composition_tag (composition_id, tag_type, value) SELECT id, 'composer', composer FROM composition WHERE composer IS NOT NULL;
INSERT INTO composition_tag (composition_id, tag_type, value) SELECT id, 'arranger', arranger FROM composition WHERE arranger IS NOT NULL;
ALTER TABLE composition DROP COLUMN composer, DROP COLUMN arranger;
