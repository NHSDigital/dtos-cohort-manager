# !/bin/bash

# Log into Azure CLI as self
az login
az account set -s 'Digital Screening DToS - Core Services Prod Hub'

# Log in to  Azure Container Registry
az acr login --name acrukshubprodcohman

# Build the Docker image
docker build -t db-immutable-backup:latest .

# Tag the image for the registry
docker tag db-immutable-backup:latest acrukshubprodcohman.azurecr.io/db-immutable-backup:latest

# Push the image to the registry
docker push acrukshubprodcohman.azurecr.io/db-immutable-backup:latest
