INSERT INTO composition (title, is_active) VALUES ('An der Elbe', true);
INSERT INTO composition (title, is_active) VALUES ('Libertas Overture', true);
INSERT INTO composition (title, is_active) VALUES ('Abschied der Gladiatoren', false);

WITH v AS (INSERT INTO voice (file, composition_id, print_config_id) VALUES (pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Bb Klarinette 1.pdf'), 1, 'a4_to_booklet') RETURNING id)
    INSERT INTO voice_to_voice_definition (voice_id, definition_id) SELECT id, 8 FROM v;
WITH v AS (INSERT INTO voice (file, composition_id, print_config_id) VALUES (pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Bb Klarinette 2.pdf'), 1, 'a4_to_a3_duplex') RETURNING id)
    INSERT INTO voice_to_voice_definition (voice_id, definition_id) SELECT id, 9 FROM v;
WITH v AS (INSERT INTO voice (file, composition_id, print_config_id) VALUES (pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Bb Klarinette 3.pdf'), 1, 'a4_to_a3_duplex') RETURNING id)
    INSERT INTO voice_to_voice_definition (voice_id, definition_id) SELECT id, 10 FROM v;
WITH v AS (INSERT INTO voice (file, composition_id, print_config_id) VALUES (pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Posaune 1.pdf'), 1, 'duplex') RETURNING id)
    INSERT INTO voice_to_voice_definition (voice_id, definition_id) SELECT id, 30 FROM v;
