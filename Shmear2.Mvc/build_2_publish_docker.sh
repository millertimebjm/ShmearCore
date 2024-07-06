#!/bin/bash

dotnet build


dotnet publish --os linux --arch x64 /t:PublishContainer -c Release
docker run -d -p 7443:7443 -v $(pwd):/certificate --restart=always --name shmear2-mvc shmear2-mvc:latest
rm -rf /tmp/Containers



# Check if there are any dangling images
if [ -n "$(docker images -f dangling=true -q)" ]; then
    # Remove dangling images
    docker rmi --force $(docker images -f "dangling=true" -q)
else
    echo "No dangling images found."
fi

