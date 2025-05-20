FROM mcr.microsoft.com/mssql-tools

# Copy all SQL files and db-setup-entrypoint.sh file from current directory
COPY *.sql .
COPY db-setup-entrypoint.sh .

# Create a non-root user and set permissions
RUN adduser --disabled-password --gecos '' nonrootuser && \
    chown nonrootuser:nonrootuser ./*.sql ./db-setup-entrypoint.sh

# Switch to non-root user
USER nonrootuser

# Make the entrypoint script executable.
RUN chmod +x db-setup-entrypoint.sh

ENTRYPOINT ["./db-setup-entrypoint.sh"]
