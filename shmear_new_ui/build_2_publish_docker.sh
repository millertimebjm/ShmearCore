#!/bin/bash

docker build -t shmear2-ui:1.0.0 .
docker run -d -p 7070:80 --name shmear2-ui shmear2-ui:1.0.0

rm -rf /tmp/Containers



# Check if there are any dangling images
if [ -n "$(docker images -f dangling=true -q)" ]; then
    # Remove dangling images
    docker rmi --force $(docker images -f "dangling=true" -q)
else
    echo "No dangling images found."
fi

