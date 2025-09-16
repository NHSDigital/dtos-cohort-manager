# Use the official WireMock image as the base
FROM wiremock/wiremock:latest

# Copy the JSON mapping files into the container
# WireMock automatically loads mappings from the /home/wiremock/mappings directory
COPY /Shared/Wiremock/mappings /home/wiremock/mappings

# Use a default, non-root built in Wiremock user
USER wiremock
