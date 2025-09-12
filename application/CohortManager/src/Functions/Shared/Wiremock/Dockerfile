# Use the official WireMock image as the base
FROM wiremock/wiremock:latest

# Copy the JSON mapping files into the container
# WireMock automatically loads mappings from the /home/wiremock/mappings directory
COPY /Shared/Wiremock/mappings /home/wiremock/mappings
