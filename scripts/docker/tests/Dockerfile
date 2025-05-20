# `*:latest` will be replaced with a corresponding version stored in the '.tool-versions' file
# hadolint ignore=DL3007
FROM python:latest
# Create a non-root user and set permissions
RUN adduser --disabled-password --gecos '' nonrootuser
# Switch to non-root user
USER nonrootuser
