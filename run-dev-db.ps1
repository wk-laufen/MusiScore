docker run --name musiscore-db -d `
    -v "${pwd}/db-schema.sql:/docker-entrypoint-initdb.d/01-db-schema.sql" `
    -v "${pwd}/db-sample.sql:/docker-entrypoint-initdb.d/02-db-sample.sql" `
    -v "${pwd}/data:/app/data" `
    -e POSTGRES_USER=musiscore `
    -e POSTGRES_PASSWORD=test1234 `
    -p 5432:5432 `
    postgres
