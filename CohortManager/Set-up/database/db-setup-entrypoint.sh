#!/bin/bash

# This script runs when the db-setup container is run

# Wait for db container to start
sleep 10s

# Change the password after the -P with the one defined in compose.yaml

# Set up database
/opt/mssql-tools/bin/sqlcmd -S 127.0.0.1 -U SA -P "psswd goes here" -i create_database_statement.sql
/opt/mssql-tools/bin/sqlcmd -S 127.0.0.1 -U SA -P "psswd goes here" -i create_statement.sql
/opt/mssql-tools/bin/sqlcmd -S 127.0.0.1 -U SA -P "psswd goes here" -i insert_statement.sql
