SET character_set_client = 'utf8';

DROP TABLE IF EXISTS voice_print;
DROP TABLE IF EXISTS voice;
DROP TABLE IF EXISTS voice_print_setting;
DROP TABLE IF EXISTS composition;

CREATE TABLE composition(
    id INT AUTO_INCREMENT PRIMARY KEY NOT NULL,
    title VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL
);

CREATE TABLE voice_print_setting(
    `key` VARCHAR(50) PRIMARY KEY NOT NULL,
    name VARCHAR(50) NOT NULL
);
INSERT INTO voice_print_setting (`key`, name) VALUES ('duplex', 'Doppelseitig');
INSERT INTO voice_print_setting (`key`, name) VALUES ('a4_to_a3_duplex', 'A4 -> A3 doppelseitig');
INSERT INTO voice_print_setting (`key`, name) VALUES ('a4_to_booklet', 'A4 -> Broschüre');

CREATE TABLE voice(
    id INT AUTO_INCREMENT PRIMARY KEY NOT NULL,
    name VARCHAR(50) NOT NULL,
    file LONGBLOB NOT NULL,
    composition_id INT NOT NULL,
    print_setting_id VARCHAR(50) NOT NULL,
    FOREIGN KEY(composition_id) REFERENCES composition(id) ON DELETE CASCADE,
    FOREIGN KEY(print_setting_id) REFERENCES voice_print_setting(`key`)
);

CREATE TABLE voice_print(
    id INT AUTO_INCREMENT PRIMARY KEY NOT NULL,
    timestamp DATETIME NOT NULL,
    count INT NOT NULL,
    voice_id INT NOT NULL,
    FOREIGN KEY(voice_id) REFERENCES voice(id) ON DELETE CASCADE
);
