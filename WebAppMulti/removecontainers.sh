

docker stop webappmulti
docker rm webappmulti

docker-compose down
docker-compose up --build -d
