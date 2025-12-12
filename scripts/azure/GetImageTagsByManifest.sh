#!/bin/bash

# Default values
tag=development
verbose=0

function show_help {
    echo "Usage: $0 [-c <container-registry>] [-g <resource-group>] [-s <subscription-id>] [-t <tag>]"
    echo ""
    echo "Options:"
    echo "  -c, --container-registry  (required)  Container registry"
    echo "  -f, --function-app-name   (required)  Name of the function app"
    echo "  -g, --resource-group      (required)  Resource group of ACR"
    echo "  -s, --subscription-id     (required)  Subscription id of ACR"
    echo "  -t, --tag                 (optional)  Name of the container tag (default: development)"
    echo "  -v, --verbose             (optional)  Be verbose about things"
    echo "  -h, --help                            Display this help message"
    exit 0
}

# Parse command line arguments
while [[ "$#" -gt 0 ]]; do
    case $1 in
        -c|--containe-registry)
            containerRegistry="$2"
            shift
            ;;
        -f|--function-app-name)
            functionAppName="$2"
            shift
            ;;
        -g|--resource-group)
            resourceGroup="$2"
            shift
            ;;
        -s|--subscription-id)
            subscriptionId="$2"
            shift
            ;;
        -t|--tag)
            tag="$2"
            shift
            ;;
        -v|--verbose)
            verbose=1
            ;;
        -h|--help)
            show_help
            ;;
        *)
            echo "Unknown parameter passed: $1"
            show_help
            ;;
    esac
    shift
done

if [[ -z "${containerRegistry}" ]]; then
  echo "Please provide a container registry argument"
  exit 1
fi

# stop processing if no image name is provided
if [[ -z "${functionAppName}" ]]; then
  echo "Please provide an image name as an argument"
  exit 1
fi

if [[ -z "${subscriptionId}" ]]; then
  echo "Please provide a Subscription ID argument"
  exit 1
fi

if [[ -z "${resourceGroup}" ]]; then
  echo "Please provide a resource group argument"
  exit 1
fi

if [[ verbose == true ]]; then
  echo "function app name: $functionAppName"
  echo "subscription Id: $subscriptionId"
  echo "resourceGroup: $resourceGroup"
  echo "containerRegistry: $containerRegistry"
  echo "tag: $tag"
fi

# get the digest of the image
digest=$(az acr repository show --name $containerRegistry --image $functionAppName:$tag --query 'digest' --output tsv 2> /dev/null)

# stop processing if the image does not exist
if [[ -z "$digest" ]]; then
  echo "Image $image not found in $containerRegistry"
  exit 1
fi

# echo the digest
if [[ verbose == true ]]; then
  echo $digest
fi

# get tags of all images in the repository that match this digest
az acr manifest list-metadata --registry $containerRegistry --name $functionAppName --query "[?digest=='$digest'].tags | [0] | [? @ != '$tag' && @ != 'latest'] | join(', ', @) " --output json 2> /dev/null | sed 's/,//g'
