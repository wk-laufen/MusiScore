INSERT INTO composition (title, is_active) VALUES ('An der Elbe', true);
INSERT INTO composition (title, is_active) VALUES ('Libertas Overture', true);
INSERT INTO composition (title, is_active) VALUES ('Abschied der Gladiatoren', false);

INSERT INTO voice (name, file, composition_id, print_config_id) VALUES ('Bb Klarinette 1', pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Bb Klarinette 1.pdf'), 1, 'a4_to_booklet');
INSERT INTO voice (name, file, composition_id, print_config_id) VALUES ('Bb Klarinette 2', pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Bb Klarinette 2.pdf'), 1, 'a4_to_a3_duplex');
INSERT INTO voice (name, file, composition_id, print_config_id) VALUES ('Bb Klarinette 3', pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Bb Klarinette 3.pdf'), 1, 'a4_to_a3_duplex');
INSERT INTO voice (name, file, composition_id, print_config_id) VALUES ('Posaune 1', pg_read_binary_file('/app/data/ohne-metadaten/An der Elbe/An der Elbe (C) - Posaune 1.pdf'), 1, 'duplex');
