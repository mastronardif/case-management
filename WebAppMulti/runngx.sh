docker network create my-net

docker run -d --name webappmulti --network my-net webappmulti
docker run -d --name webappmulti2 --network my-net webappmulti

docker run -d --name nginx-proxy \
  --network my-net \
  -p 8080:80 \
  -v C:\Users\mastronardif\source\repos\WebAppMulti\nginx.conf:/etc/nginx/nginx.conf:ro \
  nginx:alpine
  # -v $(pwd)/nginx.conf:/etc/nginx/nginx.conf:ro \