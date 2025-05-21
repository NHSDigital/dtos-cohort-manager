FROM mcr.microsoft.com/mssql-tools

# Set a clean working directory
WORKDIR /app

# Copy SQL files and entrypoint script
COPY *.sql .
COPY db-setup-entrypoint.sh .

# Create a non-root user, set permissions and make endpoint executable
RUN adduser --disabled-password --gecos '' nonrootuser && \
    chmod +x db-setup-entrypoint.sh && \
    chown -R nonrootuser:nonrootuser /app

# Switch to non-root user
USER nonrootuser

ENTRYPOINT ["./db-setup-entrypoint.sh"]