INSERT INTO composition (id, title, is_active) VALUES (1, 'An der Elbe', true);
INSERT INTO composition (id, title, is_active) VALUES (2, 'Libertas Overture', true);
INSERT INTO composition (id, title, is_active) VALUES (3, 'Abschied der Gladiatoren', false);

INSERT INTO voice (id, name, file, composition_id, print_setting_id) VALUES (1, 'Bb Klarinette 1', LOAD_FILE('/var/lib/mysql-files/data/An der Elbe/An der Elbe (C) - Bb Klarinette 1.pdf'), 1, 'a4_to_booklet');
INSERT INTO voice (id, name, file, composition_id, print_setting_id) VALUES (2, 'Bb Klarinette 2', LOAD_FILE('/var/lib/mysql-files/data/An der Elbe/An der Elbe (C) - Bb Klarinette 2.pdf'), 1, 'a4_to_a3_duplex');
INSERT INTO voice (id, name, file, composition_id, print_setting_id) VALUES (3, 'Bb Klarinette 3', LOAD_FILE('/var/lib/mysql-files/data/An der Elbe/An der Elbe (C) - Bb Klarinette 3.pdf'), 1, 'a4_to_a3_duplex');
INSERT INTO voice (id, name, file, composition_id, print_setting_id) VALUES (4, 'Posaune 1', LOAD_FILE('/var/lib/mysql-files/data/An der Elbe/An der Elbe (C) - Posaune 1.pdf'), 1, 'duplex');
