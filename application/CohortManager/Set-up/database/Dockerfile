FROM mcr.microsoft.com/mssql-tools

# Copy all SQL files (including any added in the future)
COPY *.sql .
COPY db-setup-entrypoint.sh .

# Create a non-root user and set permissions
RUN adduser --disabled-password --gecos '' nonrootuser && \
    chown nonrootuser:nonrootuser ./*.sql ./db-setup-entrypoint.sh

# Switch to non-root user
USER nonrootuser

ENTRYPOINT ["./db-setup-entrypoint.sh"]
