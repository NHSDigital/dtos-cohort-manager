FROM mcr.microsoft.com/mssql-tools

COPY . .

# Create a non-root user and set permissions
RUN adduser --disabled-password --gecos '' nonrootuser
# Switch to non-root user
USER nonrootuser

ENTRYPOINT ["./db-setup-entrypoint.sh"]
