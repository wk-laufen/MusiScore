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
    ('composer', 'Komponist', '{ "value_type": "text", "overview_display_format": { "order": 1, "format": "%s" } }'::jsonb),
    ('arranger', 'Arrangeur', '{ "value_type": "text", "overview_display_format": { "order": 2, "format": "arr. %s" } }'::jsonb),
    ('category', 'Kategorie', '{ "value_type": "text" }'::jsonb),
    ('difficultyLevel', 'Schwierigkeitsgrad', '{ "value_type": "text" }'::jsonb),
    ('notes', 'Notizen', '{ "value_type": "multi-line-text" }'::jsonb);
INSERT INTO composition_tag (composition_id, tag_type, value) SELECT id, 'composer', composer FROM composition WHERE composer IS NOT NULL;
INSERT INTO composition_tag (composition_id, tag_type, value) SELECT id, 'arranger', arranger FROM composition WHERE arranger IS NOT NULL;
ALTER TABLE composition DROP COLUMN composer, DROP COLUMN arranger;

-- add voice settings
CREATE TABLE voice_settings (
    voice_pattern VARCHAR PRIMARY KEY NOT NULL,
    sort_order INT UNIQUE NOT NULL
);
INSERT INTO voice_settings (voice_pattern, sort_order) VALUES
    ('Partitur', 1),
    ('Piccolo', 2),
    ('Flöte \d in C', 3),
    ('Oboe in C', 4),
    ('Fagott in C', 5),
    ('Klarinette in Es', 6),
    ('Klarinette \d in B', 7),
    ('Alt Klarinette', 8),
    ('Bass Klarinette in C', 9),
    ('Alt Sax \d in Es', 10),
    ('Tenor Sax \d in B', 11),
    ('Bariton Sax in Es', 12),
    ('Flügelhorn \d in B', 13),
    ('Trompete \d in B', 14),
    ('Bariton in B', 15),
    ('Tenorhorn \d in B', 16),
    ('Horn \d in F', 17),
    ('Horn \d in Es', 18),
    ('Posaune \d in C', 19),
    ('Posaune \d in B', 20),
    ('Tuba in C', 21),
    ('Tuba in B', 22),
    ('Kontrabass', 23),
    ('Pauken', 24),
    ('Schlagzeug', 25),
    ('Stabspiele', 26),
    ('Percussion 1,2', 27),
    ('Percussion 3', 28),
    ('Klavier', 29),
    ('Gesang', 30),
    ('Sonstige', 31);

-- fix constraint names
ALTER TABLE voice_print_config RENAME CONSTRAINT voice_print_setting_pkey to voice_print_config_pkey;
ALTER TABLE voice RENAME CONSTRAINT voice_print_setting_id_fkey to voice_print_config_id_fkey;

-- use exact voice names
DELETE FROM voice_settings;
ALTER TABLE voice_settings RENAME COLUMN voice_pattern TO name;
ALTER TABLE voice_settings ADD COLUMN allow_public_print BOOLEAN NOT NULL;
INSERT INTO voice_settings (name, sort_order, allow_public_print) VALUES
    ('Partitur', 1, FALSE),
    ('Piccolo', 2, TRUE),
    ('Flöte 1 in C', 3, TRUE),
    ('Flöte 2 in C', 4, TRUE),
    ('Oboe in C', 5, TRUE),
    ('Fagott in C', 6, TRUE),
    ('Klarinette in Es', 7, FALSE),
    ('Klarinette 1 in B', 8, TRUE),
    ('Klarinette 2 in B', 9, TRUE),
    ('Klarinette 3 in B', 10, TRUE),
    ('Alt Klarinette', 11, FALSE),
    ('Bass Klarinette in C', 12, TRUE),
    ('Alt Sax 1 in Es', 13, TRUE),
    ('Alt Sax 2 in Es', 14, TRUE),
    ('Tenor Sax 1 in B', 15, TRUE),
    ('Tenor Sax 2 in B', 16, TRUE),
    ('Bariton Sax in Es', 17, TRUE),
    ('Flügelhorn 1 in B', 18, TRUE),
    ('Flügelhorn 2 in B', 19, TRUE),
    ('Trompete 1 in B', 20, TRUE),
    ('Trompete 2 in B', 21, TRUE),
    ('Trompete 3 in B', 22, TRUE),
    ('Bariton in B', 23, TRUE),
    ('Tenorhorn 1 in B', 24, TRUE),
    ('Tenorhorn 2 in B', 25, TRUE),
    ('Tenorhorn 3 in B', 26, TRUE),
    ('Horn 1 in F', 27, TRUE),
    ('Horn 2 in F', 28, TRUE),
    ('Horn 3 in F', 29, TRUE),
    ('Posaune 1 in C', 30, TRUE),
    ('Posaune 2 in C', 31, TRUE),
    ('Posaune 3 in C', 32, TRUE),
    ('Posaune 1 in B', 33, TRUE),
    ('Posaune 2 in B', 34, TRUE),
    ('Posaune 3 in B', 35, TRUE),
    ('Tuba 1 in C', 36, TRUE),
    ('Tuba 2 in C', 37, TRUE),
    ('Tuba 1 in B', 38, TRUE),
    ('Tuba 2 in B', 39, TRUE),
    ('Tuba in Es', 40, TRUE),
    ('Streichbass', 41, FALSE),
    ('E-Bass', 42, FALSE),
    ('Pauken', 43, TRUE),
    ('Schlagzeug', 44, TRUE),
    ('Stabspiele', 45, TRUE),
    ('Percussion 1', 46, TRUE),
    ('Percussion 2', 47, TRUE),
    ('Percussion 3', 48, TRUE),
    ('Kleine Trommel', 49, TRUE),
    ('Große Trommel', 50, TRUE),
    ('Klavier', 51, FALSE),
    ('Gesang', 52, FALSE),
    ('Sonstige', 53, FALSE);
ALTER TABLE voice ADD CONSTRAINT voice_name_fkey FOREIGN KEY (name) REFERENCES voice_settings(name) ON UPDATE CASCADE;

-- use surrogate PK for voice_settings and rename table to voice_definition
ALTER TABLE voice DROP CONSTRAINT voice_name_fkey;
ALTER TABLE voice_settings DROP CONSTRAINT voice_settings_pkey;

ALTER TABLE voice_settings RENAME TO voice_definition;
ALTER TABLE voice_definition RENAME CONSTRAINT voice_settings_sort_order_key to voice_definition_sort_order_key;

ALTER TABLE voice_definition ADD COLUMN id SERIAL PRIMARY KEY NOT NULL;
ALTER TABLE voice_definition ADD CONSTRAINT voice_definition_name_key UNIQUE (name);

ALTER TABLE voice ADD COLUMN definition_id INT;
UPDATE voice v SET definition_id = (SELECT id FROM voice_definition vd WHERE vd.name = v.name);
ALTER TABLE voice ALTER COLUMN definition_id SET NOT NULL;
ALTER TABLE voice ADD CONSTRAINT voice_definition_id_fkey FOREIGN KEY (definition_id) REFERENCES voice_definition(id);

ALTER TABLE voice DROP COLUMN name;

-- drop unique constraint of voice_definition(sort_order)
ALTER TABLE voice_definition DROP CONSTRAINT voice_definition_sort_order_key;
