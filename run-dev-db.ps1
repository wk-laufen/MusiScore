docker run --name musiscore-db -d `
    -v "${pwd}/db-schema.sql:/docker-entrypoint-initdb.d/01-db-schema.sql" `
    -v "${pwd}/db-sample.sql:/docker-entrypoint-initdb.d/02-db-sample.sql" `
    -v "${pwd}/data:/var/lib/mysql-files/data" `
    -e MYSQL_ROOT_PASSWORD=root1234 `
    -e MYSQL_DATABASE=musiscore `
    -p 3306:3306 `
    mysql
