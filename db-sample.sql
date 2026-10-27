INSERT INTO composition (title) VALUES ('An der Elbe');
INSERT INTO composition (title) VALUES ('Libertas Overture');
INSERT INTO composition (title) VALUES ('Abschied der Gladiatoren');

WITH v AS (INSERT INTO voice (file, composition_id, print_config_id) VALUES (pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Bb Klarinette 1.pdf'), 1, 'a4_to_booklet') RETURNING id)
    INSERT INTO voice_to_voice_definition (voice_id, definition_id) SELECT id, 8 FROM v;
WITH v AS (INSERT INTO voice (file, composition_id, print_config_id) VALUES (pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Bb Klarinette 2.pdf'), 1, 'a4_to_a3_duplex') RETURNING id)
    INSERT INTO voice_to_voice_definition (voice_id, definition_id) SELECT id, 9 FROM v;
WITH v AS (INSERT INTO voice (file, composition_id, print_config_id) VALUES (pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Bb Klarinette 3.pdf'), 1, 'a4_to_a3_duplex') RETURNING id)
    INSERT INTO voice_to_voice_definition (voice_id, definition_id) SELECT id, 10 FROM v;
WITH v AS (INSERT INTO voice (file, composition_id, print_config_id) VALUES (pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Posaune 1.pdf'), 1, 'duplex') RETURNING id)
    INSERT INTO voice_to_voice_definition (voice_id, definition_id) SELECT id, 30 FROM v;

INSERT INTO event (name, is_active) VALUES ('2026-05 Frühjahrskonzert', false), ('2026-10 Konzertwertung', true);
INSERT INTO composition_to_event (composition_id, event_id) VALUES (1, 1), (3, 1), (1, 2), (2, 2);
