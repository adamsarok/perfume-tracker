Perfume Tracker: 
A simple app to track my perfume collection and try out NextJS :)

Docker commands:
docker build -t fuzzydice555/perfume-tracker .
docker run -p 3000:3000 fuzzydice555/perfume-tracker
docker push fuzzydice555/perfume-tracker   

Docker compose:
services:
    perfume-tracker:
        image: fuzzydice555/perfume-tracker
        ports:
          - 3000:3000
        restart: unless-stopped