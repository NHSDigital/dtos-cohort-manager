#!/bin/bash

# This script runs when the db-setup container is run

# Wait for db container to start
sleep 10s

# Set up database
/opt/mssql-tools/bin/sqlcmd -S 127.0.0.1 -U SA -P "${PASSWORD}" -i create_database_statement.sql
/opt/mssql-tools/bin/sqlcmd -S 127.0.0.1 -U SA -P "${PASSWORD}" -d ${DB_NAME} -i create_statement.sql
/opt/mssql-tools/bin/sqlcmd -S 127.0.0.1 -U SA -P "${PASSWORD}" -d ${DB_NAME} -i insert_statement.sql
